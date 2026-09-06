using GestorPDV.Application.Interfaces;

namespace GestorPDV.Infrastructure.Printing;

/// <summary>
/// Implementação de IImpressaoService. No banco legado o próprio gatilho
/// trg_gestorpdv_venda_fechada (criado pelo GestorPDV.Monitor original)
/// já enfileira a venda pra impressão automaticamente assim que
/// ecf_venda_cabecalho é gravado com status_venda='F' — então gravar a
/// venda (VendaRepository.FinalizarAsync) e enfileirar são, na prática, a
/// mesma coisa nesse banco. Este método existe pra deixar a intenção
/// explícita no código e ter onde plugar uma fila própria depois, caso o
/// gatilho não esteja presente no banco de destino.
/// </summary>
public sealed class PrintQueueService : IImpressaoService
{
    public Task EnfileirarAsync(int idVenda, CancellationToken ct) => Task.CompletedTask;
}
