using GestorPDV.Application.DTOs;
using GestorPDV.Application.Interfaces;

namespace GestorPDV.Application.Services;

public sealed class ProdutoService
{
    private readonly IProdutoRepository _produtos;

    public ProdutoService(IProdutoRepository produtos) => _produtos = produtos;

    public async Task<List<ProdutoDto>> BuscarPorNomeAsync(string termo, CancellationToken ct)
    {
        var produtos = await _produtos.BuscarPorNomeAsync(termo, ct);
        return produtos
            .Select(p => new ProdutoDto(p.Id, p.CodProduto ?? p.CodigoInterno ?? p.Id.ToString(), p.Nome, p.ValorVenda.Valor, p.QtdEstoque, p.UnidadeSigla))
            .ToList();
    }
}
