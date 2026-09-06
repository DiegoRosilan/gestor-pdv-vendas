using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.Interfaces;

public interface IProdutoRepository
{
    Task<Produto?> BuscarPorCodigoAsync(string codigo, CancellationToken ct);
    Task<List<Produto>> BuscarPorNomeAsync(string termo, CancellationToken ct);
    Task DebitarEstoqueAsync(int idProduto, decimal quantidade, CancellationToken ct);
}
