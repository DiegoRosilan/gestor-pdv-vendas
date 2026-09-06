namespace GestorPDV.Application.DTOs;

/// <summary>Projeção de Produto pra grids de busca (ConsultaProdutosView) — não carrega o objeto de domínio inteiro pra só listar.</summary>
public sealed record ProdutoDto(int Id, string Codigo, string Nome, decimal ValorVenda, decimal QtdEstoque, string UnidadeSigla);
