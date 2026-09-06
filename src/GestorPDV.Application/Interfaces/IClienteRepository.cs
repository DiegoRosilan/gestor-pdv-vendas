using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.Interfaces;

public interface IClienteRepository
{
    Task<List<Cliente>> BuscarAsync(string termo, CancellationToken ct);
}
