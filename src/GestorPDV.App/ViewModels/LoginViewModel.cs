using System.Windows.Input;
using GestorPDV.Application.Interfaces;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// Login do operador — mesma validação de ConfirmaLoginForm no legado
/// (login por Funcionario.Login, senha comparada em texto puro porque é
/// assim que o banco original guarda). A escolha de turno/terminal
/// (AberturaTurnoForm no legado) ainda não tem View correspondente nesta
/// estrutura nova — quando entrar em escopo, provavelmente como uma
/// segunda View chamada por MainViewModel antes de abrir a VendaView.
/// </summary>
public sealed class LoginViewModel : ViewModelBase
{
    private readonly IFuncionarioRepository _funcionarios;

    private string _login = "";
    private string _senha = "";
    private string? _mensagemErro;

    public LoginViewModel(IFuncionarioRepository funcionarios)
    {
        _funcionarios = funcionarios;
        ConfirmarCommand = RelayCommand.CreateAsync(_ => ConfirmarAsync(), _ => !string.IsNullOrWhiteSpace(Login));
    }

    public string Login { get => _login; set => SetProperty(ref _login, value); }
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }
    public string? MensagemErro { get => _mensagemErro; set => SetProperty(ref _mensagemErro, value); }

    public ICommand ConfirmarCommand { get; }

    /// <summary>Funcionário autenticado — Program.cs lê isso depois do ShowDialog pra decidir o que abrir em seguida.</summary>
    public FuncionarioInfo? FuncionarioLogado { get; private set; }

    public event EventHandler? LoginConfirmado;

    private async Task ConfirmarAsync()
    {
        MensagemErro = null;

        var funcionario = await _funcionarios.GetPorLoginAsync(Login, CancellationToken.None);
        if (funcionario is null)
        {
            MensagemErro = "Operador não encontrado.";
            return;
        }

        if (!string.IsNullOrEmpty(funcionario.Senha) && funcionario.Senha != Senha)
        {
            MensagemErro = "Senha incorreta.";
            return;
        }

        FuncionarioLogado = funcionario;
        LoginConfirmado?.Invoke(this, EventArgs.Empty);
    }
}
