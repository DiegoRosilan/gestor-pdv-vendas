using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Interfaces;
using GestorPDV.Application.Services;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// Abertura de turno (movimento de caixa) — mesma tela do legado
/// AberturaTurnoForm (escolhe turno + terminal, informa o suprimento
/// inicial), mas sem repetir login/senha do operador: quem abre o turno é
/// o funcionário já autenticado na LoginView (ver IdOperadorLogado), só a
/// senha do gerente/supervisor autoriza a abertura. Mostrada por
/// App.xaml.cs.OnStartup quando não há nenhum movimento já aberto no banco.
/// </summary>
public sealed class AberturaTurnoViewModel : ViewModelBase
{
    private readonly CaixaService _caixaService;
    private readonly IFuncionarioRepository _funcionarios;
    private readonly int _idFuncionarioLogado;

    private TurnoInfo? _turnoSelecionado;
    private TerminalInfo? _terminalSelecionado;
    private decimal _suprimento;
    private string _senhaGerente = "";
    private string? _mensagemErro;

    public AberturaTurnoViewModel(CaixaService caixaService, IFuncionarioRepository funcionarios, int idFuncionarioLogado)
    {
        _caixaService = caixaService;
        _funcionarios = funcionarios;
        _idFuncionarioLogado = idFuncionarioLogado;
        ConfirmarCommand = RelayCommand.CreateAsync(_ => ConfirmarAsync());
    }

    public ObservableCollection<TurnoInfo> Turnos { get; } = new();
    public ObservableCollection<TerminalInfo> Terminais { get; } = new();

    public TurnoInfo? TurnoSelecionado { get => _turnoSelecionado; set => SetProperty(ref _turnoSelecionado, value); }
    public TerminalInfo? TerminalSelecionado { get => _terminalSelecionado; set => SetProperty(ref _terminalSelecionado, value); }
    public decimal Suprimento { get => _suprimento; set => SetProperty(ref _suprimento, value); }
    public string SenhaGerente { get => _senhaGerente; set => SetProperty(ref _senhaGerente, value); }
    public string? MensagemErro { get => _mensagemErro; set => SetProperty(ref _mensagemErro, value); }

    public ICommand ConfirmarCommand { get; }

    /// <summary>Id do movimento aberto — App.xaml.cs lê isso depois do ShowDialog pra montar a VendaViewModel.</summary>
    public int? IdMovimentoAberto { get; private set; }

    public int IdOperador { get; private set; }

    public event EventHandler? TurnoAberto;

    public async Task CarregarAsync()
    {
        MensagemErro = null;
        try
        {
            Turnos.Clear();
            foreach (var turno in await _caixaService.GetTurnosAsync(CancellationToken.None))
                Turnos.Add(turno);
            TurnoSelecionado = Turnos.FirstOrDefault();

            Terminais.Clear();
            foreach (var terminal in await _caixaService.GetTerminaisAsync(CancellationToken.None))
                Terminais.Add(terminal);
            TerminalSelecionado = Terminais.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MensagemErro = $"Erro ao conectar: {ex.Message}";
        }
    }

    private async Task ConfirmarAsync()
    {
        MensagemErro = null;

        if (TurnoSelecionado is null)
        {
            MensagemErro = "Selecione um turno.";
            return;
        }

        if (TerminalSelecionado is null)
        {
            MensagemErro = "Selecione o terminal (caixa).";
            return;
        }

        try
        {
            IdMovimentoAberto = await _caixaService.AbrirTurnoAsync(
                _idFuncionarioLogado, SenhaGerente, TerminalSelecionado.Id, TurnoSelecionado.Id, Suprimento,
                CancellationToken.None);
            IdOperador = await _funcionarios.GetOrCreateOperadorAsync(_idFuncionarioLogado, CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            MensagemErro = ex.Message;
            return;
        }
        catch (Exception ex)
        {
            MensagemErro = $"Erro ao abrir caixa: {ex.Message}";
            return;
        }

        TurnoAberto?.Invoke(this, EventArgs.Empty);
    }
}
