using System.Windows;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Autorizacao;

public partial class SenhaGerenteView : Window
{
    public SenhaGerenteView(SenhaGerenteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Autorizado += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void CaixaSenha_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SenhaGerenteViewModel viewModel)
            viewModel.Senha = CaixaSenha.Password;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
