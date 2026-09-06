namespace GestorPDV.Infrastructure.Printing;

/// <summary>
/// Envio pra impressora física (não fiscal — ver Fiscal/ pra SAT/NFC-e,
/// fora de escopo nesta versão). NÃO implementado ainda: precisa saber
/// qual driver/protocolo a impressora de balcão do cliente usa (ESC/POS
/// via porta serial/USB, IP, etc.) — isso varia por instalação e não dá
/// pra decidir sem essa informação.
/// </summary>
public sealed class PrinterService
{
    public Task ImprimirAsync(byte[] conteudo, CancellationToken ct)
        => throw new NotImplementedException(
            "Impressão física ainda não implementada — falta saber o modelo/protocolo da impressora de destino.");
}
