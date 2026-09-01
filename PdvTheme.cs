using System.Drawing.Drawing2D;

namespace GestorPDV.Vendas;

/// <summary>
/// Paleta e elementos visuais compartilhados entre as telas — visual
/// moderno (cartões brancos com borda sutil, cantos arredondados, cor de
/// destaque única para ações primárias) mantendo a mesma estrutura do
/// sistema original: barra superior escura, barra de teclas de função
/// (F1..F12) na base das telas principais, e caixas de leitura para
/// Quantidade/Valor Unitário/Total.
/// </summary>
internal static class PdvTheme
{
    // ---- Paleta ----
    public static readonly Color BarraEscura = Color.FromArgb(22, 38, 63);
    public static readonly Color BarraEscuraClara = Color.FromArgb(31, 52, 84);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentEscuro = Color.FromArgb(29, 78, 216);
    public static readonly Color Sucesso = Color.FromArgb(22, 163, 74);
    public static readonly Color Perigo = Color.FromArgb(220, 38, 38);

    public static readonly Color CorpoClaro = Color.FromArgb(243, 245, 249);
    public static readonly Color Borda = Color.FromArgb(223, 228, 236);
    public static readonly Color TextoClaro = Color.White;
    public static readonly Color TextoPrimario = Color.FromArgb(30, 41, 59);
    public static readonly Color TextoSecundario = Color.FromArgb(100, 112, 132);

    /// <summary>Mantido pelo nome original (usado como cor de fundo de um rótulo de título em FechaVendaForm).</summary>
    public static readonly Color CaixaLeitura = Color.White;

    public static readonly Font FonteTitulo = new("Segoe UI", 12, FontStyle.Bold);
    public static readonly Font FonteLeitura = new("Segoe UI", 20, FontStyle.Bold);
    public static readonly Font FonteRotulo = new("Segoe UI", 9);

    /// <summary>Fonte padrão do app — aplicar em Form.Font para modernizar a tipografia de todos os controles que não definem a própria fonte.</summary>
    public static readonly Font FontePadrao = new("Segoe UI", 9.25f);

    /// <summary>Barra superior escura com o título centralizado e uma fina linha de destaque na base.</summary>
    public static Panel CriarBarraTitulo(string titulo, int width)
    {
        var container = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Accent,
        };

        var lbl = new Label
        {
            Text = titulo,
            ForeColor = TextoClaro,
            Font = FonteTitulo,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = BarraEscura,
        };

