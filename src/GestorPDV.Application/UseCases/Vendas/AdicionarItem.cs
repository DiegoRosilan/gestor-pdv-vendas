using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.UseCases.Vendas;

/// <summary>
/// Busca o produto pelo código e lança (ou soma quantidade, se já
/// lançado e não cancelado) na venda em andamento — igual ao
/// comportamento de VendaForm.AdicionarProdutoAoCarrinho no legado.
/// </summary>
public sealed class AdicionarItem
{
    private readonly IProdutoRepository _produtos;

    public AdicionarItem(IProdutoRepository produtos) => _produtos = produtos;

    public async Task<Produto?> ExecutarPorCodigoAsync(Venda venda, string codigo, CancellationToken ct)
    {
        var produto = await _produtos.BuscarPorCodigoAsync(codigo, ct);
        if (produto is null)
            return null;

        Executar(venda, produto);
        return produto;
    }

    public void Executar(Venda venda, Produto produto)
    {
        var existente = venda.Itens.FirstOrDefault(i => i.Produto.Id == produto.Id && !i.Cancelado);
        if (existente is not null)
        {
            existente.Quantidade += 1;
            return;
        }

        venda.AdicionarItem(new ItemVenda { Produto = produto, Quantidade = 1, ValorUnitario = produto.ValorVenda });
    }
}
