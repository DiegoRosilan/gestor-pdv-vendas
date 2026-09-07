using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using GestorPDV.Application.DTOs;
using GestorPDV.Application.Services;
using GestorPDV.Domain.Entities;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// Tela de venda — porta o comportamento de VendaForm.cs (legacy) pra
/// MVVM: item cancelado (F5) fica na lista (tachado/tarja "CANCELADO" —
/// ver ItemVendaDto.Cancelado, o binding de estilo fica na VendaView) e
/// some do total; F4 descarta o cupom inteiro; F3 abre o fechamento
/// (FechamentoVendaViewModel), que ainda precisa ser aberto pela View já
/// que ela quem sabe mostrar o diálogo modal.
/// </summary>
public sealed class VendaViewModel : ViewModelBase
{
    private readonly VendaService _vendaService;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private Venda _venda;
    private string _codigo = "";
    private string _mensagem = "";
    private ItemVendaDto? _itemSelecionado;

    public VendaViewModel(VendaService vendaService, int idMovimento, int idFuncionario, int idOperador)
    {
        _vendaService = vendaService;
        _venda = _vendaService.NovaVenda(idMovimento, idFuncionario, idOperador);

        AdicionarPorCodigoCommand = RelayCommand.CreateAsync(_ => AdicionarPorCodigoAsync(), _ => !string.IsNullOrWhiteSpace(Codigo));
        CancelarItemCommand = RelayCommand.Create(_ => CancelarItemSelecionado(), _ => ItemSelecionado is { Cancelado: false });
        CancelarCupomCommand = RelayCommand.Create(_ => CancelarCupom());
    }

    public ObservableCollection<ItemVendaDto> Itens { get; } = new();

    public string Codigo { get => _codigo; set => SetProperty(ref _codigo, value); }
    public string Mensagem { get => _mensagem; set => SetProperty(ref _mensagem, value); }
    public string TotalGeral => _venda.Total.ToString();

    public ItemVendaDto? ItemSelecionado { get => _itemSelecionado; set => SetProperty(ref _itemSelecionado, value); }

    public ICommand AdicionarPorCodigoCommand { get; }
    public ICommand CancelarItemCommand { get; }
    public ICommand CancelarCupomCommand { get; }

    /// <summary>Exposto pra FechamentoVendaViewModel/VendaView montarem o fechamento (F3) sem essa ViewModel precisar conhecer a tela de fechamento.</summary>
    public Venda VendaAtual => _venda;

    /// <summary>Mesmo movimento em toda a vida da ViewModel (CancelarCupom preserva IdMovimento) — exposto pra VendaView montar o Menu Fiscal (F8).</summary>
    public int IdMovimento => _venda.IdMovimento;

    private async Task AdicionarPorCodigoAsync()
    {
        Mensagem = "";
        var codigo = Codigo.Trim();
        if (codigo.Length == 0)
            return;

        var produto = await _vendaService.AdicionarItemPorCodigoAsync(_venda, codigo, CancellationToken.None);
        if (produto is null)
        {
            Mensagem = $"Nenhum produto encontrado com o código \"{codigo}\".";
            return;
        }

        Codigo = "";
        AtualizarLista();
    }

    private void CancelarItemSelecionado()
    {
        if (ItemSelecionado is null)
            return;

        var indice = ItemSelecionado.NumeroItem - 1;
        _venda.CancelarItem(indice);
        AtualizarLista();
    }

    private void CancelarCupom()
    {
        _venda = new Venda { IdMovimento = _venda.IdMovimento, IdFuncionario = _venda.IdFuncionario, IdOperador = _venda.IdOperador };
        AtualizarLista();
    }

    private void AtualizarLista()
    {
        Itens.Clear();
        var numero = 1;
        foreach (var item in _venda.Itens)
        {
            var codigo = item.Produto.CodProduto ?? item.Produto.CodigoInterno ?? item.Produto.Id.ToString();
            Itens.Add(new ItemVendaDto(numero, codigo, item.Produto.Nome, item.Quantidade, item.ValorUnitario.Valor, item.Desconto.Valor, item.Total.Valor, item.Cancelado));
            numero++;
        }

        OnPropertyChanged(nameof(TotalGeral));
    }
}
