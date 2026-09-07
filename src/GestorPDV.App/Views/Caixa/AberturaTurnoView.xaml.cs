using System.Windows;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Caixa;

public partial class AberturaTurnoView : Window
{
    public AberturaTurnoView(AberturaTurnoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.TurnoAberto += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
        Loaded += async (_, _) => await viewModel.CarregarAsync();
    }

    /// <summary>
    /// PasswordBox.Password não é DependencyProperty (por segurança, não
    /// dá pra fazer binding direto) — por isso repassa pro ViewModel aqui
    /// no code-behind, e só aqui (mesmo padrão de LoginView).
    /// </summary>
    private void CaixaSenhaGerente_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AberturaTurnoViewModel viewModel)
            viewModel.SenhaGerente = CaixaSenhaGerente.Password;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
