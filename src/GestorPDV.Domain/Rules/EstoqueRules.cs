using GestorPDV.Domain.Entities;

namespace GestorPDV.Domain.Rules;

public static class EstoqueRules
{
    /// <summary>
    /// O PDV legado nunca bloqueou venda por falta de estoque (só debitava,
    /// podendo ficar negativo) — este método existe pra quando essa regra
    /// for decidida; por padrão só informa, não impede.
    /// </summary>
    public static bool TemEstoqueSuficiente(Produto produto, decimal quantidade) => produto.TemEstoque(quantidade);
}
