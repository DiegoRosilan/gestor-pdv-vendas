using GestorPDV.Application.Interfaces;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>Portado de legacy/GestorPDV.Vendas/Database.cs (turno/terminal/movimento — mesmas tabelas ecf_turno/ecf_caixa/ecf_movimento).</summary>
public sealed class CaixaRepository : ICaixaRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;
    private readonly TransactionManager _transactionManager;

    public CaixaRepository(PostgreSqlConnectionFactory connectionFactory, TransactionManager transactionManager)
    {
        _connectionFactory = connectionFactory;
        _transactionManager = transactionManager;
    }

    public async Task<int?> GetMovimentoAbertoAsync(CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = "SELECT id FROM public.ecf_movimento WHERE status_movimento = 'A' ORDER BY id DESC LIMIT 1;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        var resultado = await cmd.ExecuteScalarAsync(ct);
        return resultado is null or DBNull ? null : Convert.ToInt32(resultado);
    }

    public async Task<MovimentoInfo?> GetMovimentoInfoAsync(int idMovimento, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
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

    public async Task<List<TurnoInfo>> GetTurnosAsync(CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = "SELECT id, descricao, hora_inicio::text, hora_fim::text FROM public.ecf_turno ORDER BY id;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        var result = new List<TurnoInfo>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new TurnoInfo(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? "" : rd.GetString(1),
                rd.IsDBNull(2) ? null : rd.GetString(2),
                rd.IsDBNull(3) ? null : rd.GetString(3)));
        }
        return result;
    }

    public async Task<List<TerminalInfo>> GetTerminaisAsync(CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = "SELECT id, nome FROM public.ecf_caixa ORDER BY id;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        var result = new List<TerminalInfo>();
        while (await rd.ReadAsync(ct))
            result.Add(new TerminalInfo(rd.GetInt32(0), rd.GetString(1)));
        return result;
    }

    private async Task<int> GetOrCreateOperadorAsync(NpgsqlConnection cn, NpgsqlTransaction tx, int idFuncionario, CancellationToken ct)
    {
        await using (var cmd = new NpgsqlCommand("SELECT id FROM public.ecf_operador WHERE id_ecf_funcionario = @f LIMIT 1;", cn, tx))
        {
            cmd.Parameters.AddWithValue("f", idFuncionario);
            var existente = await cmd.ExecuteScalarAsync(ct);
            if (existente is not null and not DBNull)
                return Convert.ToInt32(existente);
        }

        await using var insert = new NpgsqlCommand("INSERT INTO public.ecf_operador (id_ecf_funcionario, sit) VALUES (@f, 'A') RETURNING id;", cn, tx);
        insert.Parameters.AddWithValue("f", idFuncionario);
        return (int)(await insert.ExecuteScalarAsync(ct))!;
    }

    public Task<int> AbrirCaixaAsync(int idFuncionarioOperador, int idFuncionarioGerente, int idCaixa, int idTurno, decimal suprimento, CancellationToken ct)
        => _transactionManager.ExecutarAsync(async (cn, tx) =>
        {
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
            return (int)(await insert.ExecuteScalarAsync(ct))!;
        }, ct);

    public Task EncerrarCaixaAsync(int idMovimento, IReadOnlyList<(string TipoPagamento, decimal Valor)> encerrantes, CancellationToken ct)
        => _transactionManager.ExecutarAsync<object?>(async (cn, tx) =>
        {
            const string sqlEncerrante = """
            INSERT INTO public.ecf_fechamento (id_ecf_movimento, tipo_pagamento, valor)
            VALUES (@idMovimento, @tipo, @valor);
            """;
            foreach (var (tipoPagamento, valor) in encerrantes)
            {
                await using var cmd = new NpgsqlCommand(sqlEncerrante, cn, tx);
                cmd.Parameters.AddWithValue("idMovimento", idMovimento);
                cmd.Parameters.AddWithValue("tipo", tipoPagamento);
                cmd.Parameters.AddWithValue("valor", valor);
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
            await using var cmd2 = new NpgsqlCommand(sqlFechar, cn, tx);
            cmd2.Parameters.AddWithValue("idMovimento", idMovimento);
            await cmd2.ExecuteNonQueryAsync(ct);
            return null;
        }, ct);
}
