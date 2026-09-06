using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.Domain.Entities;

/// <summary>Espelha as colunas já validadas da tabela "produto" no banco legado.</summary>
public sealed class Produto
{
    public int Id { get; init; }
    public string? CodProduto { get; init; }
    public string? CodigoInterno { get; init; }
    public string? Gtin { get; init; }
    public required string Nome { get; init; }
    public Dinheiro ValorVenda { get; init; }
    public decimal QtdEstoque { get; set; }
    public string UnidadeSigla { get; init; } = "UN";
    public string? Localizacao { get; init; }
    public bool Inativo { get; init; }

    public bool TemEstoque(decimal quantidade) => QtdEstoque >= quantidade;
}
