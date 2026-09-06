using GestorPDV.Domain.Entities;
using GestorPDV.Domain.ValueObjects;
using Xunit;

namespace GestorPDV.Domain.Tests;

public class VendaTests
{
    private static Produto CriarProduto(decimal valorVenda) => new()
    {
        Id = 1,
        Nome = "Produto Teste",
        ValorVenda = new Dinheiro(valorVenda),
    };

    [Fact]
    public void ItemCancelado_NaoEntraNoSubtotal()
    {
        var venda = new Venda();
        venda.AdicionarItem(new ItemVenda { Produto = CriarProduto(10m), Quantidade = 2 }); // 20,00
        venda.AdicionarItem(new ItemVenda { Produto = CriarProduto(5m), Quantidade = 1 });  // 5,00

        venda.CancelarItem(1); // cancela o de 5,00

        Assert.Equal(20m, venda.SubtotalItens.Valor);
    }

    [Fact]
    public void ItemCancelado_MantemValorOriginalParaExibicao()
    {
        var venda = new Venda();
        venda.AdicionarItem(new ItemVenda { Produto = CriarProduto(10m), Quantidade = 2 });

        venda.CancelarItem(0);

        // Total do item continua calculado (a tela mostra tachado) — só o
        // SubtotalItens/Total da venda é que exclui itens cancelados.
        Assert.Equal(20m, venda.Itens[0].Total.Valor);
        Assert.Equal(0m, venda.SubtotalItens.Valor);
    }

    [Fact]
    public void Total_AplicaDescontoEAcrescimoGeral_NuncaNegativo()
    {
        var venda = new Venda { DescontoGeral = new Dinheiro(100m) };
        venda.AdicionarItem(new ItemVenda { Produto = CriarProduto(10m), Quantidade = 1 });

        Assert.Equal(0m, venda.Total.Valor);
    }
}
