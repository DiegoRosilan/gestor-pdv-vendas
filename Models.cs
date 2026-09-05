namespace GestorPDV.Vendas;

public sealed record Funcionario(int Id, string Nome, string? Login, string? Senha, bool Vendedor, bool Caixa, bool Gerente);

public sealed record Caixa(int Id, string Nome);

public sealed record Turno(int Id, string Descricao, string? HoraInicio, string? HoraFim);

/// <summary>Dados exibidos na tela "Movimento aberto - confirme dados" ao logar num turno já aberto.</summary>
public sealed record MovimentoInfo(
    int Id, string Turno, string Caixa, string Impressora, DateTime? DataAbertura, string? HoraAbertura);

public sealed record Encerrante(string TipoPagamento, decimal Valor);

public sealed record Produto(
    int Id,
    string? CodProduto,
    string? CodigoInterno,
    string? Gtin,
    string Nome,
    decimal ValorVenda,
    decimal QtdEstoque,
    string UnidadeSigla,
    string? Localizacao);

public sealed record Cliente(int Id, string? CodCliente, string Nome, string? CpfCnpj);

public sealed record FormaPagamento(int Id, string Descricao, bool GeraParcelas);

/// <summary>Uma nota (venda finalizada) do movimento atual, exibida na lista de notas emitidas do Menu Fiscal (F8).</summary>
public sealed record NotaEmitidaInfo(int Id, int Coo, DateTime DataVenda, string? HoraVenda, decimal ValorFinal, string StatusVenda);

/// <summary>Um item já lançado no carrinho da venda em andamento.</summary>
public sealed class ItemCarrinho
{
    public required Produto Produto { get; init; }
    public decimal Quantidade { get; set; } = 1;
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }

    /// <summary>
    /// Item cancelado (F5) permanece na tela com uma tarja "CANCELADO" e
    /// tachado, mas some do total da venda e não baixa estoque — igual ao
    /// comportamento do sistema original (coluna "cancelado" em
    /// ecf_venda_detalhe já existia pra isso).
    /// </summary>
    public bool Cancelado { get; set; }

    public decimal Total => Math.Max(0m, (Quantidade * ValorUnitario) - Desconto);
}

/// <summary>Uma forma de pagamento já lançada para a venda em andamento (permite dividir o total entre várias).</summary>
public sealed class PagamentoLancado
{
    public required FormaPagamento Forma { get; init; }
    public decimal Valor { get; set; }
}
