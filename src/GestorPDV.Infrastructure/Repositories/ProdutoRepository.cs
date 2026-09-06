using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;
using GestorPDV.Domain.ValueObjects;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Infrastructure.Repositories;

/// <summary>Portado de legacy/GestorPDV.Vendas/Database.cs (consultas já validadas contra o banco real na época).</summary>
public sealed class ProdutoRepository : IProdutoRepository
{
    private readonly PostgreSqlConnectionFactory _connectionFactory;

    public ProdutoRepository(PostgreSqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<Produto?> BuscarPorCodigoAsync(string codigo, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = """
        SELECT p.id, p.cod_produto, p.codigo_interno, p.gtin, p.nome,
               p.valor_venda, p.qtd_estoque, u.sigla, p.localizacao, p.inativo
        FROM public.produto p
        LEFT JOIN public.unidade_produto u ON u.id = p.id_unidade_produto
        WHERE p.produto_pdv = 'S' AND p.inativo = 'N'
          AND (p.cod_produto::text = @c OR p.codigo_interno = @c OR p.gtin = @c)
        ORDER BY p.id LIMIT 1;
        """;
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("c", codigo);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        return await rd.ReadAsync(ct) ? Mapear(rd) : null;
    }

    public async Task<List<Produto>> BuscarPorNomeAsync(string termo, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = """
        SELECT p.id, p.cod_produto, p.codigo_interno, p.gtin, p.nome,
               p.valor_venda, p.qtd_estoque, u.sigla, p.localizacao, p.inativo
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
            result.Add(Mapear(rd));
        return result;
    }

    public async Task DebitarEstoqueAsync(int idProduto, decimal quantidade, CancellationToken ct)
    {
        await using var cn = await _connectionFactory.AbrirAsync(ct);
        const string sql = "UPDATE public.produto SET qtd_estoque = qtd_estoque - @qtd WHERE id = @id;";
        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("qtd", quantidade);
        cmd.Parameters.AddWithValue("id", idProduto);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static Produto Mapear(NpgsqlDataReader rd) => new()
    {
        Id = rd.GetInt32(0),
        CodProduto = rd.IsDBNull(1) ? null : rd.GetInt32(1).ToString(),
        CodigoInterno = rd.IsDBNull(2) ? null : rd.GetString(2),
        Gtin = rd.IsDBNull(3) ? null : rd.GetString(3),
        Nome = rd.GetString(4),
        ValorVenda = new Dinheiro(rd.IsDBNull(5) ? 0m : rd.GetDecimal(5)),
        QtdEstoque = rd.IsDBNull(6) ? 0m : rd.GetDecimal(6),
        UnidadeSigla = rd.IsDBNull(7) ? "UN" : rd.GetString(7),
        Localizacao = rd.IsDBNull(8) ? null : rd.GetString(8),
        Inativo = !rd.IsDBNull(9) && rd.GetString(9) == "S",
    };
}
