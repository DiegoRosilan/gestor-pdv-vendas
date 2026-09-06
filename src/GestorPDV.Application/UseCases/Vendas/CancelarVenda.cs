using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.UseCases.Vendas;

/// <summary>F4 - Cancela Cupom: descarta o cupom inteiro em andamento (nada foi gravado ainda, então é só limpar em memória).</summary>
public sealed class CancelarVenda
{
    public Venda Executar() => new();
}
