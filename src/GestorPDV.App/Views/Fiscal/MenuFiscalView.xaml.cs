using System.Windows;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Fiscal;

public partial class MenuFiscalView : Window
{
    public MenuFiscalView(MenuFiscalViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.CarregarAsync();
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
