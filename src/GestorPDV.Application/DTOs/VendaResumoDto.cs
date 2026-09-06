namespace GestorPDV.Application.DTOs;

/// <summary>Resumo de uma venda finalizada — usado na lista de notas emitidas (Menu Fiscal / F8).</summary>
public sealed record VendaResumoDto(int Id, int Coo, DateTime Data, string? Hora, decimal ValorFinal, string Situacao);
