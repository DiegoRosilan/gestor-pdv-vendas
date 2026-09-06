namespace GestorPDV.Application.Interfaces;

/// <summary>Turno de caixa (movimento) — abertura, situação atual e encerramento. Espelha ecf_movimento/ecf_turno/ecf_caixa.</summary>
public interface ICaixaRepository
{
    Task<int?> GetMovimentoAbertoAsync(CancellationToken ct);
    Task<MovimentoInfo?> GetMovimentoInfoAsync(int idMovimento, CancellationToken ct);
    Task<List<TurnoInfo>> GetTurnosAsync(CancellationToken ct);
    Task<List<TerminalInfo>> GetTerminaisAsync(CancellationToken ct);
    Task<int> AbrirCaixaAsync(int idFuncionarioOperador, int idFuncionarioGerente, int idCaixa, int idTurno, decimal suprimento, CancellationToken ct);
    Task EncerrarCaixaAsync(int idMovimento, IReadOnlyList<(string TipoPagamento, decimal Valor)> encerrantes, CancellationToken ct);
}

public sealed record TurnoInfo(int Id, string Descricao, string? HoraInicio, string? HoraFim);
public sealed record TerminalInfo(int Id, string Nome);
public sealed record MovimentoInfo(int Id, string Turno, string Terminal, string Impressora, DateTime? DataAbertura, string? HoraAbertura);
