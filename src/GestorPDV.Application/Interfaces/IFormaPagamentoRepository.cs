namespace GestorPDV.Application.Interfaces;

public interface IFormaPagamentoRepository
{
    Task<List<FormaPagamentoInfo>> GetTodasAsync(CancellationToken ct);
}

public sealed record FormaPagamentoInfo(int Id, string Descricao, bool GeraParcelas);
