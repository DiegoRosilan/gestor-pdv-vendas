using GestorPDV.Application.Interfaces;
using GestorPDV.Domain.Entities;
using GestorPDV.Domain.Exceptions;
using GestorPDV.Domain.Rules;

namespace GestorPDV.Application.UseCases.Vendas;

/// <summary>Valida (mesmas regras de FechaVendaForm.Finalizar_Click no legado) e grava a venda.</summary>
public sealed class FinalizarVenda
{
    private readonly IVendaRepository _vendas;

    public FinalizarVenda(IVendaRepository vendas) => _vendas = vendas;

    public async Task ExecutarAsync(Venda venda, CancellationToken ct)
    {
        if (!VendaRules.PodeFecharVenda(venda))
            throw new DomainException("Adicione ao menos um item (não cancelado) antes de fechar a venda.");

        if (!PagamentoRules.CobrePagamentoTotal(venda.Pagamentos, venda.Total))
            throw new DomainException("Lance forma(s) de pagamento que cubram o total a receber.");

        var (id, coo) = await _vendas.FinalizarAsync(venda, ct);
        venda.Finalizar(id, coo);
    }
}
