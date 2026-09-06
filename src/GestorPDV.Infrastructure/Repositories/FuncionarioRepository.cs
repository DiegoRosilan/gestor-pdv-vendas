using GestorPDV.Application.Interfaces;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>Portado de legacy/GestorPDV.Vendas/Database.cs (login, validação de gerente, operador).</summary>
public sealed class FuncionarioRepository : IFuncionarioRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public FuncionarioRepository(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<FuncionarioInfo?> GetPorLoginAsync(string login, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
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
        return await rd.ReadAsync(ct) ? Mapear(rd) : null;
    }

    public async Task<FuncionarioInfo?> ValidarGerenteAsync(string senha, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(senha))
            return null;

        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = """
        SELECT id, nome, login, senha, vendedor, caixa, gerente
        FROM public.ecf_funcionario
        WHERE sit = 'A' AND gerente = 'S' AND senha = @senha
        LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("senha", senha);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        return await rd.ReadAsync(ct) ? Mapear(rd) : null;
    }

    public async Task<int> GetOrCreateOperadorAsync(int idFuncionario, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);

        await using (var cmd = new NpgsqlCommand("SELECT id FROM public.ecf_operador WHERE id_ecf_funcionario = @f LIMIT 1;", cn))
        {
            cmd.Parameters.AddWithValue("f", idFuncionario);
            var existente = await cmd.ExecuteScalarAsync(ct);
            if (existente is not null and not DBNull)
                return Convert.ToInt32(existente);
        }

        await using var insert = new NpgsqlCommand(
            "INSERT INTO public.ecf_operador (id_ecf_funcionario, sit) VALUES (@f, 'A') RETURNING id;", cn);
        insert.Parameters.AddWithValue("f", idFuncionario);
        return (int)(await insert.ExecuteScalarAsync(ct))!;
    }

    private static FuncionarioInfo Mapear(NpgsqlDataReader rd) => new(
        rd.GetInt32(0),
        rd.GetString(1),
        rd.IsDBNull(2) ? null : rd.GetString(2),
        rd.IsDBNull(3) ? null : rd.GetString(3),
        !rd.IsDBNull(4) && rd.GetString(4) == "S",
        !rd.IsDBNull(5) && rd.GetString(5) == "S",
        !rd.IsDBNull(6) && rd.GetString(6) == "S");
}
