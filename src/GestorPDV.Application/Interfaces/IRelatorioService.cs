namespace GestorPDV.Application.Interfaces;

/// <summary>Porta pra Infrastructure/Reports (FastReport) — gera o PDF/impressão de um documento a partir de um template .fr3.</summary>
public interface IRelatorioService
{
    Task<byte[]> GerarAsync(string nomeTemplate, IReadOnlyDictionary<string, object?> parametros, CancellationToken ct);
}
