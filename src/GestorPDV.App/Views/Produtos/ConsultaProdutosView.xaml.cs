using System.Windows;
using System.Windows.Input;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Produtos;

public partial class ConsultaProdutosView : Window
{
    private readonly ProdutoViewModel _viewModel;

    public ConsultaProdutosView(ProdutoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Termo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.BuscarCommand.CanExecute(null))
            _viewModel.BuscarCommand.Execute(null);
    }

    private void Resultados_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Selecionar_Click(sender, e);

    private void Selecionar_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ProdutoSelecionado is null)
            return;

        DialogResult = true;
        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
