namespace GestorPDV.Domain.Enums;

/// <summary>
/// Categoria geral de uma forma de pagamento. As formas de pagamento
/// concretas continuam cadastradas dinamicamente no banco
/// (ecf_tipo_pagamento) — esta categoria é só pra regras de negócio que
/// dependem do tipo (ex.: crediário gera parcelas).
/// </summary>
public enum TipoPagamento
{
    Dinheiro,
    Cartao,
    Pix,
    Crediario,
    Cheque,
    Outro,
}
