namespace GestorPDV.Application.Interfaces;

/// <summary>Porta pra Infrastructure/Printing — enfileirar um documento pra impressão (o próprio GestorPDV.Monitor processa a fila).</summary>
public interface IImpressaoService
{
    Task EnfileirarAsync(int idVenda, CancellationToken ct);
}
