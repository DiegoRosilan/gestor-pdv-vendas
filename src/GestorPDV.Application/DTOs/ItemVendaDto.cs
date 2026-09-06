namespace GestorPDV.Application.DTOs;

/// <summary>Projeção de ItemVenda pra exibir no grid da tela de venda (linha tachada + tarja "CANCELADO" quando Cancelado=true).</summary>
public sealed record ItemVendaDto(int NumeroItem, string Codigo, string Descricao, decimal Quantidade, decimal ValorUnitario, decimal Desconto, decimal Total, bool Cancelado);
