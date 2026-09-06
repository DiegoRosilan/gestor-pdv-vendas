using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.Interfaces;

public interface IVendaRepository
{
    /// <summary>Grava a venda inteira numa transação (cabeçalho, itens, baixa de estoque, pagamentos) e retorna (id, coo). Espelha Database.FinalizarVendaAsync do legado.</summary>
    Task<(int Id, int Coo)> FinalizarAsync(Venda venda, CancellationToken ct);

    Task<List<NotaEmitidaInfo>> GetNotasEmitidasAsync(int idMovimento, CancellationToken ct);
}

public sealed record NotaEmitidaInfo(int Id, int Coo, DateTime DataVenda, string? HoraVenda, decimal ValorFinal, string StatusVenda);
