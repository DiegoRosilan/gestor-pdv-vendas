using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

public sealed class Cliente
{
    public int Id { get; init; }
    public string? CodCliente { get; init; }
    public required string Nome { get; init; }
    public Documento? CpfCnpj { get; init; }
    public bool Ativo { get; init; } = true;
}
