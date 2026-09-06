namespace GestorPDV.Infrastructure.Database;

/// <summary>
/// Confere se o banco está acessível na subida do App/Monitor. NÃO cria
/// nem altera schema — o banco já existe e é o mesmo do sistema Delphi
/// original (ver database/ na raiz do repositório); qualquer alteração de
/// schema é feita via scripts em src/GestorPDV.Infrastructure/Migrations,
/// nunca automaticamente aqui.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public DatabaseInitializer(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<bool> TestarConexaoAsync(CancellationToken ct)
    {
        try
        {
            await using var conexao = await _connectionFactory.AbrirAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
