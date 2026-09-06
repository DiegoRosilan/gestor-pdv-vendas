using GestorPDV.Domain.Entities;
using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Rules;

public static class PagamentoRules
{
    /// <summary>Igual à validação de FechaVendaForm.Finalizar_Click: soma dos pagamentos precisa cobrir o total a receber.</summary>
    public static bool CobrePagamentoTotal(IReadOnlyList<Pagamento> pagamentos, Dinheiro total)
        => pagamentos.Sum(p => p.Valor.Valor) >= total.Valor;
}
