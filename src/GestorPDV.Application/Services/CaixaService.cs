using GestorPDV.Application.Interfaces;

namespace GestorPDV.Application.Services;

/// <summary>Abertura/encerramento de turno e consulta de movimento — espelha AberturaTurnoForm/EncerraCaixaForm do legado.</summary>
public sealed class CaixaService
{
    private readonly ICaixaRepository _caixa;
    private readonly IFuncionarioRepository _funcionarios;

    public CaixaService(ICaixaRepository caixa, IFuncionarioRepository funcionarios)
    {
        _caixa = caixa;
        _funcionarios = funcionarios;
    }

    public Task<int?> GetMovimentoAbertoAsync(CancellationToken ct) => _caixa.GetMovimentoAbertoAsync(ct);

    public Task<MovimentoInfo?> GetMovimentoInfoAsync(int idMovimento, CancellationToken ct) => _caixa.GetMovimentoInfoAsync(idMovimento, ct);

    public Task<List<TurnoInfo>> GetTurnosAsync(CancellationToken ct) => _caixa.GetTurnosAsync(ct);

    public Task<List<TerminalInfo>> GetTerminaisAsync(CancellationToken ct) => _caixa.GetTerminaisAsync(ct);

    /// <summary>
    /// Abre o turno pro operador já autenticado na LoginView (idOperador vem
    /// de lá — sem pedir login/senha de novo, diferente do legado onde
    /// AberturaTurnoForm fazia a própria autenticação do operador). Só
    /// confere a senha do gerente/supervisor, autorização exigida pra abrir
    /// o caixa — igual à validação em AberturaTurnoForm.Confirmar.
    /// </summary>
    public async Task<int> AbrirTurnoAsync(int idOperador, string senhaGerente, int idCaixa, int idTurno, decimal suprimento, CancellationToken ct)
    {
        var gerente = await _funcionarios.ValidarGerenteAsync(senhaGerente, ct)
            ?? throw new InvalidOperationException("Senha do gerente/supervisor incorreta.");

        return await _caixa.AbrirCaixaAsync(idOperador, gerente.Id, idCaixa, idTurno, suprimento, ct);
    }

    public Task EncerrarAsync(int idMovimento, IReadOnlyList<(string TipoPagamento, decimal Valor)> encerrantes, CancellationToken ct)
        => _caixa.EncerrarCaixaAsync(idMovimento, encerrantes, ct);
}
