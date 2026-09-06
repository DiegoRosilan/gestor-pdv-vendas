using GestorPDV.Domain.Entities;
using GestorPDV.Domain.Rules;

namespace GestorPDV.Application.Services;

/// <summary>
/// Hoje só expõe a regra de disponibilidade (Domain.Rules.EstoqueRules) —
/// o PDV legado nunca bloqueou venda por estoque insuficiente, só avisa.
/// </summary>
public sealed class EstoqueService
{
    public bool TemEstoqueSuficiente(Produto produto, decimal quantidade) => EstoqueRules.TemEstoqueSuficiente(produto, quantidade);
}
