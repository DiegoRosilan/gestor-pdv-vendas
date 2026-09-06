namespace GestorPDV.Vendas;

/// <summary>
/// "F1 - Opções > Configurações": modo de emissão fiscal do terminal
/// (Nenhum/Homologação/Produção — grava em appsettings.json). Antes ficava
/// dentro do Menu Fiscal (F8); F8 agora é só a lista de notas emitidas.
/// NÃO fala com a SEFAZ/SAT de verdade em nenhum modo — ver LEIA-ME.
/// </summary>
public sealed class ConfiguracoesForm : Form
{
    private static readonly (string Valor, string Rotulo)[] Modos =
    {
        ("NENHUM", "Nenhum (sem emissão fiscal — CFOP informativo 5102)"),
        ("HOMOLOGACAO", "Homologação (ambiente de testes da SEFAZ)"),
        ("PRODUCAO", "Produção (ambiente real da SEFAZ)"),
    };

    private readonly AppConfig _config;
    private readonly ComboBox _modo = new();
    private readonly Label _erro = new();

    public ConfiguracoesForm(AppConfig config)
    {
        _config = config;

        Text = "GestorPDV - Configurações";
        ClientSize = new Size(480, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Configurações", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraAcoesDialogo(this, Salvar, "Salvar"));

        var aviso = new Label
        {
            Text = "A emissão fiscal real (transmissão para a SEFAZ/SAT) não está implementada "
                 + "nesta versão — nenhuma nota é enviada, em nenhum dos modos abaixo.",
            AutoSize = false,
            Location = new Point(20, 55),
            Size = new Size(440, 50),
            ForeColor = Color.FromArgb(146, 64, 14),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(10, 8, 10, 8),
        };

        var grpConfig = new GroupBox
        {
            Text = "Configuração do Terminal",
            Location = new Point(20, 115),
            Size = new Size(440, 90),
        };
        var lblModo = new Label { Text = "Modo de emissão:", Location = new Point(15, 25), AutoSize = true };
        _modo.Location = new Point(15, 47);
        _modo.Width = 405;
        _modo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var modo in Modos)
            _modo.Items.Add(modo.Rotulo);
        var indiceAtual = Array.FindIndex(Modos, m => m.Valor == _config.ModoFiscal);
        _modo.SelectedIndex = indiceAtual >= 0 ? indiceAtual : 0;
        grpConfig.Controls.Add(lblModo);
        grpConfig.Controls.Add(_modo);

        _erro.Location = new Point(20, 213);
        _erro.Size = new Size(440, 30);
        _erro.ForeColor = PdvTheme.Perigo;

        Controls.Add(aviso);
        Controls.Add(grpConfig);
        Controls.Add(_erro);
    }

    private Task<bool> Salvar()
    {
        try
        {
            _config.ModoFiscal = Modos[_modo.SelectedIndex].Valor;
            _config.Save();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao salvar configuração: {ex.Message}";
            return Task.FromResult(false);
        }
    }
}
