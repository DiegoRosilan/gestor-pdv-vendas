using System.Drawing.Drawing2D;

namespace GestorPDV.Vendas;

/// <summary>
/// Ícones dos botões, desenhados em código (GDI+) em vez de arquivos .ico/.png
/// embutidos — mesma técnica usada no GestorPDV.Monitor, sem depender de
/// nenhum recurso binário extra no build/publish.
/// </summary>
internal static class ButtonIcons
{
    private const int Size = 16;

    private static readonly Color Ink = Color.FromArgb(100, 112, 132);
    private static readonly Color Green = Color.FromArgb(22, 163, 74);
    private static readonly Color Red = Color.FromArgb(220, 38, 38);
    private static readonly Color Blue = Color.FromArgb(37, 99, 235);

    private static Bitmap Create(Action<Graphics> draw)
    {
        var bmp = new Bitmap(Size, Size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        draw(g);
        return bmp;
    }

    /// <summary>Cruz verde — Adicionar item.</summary>
    public static Bitmap Plus() => Create(g =>
    {
        using var pen = new Pen(Green, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 8, 3, 8, 13);
        g.DrawLine(pen, 3, 8, 13, 8);
    });

    /// <summary>Lata de lixo — Remover item.</summary>
    public static Bitmap Trash() => Create(g =>
    {
        using var pen = new Pen(Red, 1.4f);
        g.DrawLine(pen, 3.5f, 4.5f, 12.5f, 4.5f);
        g.DrawLine(pen, 6, 4.5f, 6, 2.5f);
        g.DrawLine(pen, 10, 4.5f, 10, 2.5f);
        g.DrawLine(pen, 6, 2.5f, 10, 2.5f);
        g.DrawLine(pen, 4.5f, 4.5f, 5.2f, 13.5f);
        g.DrawLine(pen, 11.5f, 4.5f, 10.8f, 13.5f);
        g.DrawLine(pen, 5.2f, 13.5f, 10.8f, 13.5f);
        g.DrawLine(pen, 7, 6.5f, 7.3f, 11.5f);
        g.DrawLine(pen, 9, 6.5f, 8.7f, 11.5f);
    });

    /// <summary>Cifrão — Pagamento.</summary>
    public static Bitmap Money() => Create(g =>
    {
        using var pen = new Pen(Green, 1.6f);
        using var font = new Font("Arial", 9.5f, FontStyle.Bold);
        using var brush = new SolidBrush(Green);
        g.DrawEllipse(pen, 1.5f, 1.5f, 13, 13);
        g.DrawString("$", font, brush, 3.6f, 1.3f);
    });

    /// <summary>Silhueta de pessoa — Identificar cliente.</summary>
    public static Bitmap Person() => Create(g =>
    {
        using var brush = new SolidBrush(Ink);
        g.FillEllipse(brush, 5.5f, 2, 5, 5);
        g.FillPie(brush, 2, 8, 12, 10, 180, 180);
    });

    /// <summary>Check verde — Finalizar/Confirmar.</summary>
    public static Bitmap Check() => Create(g =>
    {
        using var pen = new Pen(Green, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, new PointF[] { new(3, 8.5f), new(6.5f, 12.5f), new(13, 3.5f) });
    });

    /// <summary>Quadrado vermelho — Cancelar venda.</summary>
    public static Bitmap Stop() => Create(g =>
    {
        using var brush = new SolidBrush(Red);
        g.FillRectangle(brush, 4, 4, 8, 8);
    });

    /// <summary>Lupa — Buscar.</summary>
    public static Bitmap Search() => Create(g =>
    {
        using var pen = new Pen(Ink, 1.8f);
        g.DrawEllipse(pen, 2.5f, 2.5f, 7.5f, 7.5f);
        g.DrawLine(pen, 9.5f, 9.5f, 13.5f, 13.5f);
    });

    /// <summary>X — Fechar/Cancelar caixa.</summary>
    public static Bitmap Close() => Create(g =>
    {
        using var pen = new Pen(Red, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 3.5f, 3.5f, 12.5f, 12.5f);
        g.DrawLine(pen, 12.5f, 3.5f, 3.5f, 12.5f);
    });

    /// <summary>Cadeado — Login.</summary>
    public static Bitmap Lock() => Create(g =>
    {
        using var pen = new Pen(Ink, 1.4f);
        using var brush = new SolidBrush(Blue);
        g.DrawArc(pen, 4.5f, 2, 7, 7, 180, 180);
        g.FillRectangle(brush, 3, 7, 10, 7);
    });

    /// <summary>Triângulo verde — Iniciar/Abrir caixa.</summary>
    public static Bitmap Play() => Create(g =>
    {
        using var brush = new SolidBrush(Green);
        g.FillPolygon(brush, new PointF[] { new(4, 2.5f), new(4, 13.5f), new(13.5f, 8) });
    });
}
