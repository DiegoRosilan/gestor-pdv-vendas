using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestorPDV.App.ViewModels;
using GestorPDV.App.Views.Produtos;
using GestorPDV.Application.Services;

namespace GestorPDV.App.Views.Vendas;

public partial class VendaView : UserControl
{
    private readonly VendaViewModel _viewModel;
    private readonly VendaService _vendaService;
    private readonly ProdutoService _produtoService;

    public VendaView(VendaViewModel viewModel, VendaService vendaService, ProdutoService produtoService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _vendaService = vendaService;
        _produtoService = produtoService;
        DataContext = viewModel;
    }

    private void CaixaCodigo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (_viewModel.AdicionarPorCodigoCommand.CanExecute(null))
            _viewModel.AdicionarPorCodigoCommand.Execute(null);
    }

    private void AbrirBuscaProduto_Click(object sender, RoutedEventArgs e)
    {
        var produtoViewModel = new ProdutoViewModel(_produtoService);
        var janela = new ConsultaProdutosView(produtoViewModel) { Owner = Window.GetWindow(this) };
        if (janela.ShowDialog() != true || produtoViewModel.ProdutoSelecionado is not { } produtoDto)
            return;

        // ConsultaProdutosView devolve um DTO (ver ProdutoDto) — reaproveita a
        // busca por código já existente na ViewModel em vez de duplicar a
        // lógica de conversão DTO -> Produto de domínio aqui.
        _viewModel.Codigo = produtoDto.Codigo;
        if (_viewModel.AdicionarPorCodigoCommand.CanExecute(null))
            _viewModel.AdicionarPorCodigoCommand.Execute(null);
    }

    private void FecharVenda_Click(object sender, RoutedEventArgs e)
    {
        var fechamentoViewModel = new FechamentoVendaViewModel(_vendaService, _viewModel.VendaAtual);
        var janela = new FechamentoVendaView(fechamentoViewModel) { Owner = Window.GetWindow(this) };
        janela.ShowDialog();
    }

    private void VendaView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1) return;
        PopupOpcoes.IsOpen = !PopupOpcoes.IsOpen;
        e.Handled = true;
    }

    private void AbrirMenuOpcoes_Click(object sender, RoutedEventArgs e)
    {
        PopupOpcoes.IsOpen = !PopupOpcoes.IsOpen;
    }

    // Nenhuma das opções do menu F1 tem caso de uso implementado nesta
    // arquitetura ainda (mesma situação dos stubs em GestorPDV.Infrastructure)
    // — só avisa, em vez de fingir que a ação foi executada.
    private void OpcaoMenu_Click(object sender, RoutedEventArgs e)
    {
        PopupOpcoes.IsOpen = false;
        if (sender is not Button botao || botao.Tag is not string nomeOpcao) return;

        MessageBox.Show(
            $"\"{nomeOpcao}\" ainda não foi portado pra esta versão WPF.",
            "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
