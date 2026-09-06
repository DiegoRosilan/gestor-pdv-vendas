namespace GestorPDV.Domain.Exceptions;

/// <summary>Violação de uma regra de negócio (Rules/) — nunca um erro técnico (isso é exceção comum/infra).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
