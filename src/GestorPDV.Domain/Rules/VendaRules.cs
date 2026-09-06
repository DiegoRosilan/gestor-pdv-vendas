using GestorPDV.Domain.Entities;

namespace GestorPDV.Domain.Rules;

public static class VendaRules
{
    /// <summary>Precisa de ao menos um item não cancelado — igual à validação que já existia em VendaForm.FecharVenda.</summary>
    public static bool PodeFecharVenda(Venda venda) => venda.Itens.Any(i => !i.Cancelado);

    /// <summary>Um item já cancelado não pode ser cancelado de novo nem editado (F5/duplo clique já bloqueavam isso na tela).</summary>
    public static bool PodeCancelarItem(ItemVenda item) => !item.Cancelado;
}
