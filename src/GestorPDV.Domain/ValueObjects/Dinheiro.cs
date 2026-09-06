using System.Globalization;

namespace GestorPDV.Domain.ValueObjects;

/// <summary>
/// Value object pra qualquer quantia em reais — evita passar "decimal cru"
/// pelo domínio e centraliza arredondamento (2 casas, sempre) e formatação
/// pt-BR. Nunca negativo: operações que resultariam em valor negativo
/// truncam em zero, igual ao comportamento já usado no PDV legado
/// (Math.Max(0m, ...) espalhado pelo VendaForm original).
/// </summary>
public readonly record struct Dinheiro
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public decimal Valor { get; }

    public Dinheiro(decimal valor) => Valor = Math.Max(0m, Math.Round(valor, 2, MidpointRounding.ToEven));

    public static readonly Dinheiro Zero = new(0m);

    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => new(a.Valor + b.Valor);
    public static Dinheiro operator -(Dinheiro a, Dinheiro b) => new(a.Valor - b.Valor);
    public static Dinheiro operator *(Dinheiro a, decimal fator) => new(a.Valor * fator);

    public static implicit operator decimal(Dinheiro d) => d.Valor;
    public static explicit operator Dinheiro(decimal v) => new(v);

    public override string ToString() => Valor.ToString("N2", PtBr);
}
