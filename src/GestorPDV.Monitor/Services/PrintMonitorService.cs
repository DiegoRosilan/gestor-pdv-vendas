namespace GestorPDV.Monitor.Services;

/// <summary>Loop de polling — chama PrintQueueWorker a cada intervalo até cancelar (Ctrl+C).</summary>
public sealed class PrintMonitorService
{
    private readonly PrintQueueWorker _worker;
    private readonly TimeSpan _intervalo;

    public PrintMonitorService(PrintQueueWorker worker, TimeSpan intervalo)
    {
        _worker = worker;
        _intervalo = intervalo;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _worker.ProcessarPendentesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"[GestorPDV.Monitor] Erro ao processar fila: {ex.Message}");
            }

            try
            {
                await Task.Delay(_intervalo, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
