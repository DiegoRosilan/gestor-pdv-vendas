namespace GestorPDV.Domain.ValueObjects;

public enum TipoDocumento
{
    Cpf,
    Cnpj,
}

/// <summary>CPF ou CNPJ de um cliente — guarda só os dígitos, tipo é inferido pela quantidade (11 = CPF, 14 = CNPJ).</summary>
public readonly record struct Documento
{
    public string Numero { get; }
    public TipoDocumento Tipo { get; }

    public Documento(string numero)
    {
        var digitos = new string(numero.Where(char.IsDigit).ToArray());
        Tipo = digitos.Length switch
        {
            11 => TipoDocumento.Cpf,
            14 => TipoDocumento.Cnpj,
            _ => throw new ArgumentException($"CPF/CNPJ inválido: \"{numero}\" (esperado 11 ou 14 dígitos).", nameof(numero)),
        };
        Numero = digitos;
    }

    public override string ToString() => Numero;
}
