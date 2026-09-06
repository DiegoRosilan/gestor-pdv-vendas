namespace GestorPDV.Application.Interfaces;

/// <summary>Funcionário do PDV — separado de Cliente porque no banco legado é uma tabela (ecf_funcionario) sem relação com Domain.Entities.Cliente.</summary>
public interface IFuncionarioRepository
{
    Task<FuncionarioInfo?> GetPorLoginAsync(string login, CancellationToken ct);
    Task<FuncionarioInfo?> ValidarGerenteAsync(string senha, CancellationToken ct);
    Task<int> GetOrCreateOperadorAsync(int idFuncionario, CancellationToken ct);
}

/// <summary>Senha vem em texto puro porque é assim que ecf_funcionario guarda no banco legado — não é uma escolha desta camada.</summary>
public sealed record FuncionarioInfo(int Id, string Nome, string? Login, string? Senha, bool Vendedor, bool Caixa, bool Gerente);
