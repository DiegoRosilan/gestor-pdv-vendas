using GestorPDV.Infrastructure.Printing;
using GestorPDV.Infrastructure.Reports;

namespace GestorPDV.Monitor.Services;

/// <summary>
/// Processa um lote de documentos pendentes: gera o relatório
/// (FastReportService) e envia pra impressora (PrinterService). Nenhum
/// dos dois está implementado ainda (ver os comentários nessas classes em
/// GestorPDV.Infrastructure) — por isso este worker só registra o erro e
/// segue, em vez de derrubar o processo inteiro a cada tentativa.
/// </summary>
public sealed class PrintQueueWorker
{
    private readonly FastReportService _relatorios;
    private readonly PrinterService _impressora;

    public PrintQueueWorker(FastReportService relatorios, PrinterService impressora)
    {
        _relatorios = relatorios;
        _impressora = impressora;
    }

    public async Task ProcessarPendentesAsync(CancellationToken ct)
    {
        // TODO: consultar a fila real de impressão (a mesma que o
        // gatilho trg_gestorpdv_venda_fechada, citado em
        // legacy/GestorPDV.Vendas/Database.cs, alimenta) — ainda não
        // modelada nesta estrutura nova. Por enquanto este método não
        // tem o que processar.
        await Task.CompletedTask;
    }
}
