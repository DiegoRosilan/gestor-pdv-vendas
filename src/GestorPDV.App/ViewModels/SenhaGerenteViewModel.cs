using System.Windows.Input;
using GestorPDV.Application.Interfaces;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// Diálogo de autorização gerencial reutilizável — usado hoje por
/// Cancelar Cupom (F4) e Cancelar Item (F5) na tela de venda, mesma
/// exigência de segurança da abertura de turno (AberturaTurnoView já
/// pede a mesma senha pra autorizar). Só confirma a senha; quem chamou
/// decide o que fazer depois (ver VendaView.PedirAutorizacaoGerente).
/// </summary>
public sealed class SenhaGerenteViewModel : ViewModelBase
{
    private readonly IFuncionarioRepository _funcionarios;
    private string _senha = "";
    private string? _mensagemErro;

    public SenhaGerenteViewModel(IFuncionarioRepository funcionarios, string titulo)
    {
        _funcionarios = funcionarios;
        Titulo = titulo;
        ConfirmarCommand = RelayCommand.CreateAsync(_ => ConfirmarAsync());
    }

    public string Titulo { get; }
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }
    public string? MensagemErro { get => _mensagemErro; set => SetProperty(ref _mensagemErro, value); }

    public ICommand ConfirmarCommand { get; }

    public event EventHandler? Autorizado;

    private async Task ConfirmarAsync()
    {
        MensagemErro = null;

        if (string.IsNullOrEmpty(Senha))
        {
            MensagemErro = "Informe a senha do gerente/supervisor.";
            return;
        }

        var gerente = await _funcionarios.ValidarGerenteAsync(Senha, CancellationToken.None);
        if (gerente is null)
        {
            MensagemErro = "Senha do gerente/supervisor incorreta.";
            return;
        }

        Autorizado?.Invoke(this, EventArgs.Empty);
    }
}
