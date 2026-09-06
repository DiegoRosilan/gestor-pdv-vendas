using GestorPDV.Application.Interfaces;
using GestorPDV.Application.UseCases.Vendas;
using GestorPDV.Domain.Entities;

namespace GestorPDV.Application.Services;

/// <summary>
/// Fachada que a VendaViewModel chama — por trás, delega pros UseCases.
/// Sem estado próprio: quem guarda a Venda em andamento é o chamador
/// (a ViewModel), pra manter este serviço fácil de testar.
/// </summary>
public sealed class VendaService
{
    private readonly AbrirVenda _abrirVenda;
    private readonly AdicionarItem _adicionarItem;
    private readonly FinalizarVenda _finalizarVenda;
    private readonly CancelarVenda _cancelarVenda;
    private readonly IFormaPagamentoRepository _formasPagamento;
    private readonly IVendaRepository _vendas;

    public VendaService(
        AbrirVenda abrirVenda,
        AdicionarItem adicionarItem,
        FinalizarVenda finalizarVenda,
        CancelarVenda cancelarVenda,
        IFormaPagamentoRepository formasPagamento,
        IVendaRepository vendas)
    {
        _abrirVenda = abrirVenda;
        _adicionarItem = adicionarItem;
        _finalizarVenda = finalizarVenda;
        _cancelarVenda = cancelarVenda;
        _formasPagamento = formasPagamento;
        _vendas = vendas;
    }

    public Venda NovaVenda(int idMovimento, int idFuncionario, int idOperador) => _abrirVenda.Executar(idMovimento, idFuncionario, idOperador);

    public Task<GestorPDV.Domain.Entities.Produto?> AdicionarItemPorCodigoAsync(Venda venda, string codigo, CancellationToken ct)
        => _adicionarItem.ExecutarPorCodigoAsync(venda, codigo, ct);

    public void AdicionarItem(Venda venda, GestorPDV.Domain.Entities.Produto produto) => _adicionarItem.Executar(venda, produto);

    public Task FinalizarAsync(Venda venda, CancellationToken ct) => _finalizarVenda.ExecutarAsync(venda, ct);

    public Venda Cancelar() => _cancelarVenda.Executar();

    public Task<List<FormaPagamentoInfo>> GetFormasPagamentoAsync(CancellationToken ct) => _formasPagamento.GetTodasAsync(ct);

    public Task<List<NotaEmitidaInfo>> GetNotasEmitidasAsync(int idMovimento, CancellationToken ct) => _vendas.GetNotasEmitidasAsync(idMovimento, ct);
}
