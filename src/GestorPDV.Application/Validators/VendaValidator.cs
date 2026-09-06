using GestorPDV.Domain.Entities;
using GestorPDV.Domain.Rules;

namespace GestorPDV.Application.Validators;

/// <summary>
/// Validação de entrada antes de chamar FinalizarVenda — separada das
/// regras de negócio em si (Domain.Rules), que continuam sendo a fonte
/// da verdade; isto aqui só traduz o resultado em mensagens pra tela.
/// </summary>
public static class VendaValidator
{
    public static IReadOnlyList<string> Validar(Venda venda)
    {
        var erros = new List<string>();

        if (!VendaRules.PodeFecharVenda(venda))
            erros.Add("Adicione ao menos um item (não cancelado) antes de fechar a venda.");

        if (!PagamentoRules.CobrePagamentoTotal(venda.Pagamentos, venda.Total))
            erros.Add("Lance forma(s) de pagamento que cubram o total a receber.");

        return erros;
    }
}
