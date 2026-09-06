using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Interfaces;
using GestorPDV.Application.Services;
using GestorPDV.Domain.Entities;
using GestorPDV.Domain.ValueObjects;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// FechamentoVendaView (F3 na tela de venda) — porta FechaVendaForm do
/// legado: desconto/acréscimo geral, lançamento de forma(s) de pagamento
/// e finalização. Some.CobrePagamentoTotal decide se F12/Finaliza libera.
/// </summary>
public sealed class FechamentoVendaViewModel : ViewModelBase
{
    private readonly VendaService _vendaService;
    private readonly Venda _venda;
    private FormaPagamentoInfo? _formaSelecionada;
    private decimal _valorPagamento;
    private string _mensagem = "";

    public FechamentoVendaViewModel(VendaService vendaService, Venda venda)
    {
        _vendaService = vendaService;
        _venda = venda;

        LancarPagamentoCommand = RelayCommand.Create(_ => LancarPagamento(), _ => FormaSelecionada is not null && ValorPagamento > 0);
        FinalizarCommand = RelayCommand.CreateAsync(_ => FinalizarAsync());
    }

    public ObservableCollection<FormaPagamentoInfo> FormasDisponiveis { get; } = new();
    public ObservableCollection<Pagamento> PagamentosLancados { get; } = new();

    public FormaPagamentoInfo? FormaSelecionada { get => _formaSelecionada; set => SetProperty(ref _formaSelecionada, value); }
    public decimal ValorPagamento { get => _valorPagamento; set => SetProperty(ref _valorPagamento, value); }
    public string Mensagem { get => _mensagem; set => SetProperty(ref _mensagem, value); }

    public string TotalVenda => _venda.Total.ToString();
    public string TotalRecebido => _venda.TotalRecebido.ToString();
    public string Troco => _venda.Troco.ToString();

    public ICommand LancarPagamentoCommand { get; }
    public ICommand FinalizarCommand { get; }

    /// <summary>A View fecha o diálogo (DialogResult=true) quando isso disparar.</summary>
    public event EventHandler? VendaFinalizada;

    /// <summary>Chamado pela View no OnLoaded, já que carregar formas de pagamento é assíncrono.</summary>
    public async Task CarregarFormasPagamentoAsync()
    {
        var formas = await _vendaService.GetFormasPagamentoAsync(CancellationToken.None);
        FormasDisponiveis.Clear();
        foreach (var forma in formas)
            FormasDisponiveis.Add(forma);
    }

    private void LancarPagamento()
    {
        if (FormaSelecionada is null || ValorPagamento <= 0)
            return;

        var pagamento = new Pagamento
        {
            IdFormaPagamento = FormaSelecionada.Id,
            Descricao = FormaSelecionada.Descricao,
            GeraParcelas = FormaSelecionada.GeraParcelas,
            Valor = new Dinheiro(ValorPagamento),
        };
        _venda.LancarPagamento(pagamento);
        PagamentosLancados.Add(pagamento);
        ValorPagamento = 0;

        OnPropertyChanged(nameof(TotalRecebido));
        OnPropertyChanged(nameof(Troco));
    }

    private async Task FinalizarAsync()
    {
        Mensagem = "";
        try
        {
            await _vendaService.FinalizarAsync(_venda, CancellationToken.None);
            VendaFinalizada?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Mensagem = ex.Message;
        }
    }
}
