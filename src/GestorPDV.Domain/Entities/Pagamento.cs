using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>Uma forma de pagamento lançada numa venda (permite dividir o total entre várias).</summary>
public sealed class Pagamento
{
    public int IdFormaPagamento { get; init; }
    public required string Descricao { get; init; }
    public bool GeraParcelas { get; init; }
    public Dinheiro Valor { get; set; }
}
