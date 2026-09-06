using GestorPDV.Infrastructure.Database;
using Xunit;

namespace GestorPDV.Infrastructure.Tests;

/// <summary>
/// Testes de repositório de verdade (ProdutoRepository, VendaRepository
/// etc.) precisam de um banco Postgres real com o schema do GestorPDV —
/// nenhum está aqui ainda porque este ambiente não tem acesso a um (ver
/// LEIA-ME em legacy/GestorPDV.Vendas). Este teste só confere que a
/// connection string é validada antes de qualquer tentativa de conexão.
/// </summary>
public class PostgreSqlConnectionFactoryTests
{
    [Fact]
    public async Task AbrirAsync_ConnectionStringInvalida_LancaExcecao()
    {
        var factory = new PostgreSqlConnectionFactory("Host=host-que-nao-existe.invalid;Timeout=1");

        await Assert.ThrowsAnyAsync<Exception>(() => factory.AbrirAsync(CancellationToken.None));
    }
}
