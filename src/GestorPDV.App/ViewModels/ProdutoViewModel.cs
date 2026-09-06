using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.DTOs;
using GestorPDV.Application.Services;

namespace GestorPDV.App.ViewModels;

/// <summary>ConsultaProdutosView (F2 na tela de venda) — porta ProdutoSearchForm do legado.</summary>
public sealed class ProdutoViewModel : ViewModelBase
{
    private readonly ProdutoService _produtoService;
    private string _termo = "";
    private ProdutoDto? _produtoSelecionado;

    public ProdutoViewModel(ProdutoService produtoService)
    {
        _produtoService = produtoService;
        BuscarCommand = RelayCommand.CreateAsync(_ => BuscarAsync());
    }

    public string Termo { get => _termo; set => SetProperty(ref _termo, value); }
    public ObservableCollection<ProdutoDto> Resultados { get; } = new();
    public ProdutoDto? ProdutoSelecionado { get => _produtoSelecionado; set => SetProperty(ref _produtoSelecionado, value); }

    public ICommand BuscarCommand { get; }

    private async Task BuscarAsync()
    {
        var resultados = await _produtoService.BuscarPorNomeAsync(Termo.Trim(), CancellationToken.None);
        Resultados.Clear();
        foreach (var produto in resultados)
            Resultados.Add(produto);
    }
}
