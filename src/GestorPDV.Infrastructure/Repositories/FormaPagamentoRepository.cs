using GestorPDV.Application.Interfaces;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>Portado de legacy/GestorPDV.Vendas/Database.cs.GetFormasPagamentoAsync.</summary>
public sealed class FormaPagamentoRepository : IFormaPagamentoRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public FormaPagamentoRepository(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<List<FormaPagamentoInfo>> GetTodasAsync(CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = """
        SELECT id, descricao, gera_parcelas
        FROM public.ecf_tipo_pagamento
        WHERE sit IS DISTINCT FROM 'I'
        ORDER BY id;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        var result = new List<FormaPagamentoInfo>();
        while (await rd.ReadAsync(ct))
        {
            result.Add(new FormaPagamentoInfo(
                rd.GetInt32(0),
                rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                !rd.IsDBNull(2) && rd.GetString(2) == "S"));
        }
        return result;
    }
}
