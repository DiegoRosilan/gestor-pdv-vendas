using GestorPDV.Application.Interfaces;

namespace GestorPDV.Application.Services;

/// <summary>Fachada exposta à UI — a implementação real (fila/impressora) está em Infrastructure.Printing via IImpressaoService.</summary>
public sealed class ImpressaoService
{
    private readonly IImpressaoService _impressao;

    public ImpressaoService(IImpressaoService impressao) => _impressao = impressao;

    public Task EnfileirarAsync(int idVenda, CancellationToken ct) => _impressao.EnfileirarAsync(idVenda, ct);
}
