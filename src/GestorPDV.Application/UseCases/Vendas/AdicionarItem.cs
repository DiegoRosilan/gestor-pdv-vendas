using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.UseCases.Vendas;

/// <summary>
/// Busca o produto pelo código/código de barras/código interno (mesma
/// query de sempre) e lança (ou soma quantidade, se já lançado e não
/// cancelado) na venda em andamento — igual ao comportamento de
/// VendaForm.AdicionarProdutoAoCarrinho no legado. Se não achar por
/// código, tenta pelo nome: um resultado só já lança direto, mais de um
/// devolve os candidatos pra quem chamou decidir (normalmente abrindo a
/// busca já filtrada) — mesmo campo "Código/Descrição do Produto" da
/// tela de venda serve pros dois tipos de busca.
/// </summary>
public sealed class AdicionarItem
{
    private readonly IProdutoRepository _produtos;

    public AdicionarItem(IProdutoRepository produtos) => _produtos = produtos;

    public async Task<ResultadoAdicionarItem> ExecutarPorTermoAsync(Venda venda, string termo, CancellationToken ct)
    {
        var produto = await _produtos.BuscarPorCodigoAsync(termo, ct);
        if (produto is not null)
        {
            Executar(venda, produto);
            return ResultadoAdicionarItem.Adicionado(produto);
        }

        var candidatos = await _produtos.BuscarPorNomeAsync(termo, ct);
        if (candidatos.Count == 1)
        {
            Executar(venda, candidatos[0]);
            return ResultadoAdicionarItem.Adicionado(candidatos[0]);
        }
        if (candidatos.Count > 1)
            return ResultadoAdicionarItem.VariosEncontrados(candidatos);

        return ResultadoAdicionarItem.NenhumEncontrado();
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

public sealed class ResultadoAdicionarItem
{
    public Produto? ProdutoAdicionado { get; private init; }
    public IReadOnlyList<Produto> Candidatos { get; private init; } = Array.Empty<Produto>();

    public static ResultadoAdicionarItem Adicionado(Produto produto) => new() { ProdutoAdicionado = produto };
    public static ResultadoAdicionarItem VariosEncontrados(IReadOnlyList<Produto> candidatos) => new() { Candidatos = candidatos };
    public static ResultadoAdicionarItem NenhumEncontrado() => new();
}