        container.Controls.Add(lbl);
        return container;
    }

    /// <summary>Uma "caixa de leitura" (Quantidade/Valor Unitário/Total): cartão branco com número grande e uma fina borda de destaque na base.</summary>
    public static Panel CriarCaixaLeitura(string rotulo, out Label valorLabel)
    {
        var painel = new Panel
        {
            Size = new Size(200, 70),
            BackColor = Color.Transparent,
        };

        var lblRotulo = new Label
        {
            Text = rotulo,
            Location = new Point(0, 0),
            AutoSize = true,
            Font = FonteRotulo,
            ForeColor = TextoSecundario,
        };

        var moldura = new Panel
        {
            Location = new Point(0, 20),
            Size = new Size(200, 48),
            BackColor = Accent,
        };

        var valor = new Label
        {
            Text = "0,00",
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Font = FonteLeitura,
            ForeColor = TextoPrimario,
            Padding = new Padding(0, 0, 8, 0),
        };

        moldura.Controls.Add(valor);
        painel.Controls.Add(lblRotulo);
        painel.Controls.Add(moldura);
        valorLabel = valor;
        return painel;
    }

    /// <summary>Um item de tecla de função (círculo com "F1" + rótulo) igual à barra de baixo das telas do sistema original.</summary>
    public sealed record TeclaFuncao(int Numero, string Rotulo, EventHandler? Acao, bool Habilitada = true);

    /// <summary>
    /// Monta a barra inferior escura com as teclas de função e já liga
    /// tanto o clique do mouse quanto a tecla real (F1..F12) do teclado —
    /// o formulário precisa ter KeyPreview=true para as teclas funcionarem.
    /// Itens com Acao=null aparecem na barra (para bater visualmente com o
    /// sistema original) mas não fazem nada ainda.
    /// </summary>
    public static Panel CriarBarraFuncoes(Form form, IReadOnlyList<TeclaFuncao> teclas)
    {
        var container = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Accent,
        };

        var barra = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            BackColor = BarraEscura,
        };

        var largura = Math.Max(70, 900 / Math.Max(1, teclas.Count));
        var x = 4;
        foreach (var tecla in teclas)
        {
            var botao = new Button
            {
                Text = tecla.Rotulo,
                Location = new Point(x, 3),
                Size = new Size(largura - 6, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = tecla.Habilitada ? TextoClaro : Color.FromArgb(120, 132, 150),
                BackColor = tecla.Habilitada ? BarraEscuraClara : BarraEscura,
                Font = new Font("Segoe UI", 7.5f),
                Enabled = tecla.Habilitada,
                Image = CriarSeloFKey(tecla.Numero, tecla.Habilitada),
                TextImageRelation = TextImageRelation.TextBeforeImage,
                ImageAlign = ContentAlignment.MiddleRight,
                Cursor = tecla.Habilitada ? Cursors.Hand : Cursors.Default,
            };
            botao.FlatAppearance.BorderSize = 0;
            AplicarCantosArredondados(botao, 8);

            if (tecla.Habilitada)
            {
                botao.MouseEnter += (_, _) => botao.BackColor = Accent;
                botao.MouseLeave += (_, _) => botao.BackColor = BarraEscuraClara;
            }

            if (tecla.Acao is not null)
                botao.Click += tecla.Acao;

            barra.Controls.Add(botao);
            x += largura;
        }

        container.Controls.Add(barra);

        form.KeyPreview = true;
        form.KeyDown += (_, e) =>
        {
            var numero = e.KeyCode switch
            {
                Keys.F1 => 1, Keys.F2 => 2, Keys.F3 => 3, Keys.F4 => 4,
                Keys.F5 => 5, Keys.F6 => 6, Keys.F7 => 7, Keys.F8 => 8,
                Keys.F9 => 9, Keys.F10 => 10, Keys.F11 => 11, Keys.F12 => 12,
                _ => 0,
            };
            if (numero == 0) return;

            var tecla = teclas.FirstOrDefault(t => t.Numero == numero && t.Habilitada && t.Acao is not null);
            if (tecla is null) return;

            e.Handled = true;
            tecla.Acao!(form, EventArgs.Empty);
        };

        return container;
    }

    /// <summary>
    /// Barra de ações padrão dos diálogos (login, abertura/encerramento de
    /// caixa): "Cancela (ESC)" e "Confirma (F12)", com uma fina linha
    /// divisória no topo. confirmar retorna false para impedir o
    /// fechamento do diálogo (ex.: validação que falhou).
    ///
    /// confirmar é assíncrono (Func&lt;Task&lt;bool&gt;&gt;) de propósito: os
    /// diálogos fazem consultas ao banco para validar login/senha, e
    /// bloquear a thread da UI esperando essa consulta terminar
    /// (.GetAwaiter().GetResult() dentro do clique) trava a tela — o
    /// SynchronizationContext do WinForms não consegue entregar a
    /// continuação do await enquanto a mesma thread está presa esperando
    /// por ela. Usar "await" de verdade aqui evita esse deadlock.
    /// </summary>
    public static Panel CriarBarraAcoesDialogo(Form form, Func<Task<bool>> confirmar, string textoConfirmar = "Confirma")
    {
        var container = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 51,
            BackColor = Borda,
        };

        var barra = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = CorpoClaro,
        };

        // FlowLayoutPanel da direita pra esquerda: alinha os dois botões na
        // ponta direita independente da largura final do diálogo (que ainda
        // não é conhecida no momento em que este painel é montado).
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 7, 10, 0),
        };

        var cancelar = new Button
        {
            Text = "Cancela (ESC)",
            Size = new Size(140, 36),
            Image = ButtonIcons.Close(),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextoPrimario,
        };
        cancelar.FlatAppearance.BorderColor = Borda;
        cancelar.FlatAppearance.BorderSize = 1;
        AplicarCantosArredondados(cancelar, 8);

        var confirmarBotao = new Button
        {
            Text = $"{textoConfirmar} (F12)",
            Size = new Size(140, 36),
            Margin = new Padding(10, 0, 0, 0),
            Image = ButtonIcons.Check(),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = TextoClaro,
        };
        confirmarBotao.FlatAppearance.BorderSize = 0;
        AplicarCantosArredondados(confirmarBotao, 8);
        confirmarBotao.MouseEnter += (_, _) => { if (confirmarBotao.Enabled) confirmarBotao.BackColor = AccentEscuro; };
        confirmarBotao.MouseLeave += (_, _) => confirmarBotao.BackColor = Accent;

        confirmarBotao.Click += async (_, _) =>
        {
            confirmarBotao.Enabled = false;
            try
            {
                if (await confirmar())
                {
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
            }
            finally
            {
                confirmarBotao.Enabled = true;
            }
        };

        // FlowDirection.RightToLeft adiciona da direita pra esquerda, então
        // o primeiro adicionado (Confirma) fica mais à direita.
        flow.Controls.Add(confirmarBotao);
        flow.Controls.Add(cancelar);
        barra.Controls.Add(flow);
        container.Controls.Add(barra);

        form.CancelButton = cancelar;
        form.KeyPreview = true;
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.F12) return;
            e.Handled = true;
            confirmarBotao.PerformClick();
        };

        return container;
    }

    /// <summary>Estilo de botão de ação principal (preenchido, cor de destaque). Chamar depois de definir Location/Size do botão.</summary>
    public static void EstilizarBotaoPrimario(Button botao)
    {
        botao.FlatStyle = FlatStyle.Flat;
        botao.FlatAppearance.BorderSize = 0;
        botao.BackColor = Accent;
        botao.ForeColor = TextoClaro;
        botao.Cursor = Cursors.Hand;
        AplicarCantosArredondados(botao, 8);
        botao.MouseEnter += (_, _) => { if (botao.Enabled) botao.BackColor = AccentEscuro; };
        botao.MouseLeave += (_, _) => botao.BackColor = Accent;
    }

    /// <summary>Estilo de botão de ação secundária (contorno claro). Chamar depois de definir Location/Size do botão.</summary>
    public static void EstilizarBotaoSecundario(Button botao)
    {
        botao.FlatStyle = FlatStyle.Flat;
        botao.FlatAppearance.BorderColor = Borda;
        botao.FlatAppearance.BorderSize = 1;
        botao.BackColor = Color.White;
        botao.ForeColor = TextoPrimario;
        botao.Cursor = Cursors.Hand;
        AplicarCantosArredondados(botao, 8);
        botao.MouseEnter += (_, _) => { if (botao.Enabled) botao.BackColor = CorpoClaro; };
        botao.MouseLeave += (_, _) => botao.BackColor = Color.White;
    }

    /// <summary>Recorta os cantos do controle em um retângulo arredondado. Chamar depois de definir Size (não reage a Resize).</summary>
    public static void AplicarCantosArredondados(Control controle, int raio)
    {
        if (controle.Width <= 0 || controle.Height <= 0) return;

        var rect = new Rectangle(0, 0, controle.Width, controle.Height);
        using var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, raio, raio, 180, 90);
        path.AddArc(rect.Right - raio, rect.Y, raio, raio, 270, 90);
        path.AddArc(rect.Right - raio, rect.Bottom - raio, raio, raio, 0, 90);
        path.AddArc(rect.X, rect.Bottom - raio, raio, raio, 90, 90);
        path.CloseFigure();
        controle.Region = new Region(path);
    }

    /// <summary>Aplica o visual moderno padrão do app a um DataGridView (cabeçalho, seleção, linhas alternadas), sem alterar as colunas já definidas pelo chamador.</summary>
    public static void EstilizarGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.GridColor = Borda;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 32;
        grid.RowTemplate.Height = 28;

        grid.ColumnHeadersDefaultCellStyle.BackColor = CorpoClaro;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextoPrimario;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
    }

    private static Bitmap CriarSeloFKey(int numero, bool habilitada)
    {
        var bmp = new Bitmap(22, 22);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(habilitada ? Color.White : Color.Gray, 1.4f);
        g.DrawEllipse(pen, 1, 1, 19, 19);
        using var font = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        using var brush = new SolidBrush(habilitada ? Color.White : Color.Gray);
        var texto = "F" + numero;
        var tamanho = g.MeasureString(texto, font);
        g.DrawString(texto, font, brush, (22 - tamanho.Width) / 2, (22 - tamanho.Height) / 2);
        return bmp;
    }
}
