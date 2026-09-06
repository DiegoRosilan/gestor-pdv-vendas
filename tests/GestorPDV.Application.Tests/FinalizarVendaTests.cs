using GestorPDV.Application.Interfaces;
using GestorPDV.Application.UseCases.Vendas;
using GestorPDV.Domain.Entities;
using GestorPDV.Domain.Exceptions;
using Moq;
using Xunit;

namespace GestorPDV.Application.Tests;

public class FinalizarVendaTests
{
    [Fact]
    public async Task ExecutarAsync_SemItens_LancaDomainException()
    {
        var vendasMock = new Mock<IVendaRepository>();
        var useCase = new FinalizarVenda(vendasMock.Object);
        var venda = new Venda();

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecutarAsync(venda, CancellationToken.None));
        vendasMock.Verify(v => v.FinalizarAsync(It.IsAny<Venda>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_PagamentoNaoCobreTotal_LancaDomainException()
    {
        var vendasMock = new Mock<IVendaRepository>();
        var useCase = new FinalizarVenda(vendasMock.Object);
        var venda = new Venda();
        venda.AdicionarItem(new ItemVenda
        {
            Produto = new Produto { Id = 1, Nome = "Produto Teste", ValorVenda = new(10m) },
            Quantidade = 1,
        });
        // Nenhum pagamento lançado — total a receber (10,00) não é coberto.

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecutarAsync(venda, CancellationToken.None));
    }
}
