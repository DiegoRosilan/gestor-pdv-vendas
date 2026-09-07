using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestorPDV.App.ViewModels;
using GestorPDV.App.Views.Fiscal;
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
        Unloaded += (_, _) => _viewModel.Dispose();
    }

    private void CaixaCodigo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (_viewModel.AdicionarPorCodigoCommand.CanExecute(null))
            _viewModel.AdicionarPorCodigoCommand.Execute(null);
    }

    // ---- Barra de funções F1-F12 (clique e tecla chamam os mesmos métodos, igual a PdvTheme.CriarBarraFuncoes no legado) ----

    private void VendaView_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1:
                PopupOpcoes.IsOpen = !PopupOpcoes.IsOpen;
                e.Handled = true;
                break;
            case Key.F2:
                AbrirBuscaProduto();
                e.Handled = true;
                break;
            case Key.F3:
                FecharVenda();
                e.Handled = true;
                break;
            case Key.F4:
                if (_viewModel.CancelarCupomCommand.CanExecute(null))
                    _viewModel.CancelarCupomCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F5:
                if (_viewModel.CancelarItemCommand.CanExecute(null))
                    _viewModel.CancelarItemCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F7:
                IdentificarCliente();
                e.Handled = true;
                break;
            case Key.F8:
                AbrirMenuFiscal();
                e.Handled = true;
                break;
            case Key.F12:
                Sair();
                e.Handled = true;
                break;
        }
    }

    private void AbrirMenuOpcoes_Click(object sender, RoutedEventArgs e)
    {
        PopupOpcoes.IsOpen = !PopupOpcoes.IsOpen;
    }

    private void AbrirBuscaProduto_Click(object sender, RoutedEventArgs e) => AbrirBuscaProduto();

    private void AbrirBuscaProduto()
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

    private void FecharVenda_Click(object sender, RoutedEventArgs e) => FecharVenda();

    private void FecharVenda()
    {
        var fechamentoViewModel = new FechamentoVendaViewModel(_vendaService, _viewModel.VendaAtual);
        var janela = new FechamentoVendaView(fechamentoViewModel) { Owner = Window.GetWindow(this) };
        janela.ShowDialog();
    }

    private void IdentificarCliente_Click(object sender, RoutedEventArgs e) => IdentificarCliente();

    // Identificar cliente (F7) ainda não tem View WPF (ver Views/Clientes) — mesma transparência do flyout F1.
    private void IdentificarCliente()
    {
        MessageBox.Show(
            "Identificar cliente (F7) ainda não foi portado pra esta versão WPF.",
            "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AbrirMenuFiscal_Click(object sender, RoutedEventArgs e) => AbrirMenuFiscal();

    private void AbrirMenuFiscal()
    {
        var menuFiscalViewModel = new MenuFiscalViewModel(_vendaService, _viewModel.IdMovimento);
        var janela = new MenuFiscalView(menuFiscalViewModel) { Owner = Window.GetWindow(this) };
        janela.ShowDialog();
    }

    private void Sair_Click(object sender, RoutedEventArgs e) => Sair();

    /// <summary>
    /// Encerrar o turno antes de sair (EncerraCaixaForm no legado) ainda
    /// não tem View WPF — aqui só fecha a janela principal, exigindo que a
    /// venda em andamento já esteja fechada ou cancelada.
    /// </summary>
    private void Sair()
    {
        if (_viewModel.Itens.Any(i => !i.Cancelado))
        {
            MessageBox.Show("Finalize ou cancele a venda em andamento antes de sair.",
                "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var resultado = MessageBox.Show("Deseja realmente sair do GestorPDV?",
            "GestorPDV", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (resultado == MessageBoxResult.Yes)
            Window.GetWindow(this)?.Close();
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
