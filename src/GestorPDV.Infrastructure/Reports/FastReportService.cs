using GestorPDV.Application.Interfaces;

namespace GestorPDV.Infrastructure.Reports;

/// <summary>
/// Implementação de IRelatorioService usando FastReport (templates .fr3
/// em Reports/Templates/). NÃO implementado ainda: requer referenciar o
/// pacote FastReport.OpenSource (ou a licença comercial FastReport.Net,
/// se for o caso do cliente) e desenhar os .fr3 no FastReport Designer —
/// nenhum dos dois eu tenho como fazer sem acesso a essas ferramentas.
/// Ver Templates/README.md.
/// </summary>
public sealed class FastReportService : IRelatorioService
{
    public Task<byte[]> GerarAsync(string nomeTemplate, IReadOnlyDictionary<string, object?> parametros, CancellationToken ct)
        => throw new NotImplementedException(
            $"Geração de relatório \"{nomeTemplate}\" ainda não implementada — falta integrar o FastReport (pacote + templates .fr3).");
}
