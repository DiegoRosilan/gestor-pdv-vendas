using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>Pré-venda (F9 na tela de venda) — ainda não implementada no PDV; entidade preparada pra quando entrar em escopo.</summary>
public sealed class PreVenda
{
    public int Id { get; init; }
    public DateTime Data { get; init; }
    public Cliente? Cliente { get; init; }
    public List<ItemVenda> Itens { get; init; } = new();

    public Dinheiro Total => new(Itens.Where(i => !i.Cancelado).Sum(i => i.Total.Valor));
}
