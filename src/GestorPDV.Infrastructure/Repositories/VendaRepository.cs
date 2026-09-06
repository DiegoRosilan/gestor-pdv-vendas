using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>
/// Portado de legacy/GestorPDV.Vendas/Database.cs.FinalizarVendaAsync e
/// GetNotasEmitidasAsync — mesmas tabelas e mesma regra pra item
/// cancelado (fica gravado com cancelado='S'/movimenta_estoque='N', sem
/// entrar nos totais do cabeçalho).
/// </summary>
public sealed class VendaRepository : IVendaRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;
    private readonly TransactionManager _transactionManager;

    public VendaRepository(PostgreSqlConnectionFactory connectionFactory, TransactionManager transactionManager)
    {
        _connectionFactory = connectionFactory;
        _transactionManager = transactionManager;
    }

    public Task<(int Id, int Coo)> FinalizarAsync(Venda venda, CancellationToken ct)
        => _transactionManager.ExecutarAsync(async (cn, tx) =>
        {
            var itensValidos = venda.Itens.Where(i => !i.Cancelado).ToList();
            var subtotal = itensValidos.Sum(i => i.Quantidade * i.ValorUnitario.Valor);
            var descontoTotal = itensValidos.Sum(i => i.Desconto.Valor) + venda.DescontoGeral.Valor;
            var total = Math.Max(0m, venda.Total.Valor);
            var recebido = venda.TotalRecebido.Valor;

            int nextCoo;
            await using (var cmd = new NpgsqlCommand(
                "SELECT COALESCE(MAX(coo), 0) + 1 FROM public.ecf_venda_cabecalho WHERE coo > 0;", cn, tx))
            {
                nextCoo = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            }

            int idVenda;
            const string sqlCabecalho = """
            INSERT INTO public.ecf_venda_cabecalho
                (id_cliente, id_ecf_funcionario, id_ecf_operador, id_ecf_movimento,
                 cfop, coo, data_venda, hora_venda,
                 valor_venda, desconto, acrescimo, valor_final, valor_recebido, troco,
                 status_venda, nome_cliente, cpf_cnpj_cliente, operador_inc, modelo_cupom, observacao)
            VALUES
                (@idCliente, @idFuncionario, @idOperador, @idMovimento,
                 5102, @coo, CURRENT_DATE, to_char(now(), 'HH24:MI:SS'),
                 @subtotal, @desconto, @acrescimo, @total, @recebido, @troco,
                 'F', @nomeCliente, @cpfCliente, @operadorInc, '2', @observacao)
            RETURNING id;
            """;
            await using (var cmd = new NpgsqlCommand(sqlCabecalho, cn, tx))
            {
                cmd.Parameters.AddWithValue("idCliente", (object?)venda.Cliente?.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("idFuncionario", venda.IdFuncionario);
                cmd.Parameters.AddWithValue("idOperador", venda.IdOperador);
                cmd.Parameters.AddWithValue("idMovimento", venda.IdMovimento);
                cmd.Parameters.AddWithValue("coo", nextCoo);
                cmd.Parameters.AddWithValue("subtotal", subtotal);
                cmd.Parameters.AddWithValue("desconto", descontoTotal);
                cmd.Parameters.AddWithValue("acrescimo", venda.AcrescimoGeral.Valor);
                cmd.Parameters.AddWithValue("total", total);
                cmd.Parameters.AddWithValue("recebido", recebido);
                cmd.Parameters.AddWithValue("troco", Math.Max(0m, recebido - total));
                cmd.Parameters.AddWithValue("nomeCliente", (object?)venda.Cliente?.Nome ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cpfCliente", (object?)venda.Cliente?.CpfCnpj?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("operadorInc", $"funcionario:{venda.IdFuncionario}");
                cmd.Parameters.AddWithValue("observacao", (object?)venda.Observacao ?? DBNull.Value);
                idVenda = (int)(await cmd.ExecuteScalarAsync(ct))!;
            }

            const string sqlItem = """
            INSERT INTO public.ecf_venda_detalhe
                (id_ecf_produto, id_ecf_venda_cabecalho, cfop, item,
                 quantidade, valor_unitario, valor_total, total_item, desconto,
                 cancelado, movimenta_estoque, hora)
            VALUES
                (@idProduto, @idVenda, 5102, @item,
                 @quantidade, @valorUnitario, @valorTotal, @totalItem, @desconto,
                 @cancelado, @movimentaEstoque, to_char(now(), 'HH24:MI:SS'));
            """;
            const string sqlEstoque = "UPDATE public.produto SET qtd_estoque = qtd_estoque - @qtd WHERE id = @id;";

            var numeroItem = 1;
            foreach (var item in venda.Itens)
            {
                var valorTotalBruto = item.Quantidade * item.ValorUnitario.Valor;

                await using (var cmd = new NpgsqlCommand(sqlItem, cn, tx))
                {
                    cmd.Parameters.AddWithValue("idProduto", item.Produto.Id);
                    cmd.Parameters.AddWithValue("idVenda", idVenda);
                    cmd.Parameters.AddWithValue("item", numeroItem);
                    cmd.Parameters.AddWithValue("quantidade", item.Quantidade);
                    cmd.Parameters.AddWithValue("valorUnitario", item.ValorUnitario.Valor);
                    cmd.Parameters.AddWithValue("valorTotal", valorTotalBruto);
                    cmd.Parameters.AddWithValue("totalItem", item.Total.Valor);
                    cmd.Parameters.AddWithValue("desconto", item.Desconto.Valor);
                    cmd.Parameters.AddWithValue("cancelado", item.Cancelado ? "S" : "N");
                    cmd.Parameters.AddWithValue("movimentaEstoque", item.Cancelado ? "N" : "S");
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                if (!item.Cancelado)
                {
                    await using var cmd = new NpgsqlCommand(sqlEstoque, cn, tx);
                    cmd.Parameters.AddWithValue("qtd", item.Quantidade);
                    cmd.Parameters.AddWithValue("id", item.Produto.Id);
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                numeroItem++;
            }

            const string sqlPagamento = """
            INSERT INTO public.ecf_total_tipo_pgto
                (id_ecf_venda_cabecalho, id_ecf_tipo_pagamento, valor, data_venda)
            VALUES (@idVenda, @idForma, @valor, CURRENT_DATE);
            """;
            foreach (var pagamento in venda.Pagamentos)
            {
                await using var cmd = new NpgsqlCommand(sqlPagamento, cn, tx);
                cmd.Parameters.AddWithValue("idVenda", idVenda);
                cmd.Parameters.AddWithValue("idForma", pagamento.IdFormaPagamento);
                cmd.Parameters.AddWithValue("valor", pagamento.Valor.Valor);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Parcelamento de crediário (contas_pagar_receber/contas_parcelas) fica
            // pendente de portar nesta rodada — ver legacy/GestorPDV.Vendas/Database.cs
            // FinalizarVendaAsync pra referência quando isso entrar em escopo aqui.

            return (idVenda, nextCoo);
        }, ct);

    public async Task<List<NotaEmitidaInfo>> GetNotasEmitidasAsync(int idMovimento, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = """
        SELECT id, coo, data_venda, hora_venda, valor_final, status_venda
        FROM public.ecf_venda_cabecalho
        WHERE id_ecf_movimento = @idMovimento
        ORDER BY id DESC;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("idMovimento", idMovimento);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        var result = new List<NotaEmitidaInfo>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new NotaEmitidaInfo(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                rd.GetDateTime(2),
                rd.IsDBNull(3) ? null : rd.GetString(3),
                rd.IsDBNull(4) ? 0m : rd.GetDecimal(4),
                rd.IsDBNull(5) ? "" : rd.GetString(5)));
        }
        return result;
    }
}
