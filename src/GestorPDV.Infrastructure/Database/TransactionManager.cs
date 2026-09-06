using Npgsql;

namespace GestorPDV.Infrastructure.Database;

/// <summary>
/// Executa uma unidade de trabalho dentro de uma transação (abre conexão,
/// begin, roda a ação, commit — rollback automático se a ação lançar).
/// Usado por repositórios que gravam mais de uma tabela numa operação só
/// (ex.: VendaRepository.FinalizarAsync: cabeçalho + itens + pagamentos).
/// </summary>
public sealed class TransactionManager
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public TransactionManager(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<T> ExecutarAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> acao, CancellationToken ct)
    {
        await using var conexao = await _connectionFactory.AbrirAsync(ct);
        await using var transacao = await conexao.BeginTransactionAsync(ct);

        var resultado = await acao(conexao, transacao);
        await transacao.CommitAsync(ct);
        return resultado;
    }
}
