using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>
/// Um item lançado numa venda. Cancelado (F5 na tela de venda) permanece
/// na lista — pra manter o histórico do cupom visível, com o valor
/// original ainda calculado em Total (a tela mostra tachado) — mas sai do
/// total GERAL da venda (ver Venda.SubtotalItens, que filtra Cancelado) e
/// não baixa estoque.
/// </summary>
public sealed class ItemVenda
{
    public required Produto Produto { get; init; }
    public decimal Quantidade { get; set; } = 1;
    public Dinheiro ValorUnitario { get; set; }
    public Dinheiro Desconto { get; set; }
    public bool Cancelado { get; private set; }

    public Dinheiro Total => new(Quantidade * ValorUnitario.Valor - Desconto.Valor);

    public void Cancelar() => Cancelado = true;
}
