using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.UseCases.Vendas;

/// <summary>Começa um cupom novo em memória — nada é gravado no banco até FinalizarVenda.</summary>
public sealed class AbrirVenda
{
    public Venda Executar(int idMovimento, int idFuncionario, int idOperador) => new()
    {
        IdMovimento = idMovimento,
        IdFuncionario = idFuncionario,
        IdOperador = idOperador,
    };
}
