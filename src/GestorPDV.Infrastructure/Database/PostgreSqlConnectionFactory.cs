using Npgsql;

namespace GestorPDV.Infrastructure.Database;

/// <summary>Única fonte de conexões do app — mesmo banco/schema do legado (legacy/GestorPDV.Vendas), sem tabela própria nova.</summary>
public sealed class PostgreSqlConnectionFactory
{
    private readonly string _connectionString;

    public PostgreSqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conexao = new NpgsqlConnection(_connectionString);
        await conexao.OpenAsync(ct);
        return conexao;
    }
}
