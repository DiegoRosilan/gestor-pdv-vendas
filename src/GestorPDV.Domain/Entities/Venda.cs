using GestorPDV.Domain.Enums;
using GestorPDV.Domain.Exceptions;
using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>
/// Aggregate root da venda em andamento — mesma regra que já estava em
/// VendaForm.cs (legacy/GestorPDV.Vendas): item cancelado fica na lista
/// mas sai do total; total geral = itens não cancelados - desconto geral
/// + acréscimo geral, nunca negativo.
/// </summary>
public sealed class Venda
{
    private readonly List<ItemVenda> _itens = new();
    private readonly List<Pagamento> _pagamentos = new();

    public int Id { get; private set; }
    public int Coo { get; private set; }
    public int IdMovimento { get; init; }
    public int IdFuncionario { get; init; }
    public int IdOperador { get; init; }
    public Cliente? Cliente { get; set; }
    public Dinheiro DescontoGeral { get; set; }
    public Dinheiro AcrescimoGeral { get; set; }
    public string? Observacao { get; set; }
    public StatusVenda Status { get; private set; } = StatusVenda.Aberta;

    public IReadOnlyList<ItemVenda> Itens => _itens;
    public IReadOnlyList<Pagamento> Pagamentos => _pagamentos;

    public void AdicionarItem(ItemVenda item) => _itens.Add(item);

    public void CancelarItem(int indice)
    {
        if (indice < 0 || indice >= _itens.Count)
            throw new DomainException("Item inválido para cancelamento.");
        _itens[indice].Cancelar();
    }

    public void LancarPagamento(Pagamento pagamento) => _pagamentos.Add(pagamento);

    public Dinheiro SubtotalItens => new(_itens.Where(i => !i.Cancelado).Sum(i => i.Total.Valor));

    public Dinheiro Total => new(SubtotalItens.Valor - DescontoGeral.Valor + AcrescimoGeral.Valor);

    public Dinheiro TotalRecebido => new(_pagamentos.Sum(p => p.Valor.Valor));

    public Dinheiro Troco => new(TotalRecebido.Valor - Total.Valor);

    /// <summary>Marca a venda como finalizada. Quem chama (UseCase) já validou via VendaRules/PagamentoRules antes.</summary>
    public void Finalizar(int id, int coo)
    {
        if (Status != StatusVenda.Aberta)
            throw new DomainException("Só é possível finalizar uma venda aberta.");
        Id = id;
        Coo = coo;
        Status = StatusVenda.Finalizada;
    }
}
