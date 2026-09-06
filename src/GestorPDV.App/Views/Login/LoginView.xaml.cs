using System.Windows;
using GestorPDV.App.ViewModels;

namespace GestorPDV.App.Views.Login;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoginConfirmado += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    /// <summary>
    /// PasswordBox.Password não é DependencyProperty (por segurança, não
    /// dá pra fazer binding direto) — por isso repassa pro ViewModel aqui
    /// no code-behind, e só aqui.
    /// </summary>
    private void CaixaSenha_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
            viewModel.Senha = CaixaSenha.Password;
    }
}
