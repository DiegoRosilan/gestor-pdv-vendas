using System.Windows;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Vendas;

public partial class FechamentoVendaView : Window
{
    private readonly FechamentoVendaViewModel _viewModel;

    public FechamentoVendaView(FechamentoVendaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.VendaFinalizada += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private async void FechamentoVendaView_Loaded(object sender, RoutedEventArgs e)
        => await _viewModel.CarregarFormasPagamentoAsync();

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
