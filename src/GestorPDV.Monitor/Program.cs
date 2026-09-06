using GestorPDV.Infrastructure.Printing;
using GestorPDV.Infrastructure.Reports;
using GestorPDV.Monitor.Services;
using Microsoft.Extensions.Configuration;

namespace GestorPDV.Monitor;

/// <summary>Serviço à parte do GestorPDV.App — observa a fila de impressão e processa vendas fechadas, mesmo com o PDV fechado.</summary>
internal static class Program
{
    private static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _ = configuration["ConnectionString"]
            ?? throw new InvalidOperationException("ConnectionString não configurada em appsettings.json.");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var worker = new PrintQueueWorker(new FastReportService(), new PrinterService());
        var monitor = new PrintMonitorService(worker, TimeSpan.FromSeconds(10));

        Console.WriteLine("GestorPDV.Monitor rodando — Ctrl+C para sair.");
        await monitor.ExecutarAsync(cts.Token);
    }
}
