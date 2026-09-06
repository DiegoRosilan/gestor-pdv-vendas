namespace GestorPDV.Domain.Enums;

/// <summary>Espelha status_venda em ecf_venda_cabecalho ('A'/'F'/'C' no banco legado).</summary>
public enum StatusVenda
{
    Aberta,
    Finalizada,
    Cancelada,
}
