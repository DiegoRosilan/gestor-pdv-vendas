using Npgsql;

namespace GestorPDV.Vendas;

/// <summary>
/// Acesso ao banco real do GestorPDV (mesmas tabelas usadas pelo sistema
/// Delphi original — não existe um banco "próprio" deste app). Grava
/// apenas os campos operacionais de uma venda simples (sem cálculo fiscal:
/// ICMS/PIS/COFINS/IBS/CBS ficam NULL, como já era feito no
/// GestorPDV.Monitor). Ao gravar ecf_venda_cabecalho com status_venda='F',
/// o gatilho trg_gestorpdv_venda_fechada (criado pelo GestorPDV.Monitor na
/// primeira vez que ele roda) enfileira a venda para impressão automática
/// — ou seja, esta tela de venda e o Monitor já trabalham juntos sem
/// nenhum código extra aqui.
/// </summary>
public sealed class Database
{
    private readonly string _connectionString;

    public Database(string connectionString) => _connectionString = connectionString;

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);
        return cn;
    }

    public async Task TestarConexaoAsync(CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
    }

    /// <summary>Login do operador (campo "Login" na tela de acesso do sistema original), não o nome.</summary>
    public async Task<Funcionario?> GetFuncionarioPorLoginAsync(string login, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT id, nome, login, senha, vendedor, caixa, gerente
        FROM public.ecf_funcionario
        WHERE sit = 'A'
          AND (vendedor = 'S' OR caixa = 'S' OR gerente = 'S')
          AND login ILIKE @login
        LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("login", login);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;
        return ReadFuncionario(rd);
    }

    private static Funcionario ReadFuncionario(NpgsqlDataReader rd) => new(
        rd.GetInt32(0),
        rd.GetString(1),
        rd.IsDBNull(2) ? null : rd.GetString(2),
        rd.IsDBNull(3) ? null : rd.GetString(3),
        rd.IsDBNull(4) ? false : rd.GetString(4) == "S",
        rd.IsDBNull(5) ? false : rd.GetString(5) == "S",
        rd.IsDBNull(6) ? false : rd.GetString(6) == "S");

    /// <summary>
    /// Confere a senha do gerente/supervisor entre os funcionários
    /// marcados como gerente (usado ao abrir ou encerrar o caixa). Exige
    /// uma senha real cadastrada — diferente do login do operador comum,
    /// aqui não existe "sem senha libera direto".
    /// </summary>
    public async Task<Funcionario?> ValidarGerenteAsync(string senha, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(senha)) return null;

        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT id, nome, login, senha, vendedor, caixa, gerente
        FROM public.ecf_funcionario
        WHERE sit = 'A' AND gerente = 'S' AND senha = @senha
        LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("senha", senha);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;
        return ReadFuncionario(rd);
    }

    public async Task<List<Turno>> GetTurnosAsync(CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = "SELECT id, descricao, hora_inicio::text, hora_fim::text FROM public.ecf_turno ORDER BY id;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        var result = new List<Turno>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new Turno(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? "" : rd.GetString(1),
                rd.IsDBNull(2) ? null : rd.GetString(2),
                rd.IsDBNull(3) ? null : rd.GetString(3)));
        }
        return result;
    }

    public async Task<List<Caixa>> GetCaixasAsync(CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = "SELECT id, nome FROM public.ecf_caixa ORDER BY id;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        var result = new List<Caixa>();
        while (await rd.ReadAsync(ct))
            result.Add(new Caixa(rd.GetInt32(0), rd.GetString(1)));
        return result;
    }

    /// <summary>Retorna o id de um movimento (turno de caixa) já aberto no sistema, ou null se nenhum estiver aberto.</summary>
    public async Task<int?> GetMovimentoAbertoAsync(CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT id FROM public.ecf_movimento
        WHERE status_movimento = 'A'
        ORDER BY id DESC LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    private static async Task<int> GetOrCreateOperadorAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, int idFuncionario, CancellationToken ct)
    {
        await using (var cmd = new NpgsqlCommand(
            "SELECT id FROM public.ecf_operador WHERE id_ecf_funcionario = @f LIMIT 1;", cn, tx))
        {
            cmd.Parameters.AddWithValue("f", idFuncionario);
            var existing = await cmd.ExecuteScalarAsync(ct);
            if (existing is not null and not DBNull)
                return Convert.ToInt32(existing);
        }

        await using var insert = new NpgsqlCommand(
            "INSERT INTO public.ecf_operador (id_ecf_funcionario, sit) VALUES (@f, 'A') RETURNING id;", cn, tx);
        insert.Parameters.AddWithValue("f", idFuncionario);
        return (int)(await insert.ExecuteScalarAsync(ct))!;
    }

    /// <summary>
    /// id_ecf_operador (usado em ecf_venda_cabecalho e ecf_movimento) é uma
    /// tabela separada de ecf_funcionario — cada funcionário que efetivamente
    /// opera o PDV precisa de uma linha correspondente em ecf_operador. Usado
    /// logo após o login, independente de estar abrindo um caixa novo ou
    /// reaproveitando um já aberto.
    /// </summary>
    public async Task<int> GetOrCreateOperadorAsync(int idFuncionario, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        return await GetOrCreateOperadorAsync(cn, null, idFuncionario, ct);
    }

    /// <summary>
    /// Abre um novo movimento (turno de caixa). Impressora fixada em 0
    /// ("NENHUM" no cadastro) — este app não emite fiscal de verdade, então
    /// não há impressora fiscal real associada.
    /// </summary>
    public async Task<int> AbrirCaixaAsync(
        int idFuncionarioOperador, int idFuncionarioGerente, int idCaixa, int idTurno, decimal suprimento,
        CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var idOperador = await GetOrCreateOperadorAsync(cn, tx, idFuncionarioOperador, ct);
        var idGerenteOperador = await GetOrCreateOperadorAsync(cn, tx, idFuncionarioGerente, ct);

        int idEmpresa;
        await using (var cmd = new NpgsqlCommand("SELECT id FROM public.ecf_empresa ORDER BY id LIMIT 1;", cn, tx))
            idEmpresa = (int)(await cmd.ExecuteScalarAsync(ct))!;

        const string sql = """
        INSERT INTO public.ecf_movimento
            (id_ecf_empresa, id_ecf_turno, id_ecf_impressora, id_ecf_operador,
             id_ecf_caixa, id_gerente_supervisor, status_movimento,
             data_abertura, hora_abertura, total_suprimento)
        VALUES
            (@empresa, @turno, 0, @operador,
             @caixa, @gerenteOperador, 'A',
             CURRENT_DATE, to_char(now(), 'HH24:MI:SS'), @suprimento)
        RETURNING id;
        """;
        await using var insert = new NpgsqlCommand(sql, cn, tx);
        insert.Parameters.AddWithValue("empresa", idEmpresa);
        insert.Parameters.AddWithValue("turno", idTurno);
        insert.Parameters.AddWithValue("operador", idOperador);
        insert.Parameters.AddWithValue("caixa", idCaixa);
        insert.Parameters.AddWithValue("gerenteOperador", idGerenteOperador);
        insert.Parameters.AddWithValue("suprimento", suprimento);
        var idMovimento = (int)(await insert.ExecuteScalarAsync(ct))!;

        await tx.CommitAsync(ct);
        return idMovimento;
    }

    /// <summary>Dados exibidos na tela de login quando já existe um movimento aberto para entrar.</summary>
    public async Task<MovimentoInfo?> GetMovimentoInfoAsync(int idMovimento, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT m.id, t.descricao, c.nome,
               COALESCE(NULLIF(TRIM(i.marca || ' ' || COALESCE(i.modelo, '')), ''), 'NENHUM'),
               m.data_abertura, m.hora_abertura
        FROM public.ecf_movimento m
        LEFT JOIN public.ecf_turno t ON t.id = m.id_ecf_turno
        LEFT JOIN public.ecf_caixa c ON c.id = m.id_ecf_caixa
        LEFT JOIN public.ecf_impressora i ON i.id = m.id_ecf_impressora
        WHERE m.id = @id;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("id", idMovimento);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new MovimentoInfo(
            rd.GetInt32(0),
            rd.IsDBNull(1) ? "" : rd.GetString(1),
            rd.IsDBNull(2) ? "" : rd.GetString(2),
            rd.IsDBNull(3) ? "NENHUM" : rd.GetString(3),
            rd.IsDBNull(4) ? null : rd.GetDateTime(4),
            rd.IsDBNull(5) ? null : rd.GetString(5));
    }

    /// <summary>
    /// Encerra o movimento: grava os valores declarados pelo operador em
    /// ecf_fechamento (o "conferido" do caixa), soma os totais reais do
    /// sistema (vendas/pagamentos lançados neste movimento) em
    /// ecf_movimento e marca status_movimento='F'. Não compara o declarado
    /// com o total do sistema (isso ficaria para um relatório de
    /// conferência, fora do escopo desta versão).
    /// </summary>
    public async Task EncerrarCaixaAsync(int idMovimento, IReadOnlyList<Encerrante> encerrantes, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        const string sqlEncerrante = """
        INSERT INTO public.ecf_fechamento (id_ecf_movimento, tipo_pagamento, valor)
        VALUES (@idMovimento, @tipo, @valor);
        """;
        foreach (var e in encerrantes)
        {
            await using var cmd = new NpgsqlCommand(sqlEncerrante, cn, tx);
            cmd.Parameters.AddWithValue("idMovimento", idMovimento);
            cmd.Parameters.AddWithValue("tipo", e.TipoPagamento);
            cmd.Parameters.AddWithValue("valor", e.Valor);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        const string sqlFechar = """
        UPDATE public.ecf_movimento m
        SET status_movimento = 'F',
            data_fechamento = CURRENT_DATE,
            hora_fechamento = to_char(now(), 'HH24:MI:SS'),
            total_venda = COALESCE((SELECT SUM(v.valor_final) FROM public.ecf_venda_cabecalho v
                                     WHERE v.id_ecf_movimento = m.id AND v.status_venda = 'F'), 0),
            total_desconto = COALESCE((SELECT SUM(v.desconto) FROM public.ecf_venda_cabecalho v
                                        WHERE v.id_ecf_movimento = m.id AND v.status_venda = 'F'), 0),
            total_recebido = COALESCE((SELECT SUM(t.valor) FROM public.ecf_total_tipo_pgto t
                                        JOIN public.ecf_venda_cabecalho v ON v.id = t.id_ecf_venda_cabecalho
                                        WHERE v.id_ecf_movimento = m.id AND v.status_venda = 'F'), 0),
            total_crediario = COALESCE((SELECT SUM(t.valor) FROM public.ecf_total_tipo_pgto t
                                         JOIN public.ecf_venda_cabecalho v ON v.id = t.id_ecf_venda_cabecalho
                                         JOIN public.ecf_tipo_pagamento fp ON fp.id = t.id_ecf_tipo_pagamento
                                         WHERE v.id_ecf_movimento = m.id AND v.status_venda = 'F'
                                           AND fp.gera_parcelas = 'S'), 0)
        WHERE m.id = @idMovimento;
        """;
        await using (var cmd = new NpgsqlCommand(sqlFechar, cn, tx))
        {
            cmd.Parameters.AddWithValue("idMovimento", idMovimento);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<Produto?> BuscarProdutoPorCodigoAsync(string codigo, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT p.id, p.cod_produto, p.codigo_interno, p.gtin, p.nome,
               p.valor_venda, p.qtd_estoque, u.sigla, p.localizacao
        FROM public.produto p
        LEFT JOIN public.unidade_produto u ON u.id = p.id_unidade_produto
        WHERE p.produto_pdv = 'S' AND p.inativo = 'N'
          AND (p.cod_produto::text = @c OR p.codigo_interno = @c OR p.gtin = @c)
        ORDER BY p.id LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("c", codigo);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;
        return ReadProduto(rd);
    }

    public async Task<List<Produto>> BuscarProdutosPorNomeAsync(string termo, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT p.id, p.cod_produto, p.codigo_interno, p.gtin, p.nome,
               p.valor_venda, p.qtd_estoque, u.sigla, p.localizacao
        FROM public.produto p
        LEFT JOIN public.unidade_produto u ON u.id = p.id_unidade_produto
        WHERE p.produto_pdv = 'S' AND p.inativo = 'N'
          AND p.nome ILIKE @t
        ORDER BY p.nome LIMIT 30;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("t", $"%{termo}%");
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        var result = new List<Produto>();
        while (await rd.ReadAsync(ct))
            result.Add(ReadProduto(rd));
        return result;
    }

    private static Produto ReadProduto(NpgsqlDataReader rd) => new(
        rd.GetInt32(0),
        rd.IsDBNull(1) ? null : rd.GetInt32(1).ToString(),
        rd.IsDBNull(2) ? null : rd.GetString(2),
        rd.IsDBNull(3) ? null : rd.GetString(3),
        rd.GetString(4),
        rd.IsDBNull(5) ? 0m : rd.GetDecimal(5),
        rd.IsDBNull(6) ? 0m : rd.GetDecimal(6),
        rd.IsDBNull(7) ? "UN" : rd.GetString(7),
        rd.IsDBNull(8) ? null : rd.GetString(8));

    public async Task<List<Cliente>> BuscarClientesAsync(string termo, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT id, cod_cliente, nome, cpf_cnpj
        FROM public.cliente
        WHERE sit = 'A' AND (nome ILIKE @t OR cpf_cnpj = @termoExato)
        ORDER BY nome LIMIT 30;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("t", $"%{termo}%");
        cmd.Parameters.AddWithValue("termoExato", termo);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        var result = new List<Cliente>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new Cliente(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? null : rd.GetInt32(1).ToString(),
                rd.GetString(2),
                rd.IsDBNull(3) ? null : rd.GetString(3)));
        }
        return result;
    }

    public async Task<List<FormaPagamento>> GetFormasPagamentoAsync(CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        const string sql = """
        SELECT id, descricao, gera_parcelas
        FROM public.ecf_tipo_pagamento
        WHERE sit IS DISTINCT FROM 'I'
        ORDER BY id;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        var result = new List<FormaPagamento>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new FormaPagamento(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                !rd.IsDBNull(2) && rd.GetString(2) == "S"));
        }
        return result;
    }

    /// <summary>Todas as notas (vendas finalizadas) do movimento atual, mais recente primeiro — exibidas no Menu Fiscal (F8). Usa as mesmas colunas já gravadas por FinalizarVendaAsync.</summary>
    public async Task<List<NotaEmitidaInfo>> GetNotasEmitidasAsync(int idMovimento, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
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

    /// <summary>
    /// Grava a venda inteira numa única transação: cabeçalho, itens, baixa
    /// de estoque, pagamentos e — se algum pagamento for crediário — as
    /// parcelas em contas_pagar_receber/contas_parcelas. Se algo falhar no
    /// meio, nada é gravado (rollback).
    /// </summary>
    public async Task<int> FinalizarVendaAsync(
        int idMovimento,
        int idFuncionario,
        int idOperador,
        Cliente? cliente,
        IReadOnlyList<ItemCarrinho> itens,
        IReadOnlyList<PagamentoLancado> pagamentos,
        decimal descontoGeral,
        decimal acrescimoGeral,
        string? observacao,
        int parcelasCrediario,
        DateOnly primeiroVencimentoCrediario,
        CancellationToken ct)
    {
        if (itens.Count == 0)
            throw new InvalidOperationException("A venda não tem nenhum item.");

        await using var cn = await OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        // Itens cancelados (F5 na tela de venda) ficam gravados — pra manter o
        // histórico do cupom — mas não entram nos totais nem na baixa de estoque.
        var itensValidos = itens.Where(i => !i.Cancelado).ToList();
        var subtotal = itensValidos.Sum(i => i.Quantidade * i.ValorUnitario);
        var descontoTotal = itensValidos.Sum(i => i.Desconto) + descontoGeral;
        var total = Math.Max(0m, itensValidos.Sum(i => i.Total) - descontoGeral + acrescimoGeral);
        var recebido = pagamentos.Sum(p => p.Valor);

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
            cmd.Parameters.AddWithValue("idCliente", (object?)cliente?.Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("idFuncionario", idFuncionario);
            cmd.Parameters.AddWithValue("idOperador", idOperador);
            cmd.Parameters.AddWithValue("idMovimento", idMovimento);
            cmd.Parameters.AddWithValue("coo", nextCoo);
            cmd.Parameters.AddWithValue("subtotal", subtotal);
            cmd.Parameters.AddWithValue("desconto", descontoTotal);
            cmd.Parameters.AddWithValue("acrescimo", acrescimoGeral);
            cmd.Parameters.AddWithValue("total", total);
            cmd.Parameters.AddWithValue("recebido", recebido);
            cmd.Parameters.AddWithValue("troco", Math.Max(0m, recebido - total));
            cmd.Parameters.AddWithValue("nomeCliente", (object?)cliente?.Nome ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cpfCliente", (object?)cliente?.CpfCnpj ?? DBNull.Value);
            cmd.Parameters.AddWithValue("operadorInc", $"funcionario:{idFuncionario}");
            cmd.Parameters.AddWithValue("observacao", (object?)observacao ?? DBNull.Value);
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
        foreach (var item in itens)
        {
            var valorTotalBruto = item.Quantidade * item.ValorUnitario;

            await using (var cmd = new NpgsqlCommand(sqlItem, cn, tx))
            {
                cmd.Parameters.AddWithValue("idProduto", item.Produto.Id);
                cmd.Parameters.AddWithValue("idVenda", idVenda);
                cmd.Parameters.AddWithValue("item", numeroItem);
                cmd.Parameters.AddWithValue("quantidade", item.Quantidade);
                cmd.Parameters.AddWithValue("valorUnitario", item.ValorUnitario);
                cmd.Parameters.AddWithValue("valorTotal", valorTotalBruto);
                cmd.Parameters.AddWithValue("totalItem", item.Total);
                cmd.Parameters.AddWithValue("desconto", item.Desconto);
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
        foreach (var pg in pagamentos)
        {
            await using var cmd = new NpgsqlCommand(sqlPagamento, cn, tx);
            cmd.Parameters.AddWithValue("idVenda", idVenda);
            cmd.Parameters.AddWithValue("idForma", pg.Forma.Id);
            cmd.Parameters.AddWithValue("valor", pg.Valor);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var crediario = pagamentos.FirstOrDefault(p => p.Forma.GeraParcelas);
        if (crediario is not null && parcelasCrediario > 0)
        {
            const string sqlContas = """
            INSERT INTO public.contas_pagar_receber
                (id_ecf_venda_cabecalho, id_pessoa, tipo, valor, data_lancamento,
                 primeiro_vencimento, natureza_lancamento, quantidade_parcela,
                 numero_cupom, situacao, hora_emissao, operador_inc)
            VALUES
                (@idVenda, @idPessoa, 'R', @valor, CURRENT_DATE,
                 @primeiroVencimento, 'S', @qtdParcelas,
                 @coo, 'A', to_char(now(), 'HH24:MI:SS'), @operadorInc)
            RETURNING id;
            """;
            int idContas;
            await using (var cmd = new NpgsqlCommand(sqlContas, cn, tx))
            {
                cmd.Parameters.AddWithValue("idVenda", idVenda);
                cmd.Parameters.AddWithValue("idPessoa", (object?)cliente?.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("valor", crediario.Valor);
                cmd.Parameters.AddWithValue("primeiroVencimento", primeiroVencimentoCrediario.ToDateTime(TimeOnly.MinValue));
                cmd.Parameters.AddWithValue("qtdParcelas", parcelasCrediario);
                cmd.Parameters.AddWithValue("coo", nextCoo);
                cmd.Parameters.AddWithValue("operadorInc", $"funcionario:{idFuncionario}");
                idContas = (int)(await cmd.ExecuteScalarAsync(ct))!;
            }

            const string sqlParcela = """
            INSERT INTO public.contas_parcelas
                (id_contas_pagar_receber, data_emissao, data_vencimento,
                 numero_parcela, valor, total_parcela, situacao, hora_emissao, operador_inc)
            VALUES
                (@idContas, CURRENT_DATE, @vencimento,
                 @numero, @valor, @valor, 'A', to_char(now(), 'HH24:MI:SS'), @operadorInc);
            """;

            var valorParcelaBase = Math.Round(crediario.Valor / parcelasCrediario, 2, MidpointRounding.ToEven);
            var somaParcelasAnteriores = 0m;
            for (var n = 1; n <= parcelasCrediario; n++)
            {
                var valorParcela = n < parcelasCrediario
                    ? valorParcelaBase
                    : crediario.Valor - somaParcelasAnteriores;
                somaParcelasAnteriores += valorParcela;

                await using var cmd = new NpgsqlCommand(sqlParcela, cn, tx);
                cmd.Parameters.AddWithValue("idContas", idContas);
                cmd.Parameters.AddWithValue("vencimento", primeiroVencimentoCrediario.AddMonths(n - 1).ToDateTime(TimeOnly.MinValue));
                cmd.Parameters.AddWithValue("numero", n);
                cmd.Parameters.AddWithValue("valor", valorParcela);
                cmd.Parameters.AddWithValue("operadorInc", $"funcionario:{idFuncionario}");
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        await tx.CommitAsync(ct);
        return idVenda;
    }
}
