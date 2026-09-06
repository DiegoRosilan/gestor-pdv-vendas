using GestorPDV.Application.DTOs;
using GestorPDV.Application.Interfaces;

namespace GestorPDV.Application.Services;

public sealed class ClienteService
{
    private readonly IClienteRepository _clientes;

    public ClienteService(IClienteRepository clientes) => _clientes = clientes;

    public async Task<List<ClienteDto>> BuscarAsync(string termo, CancellationToken ct)
    {
        var clientes = await _clientes.BuscarAsync(termo, ct);
        return clientes.Select(c => new ClienteDto(c.Id, c.Nome, c.CpfCnpj?.ToString())).ToList();
    }
}
