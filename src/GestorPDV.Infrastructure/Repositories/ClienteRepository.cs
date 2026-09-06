using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;
using GestorPDV.Domain.ValueObjects;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>Portado de legacy/GestorPDV.Vendas/Database.cs.BuscarClientesAsync.</summary>
public sealed class ClienteRepository : IClienteRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public ClienteRepository(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<List<Cliente>> BuscarAsync(string termo, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
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
            var cpfCnpjTexto = rd.IsDBNull(3) ? null : rd.GetString(3);
            Documento? documento = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(cpfCnpjTexto))
                    documento = new Documento(cpfCnpjTexto);
            }
            catch (ArgumentException)
            {
                // CPF/CNPJ cadastrado fora do padrão (11/14 dígitos) — mantém o cliente sem o documento em vez de quebrar a busca.
            }

            result.Add(new Cliente
            {
                Id = rd.GetInt32(0),
                CodCliente = rd.IsDBNull(1) ? null : rd.GetInt32(1).ToString(),
                Nome = rd.GetString(2),
                CpfCnpj = documento,
            });
        }
        return result;
    }
}
