using GestorPDV.Domain.Entities;

namespace GestorPDV.Domain.Rules;

public static class ProdutoRules
{
    /// <summary>Espelha o filtro "produto_pdv = 'S' AND inativo = 'N'" já usado nas consultas do PDV legado.</summary>
    public static bool PodeVender(Produto produto) => !produto.Inativo;
}
