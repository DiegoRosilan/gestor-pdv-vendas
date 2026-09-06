using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>Orçamento (F10 na tela de venda) — ainda não implementado no PDV; entidade preparada pra quando entrar em escopo.</summary>
public sealed class Orcamento
{
    public int Id { get; init; }
    public DateTime Data { get; init; }
    public Cliente? Cliente { get; init; }
    public List<ItemVenda> Itens { get; init; } = new();
    public DateOnly? Validade { get; init; }

    public Dinheiro Total => new(Itens.Where(i => !i.Cancelado).Sum(i => i.Total.Valor));
}
