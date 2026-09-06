namespace GestorPDV.Vendas;

/// <summary>
/// "F1 - Opções": menu simples com os submenus disponíveis. Por enquanto
/// só tem "Configurações" (modo de emissão fiscal, que antes ficava no
/// Menu Fiscal/F8). Feito como lista pra crescer com outros submenus no
/// futuro sem precisar redesenhar a tela.
/// </summary>
public sealed class OpcoesMenuForm : Form
{
    private readonly AppConfig _config;
    private readonly ListBox _opcoes = new();

    public OpcoesMenuForm(AppConfig config)
    {
        _config = config;

        Text = "GestorPDV - Opções";
        ClientSize = new Size(360, 320);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Opções (F1)", ClientSize.Width));

        _opcoes.Location = new Point(20, 60);
        _opcoes.Size = new Size(320, 190);
        _opcoes.Font = new Font("Segoe UI", 10.5f);
        _opcoes.ItemHeight = 28;
        _opcoes.Items.Add("Configurações");
        _opcoes.SelectedIndex = 0;
        _opcoes.DoubleClick += (_, _) => AbrirSelecionado();
        _opcoes.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AbrirSelecionado(); } };

        var abrir = new Button { Text = "Abrir", Location = new Point(150, 260), Size = new Size(90, 36) };
        abrir.Click += (_, _) => AbrirSelecionado();
        PdvTheme.EstilizarBotaoPrimario(abrir);

        var fechar = new Button { Text = "Fechar (ESC)", Location = new Point(250, 260), Size = new Size(90, 36), DialogResult = DialogResult.Cancel };
        PdvTheme.EstilizarBotaoSecundario(fechar);

        Controls.Add(_opcoes);
        Controls.Add(abrir);
        Controls.Add(fechar);

        CancelButton = fechar;
        Shown += (_, _) => _opcoes.Focus();
    }

    private void AbrirSelecionado()
    {
        if (_opcoes.SelectedIndex != 0)
            return;

        using var form = new ConfiguracoesForm(_config);
        form.ShowDialog(this);
    }
}
