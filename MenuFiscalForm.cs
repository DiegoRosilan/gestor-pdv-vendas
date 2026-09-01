using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// "F8 - Menu Fiscal": configura o modo de emissão fiscal do terminal
/// (Nenhum/Homologação/Produção — grava em appsettings.json) e mostra o
/// status do último documento gravado neste movimento. NÃO fala com a
/// SEFAZ/SAT de verdade em nenhum modo — ver aviso na própria tela e o
/// LEIA-ME. Isso é intencional: implementar emissão real de NFC-e/SAT
/// exigiria certificado digital, homologação com a SEFAZ e webservices
/// específicos do ambiente do cliente, fora do que dá pra validar sem
/// acesso a esse ambiente real.
/// </summary>
public sealed class MenuFiscalForm : Form
{
    private static readonly (string Valor, string Rotulo)[] Modos =
    {
        ("NENHUM", "Nenhum (sem emissão fiscal — CFOP informativo 5102)"),
        ("HOMOLOGACAO", "Homologação (ambiente de testes da SEFAZ)"),
        ("PRODUCAO", "Produção (ambiente real da SEFAZ)"),
    };

    private readonly Database _db;
    private readonly AppConfig _config;
    private readonly int _idMovimento;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ComboBox _modo = new();
    private readonly Label _statusCoo = new();
    private readonly Label _statusData = new();
    private readonly Label _statusValor = new();
    private readonly Label _statusSituacao = new();
    private readonly Label _erro = new();

    public MenuFiscalForm(Database db, AppConfig config, int idMovimento)
    {
        _db = db;
        _config = config;
        _idMovimento = idMovimento;

        Text = "GestorPDV - Menu Fiscal";
        ClientSize = new Size(520, 470);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Menu Fiscal", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraAcoesDialogo(this, Salvar, "Salvar"));

        var aviso = new Label
        {
            Text = "Este painel configura o modo de emissão e mostra o status dos documentos já "
                 + "gravados. A emissão fiscal real (transmissão para a SEFAZ/SAT) não está "
                 + "implementada nesta versão — nenhuma nota é enviada, em nenhum dos modos abaixo.",
            AutoSize = false,
            Location = new Point(20, 55),
            Size = new Size(480, 55),
            ForeColor = Color.FromArgb(146, 64, 14),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(10, 8, 10, 8),
        };

        var grpConfig = new GroupBox
        {
            Text = "Configuração do Terminal",
            Location = new Point(20, 120),
            Size = new Size(480, 100),
        };
        var lblModo = new Label { Text = "Modo de emissão:", Location = new Point(15, 30), AutoSize = true };
        _modo.Location = new Point(15, 52);
        _modo.Width = 445;
        _modo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var modo in Modos)
            _modo.Items.Add(modo.Rotulo);
        var indiceAtual = Array.FindIndex(Modos, m => m.Valor == _config.ModoFiscal);
        _modo.SelectedIndex = indiceAtual >= 0 ? indiceAtual : 0;
        grpConfig.Controls.Add(lblModo);
        grpConfig.Controls.Add(_modo);

        var grpStatus = new GroupBox
        {
            Text = "Status do Último Documento Deste Movimento",
            Location = new Point(20, 235),
            Size = new Size(480, 140),
        };
        AdicionarLinhaStatus(grpStatus, "COO:", _statusCoo, 30);
        AdicionarLinhaStatus(grpStatus, "Data/Hora:", _statusData, 58);
        AdicionarLinhaStatus(grpStatus, "Valor:", _statusValor, 86);
        AdicionarLinhaStatus(grpStatus, "Situação:", _statusSituacao, 114);

        _erro.Location = new Point(20, 380);
        _erro.Size = new Size(480, 30);
        _erro.ForeColor = PdvTheme.Perigo;

        Controls.Add(aviso);
        Controls.Add(grpConfig);
        Controls.Add(grpStatus);
        Controls.Add(_erro);

        LimparStatus();
        Shown += async (_, _) => await CarregarStatusAsync();
    }

    private static void AdicionarLinhaStatus(Control container, string rotulo, Label valorLabel, int top)
    {
        container.Controls.Add(new Label { Text = rotulo, Location = new Point(15, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        valorLabel.Location = new Point(140, top);
        valorLabel.AutoSize = true;
        container.Controls.Add(valorLabel);
    }

    private void LimparStatus()
    {
        _statusCoo.Text = "-";
        _statusData.Text = "-";
        _statusValor.Text = "-";
        _statusSituacao.Text = "-";
    }

    private async Task CarregarStatusAsync()
    {
        try
        {
            var ultima = await _db.GetUltimaVendaDoMovimentoAsync(_idMovimento, CancellationToken.None);
            if (ultima is null)
            {
                _statusCoo.Text = "-";
                _statusData.Text = "Nenhuma venda finalizada neste movimento ainda.";
                _statusValor.Text = "-";
                _statusSituacao.Text = "-";
                return;
            }

            _statusCoo.Text = ultima.Coo.ToString();
            _statusData.Text = $"{ultima.DataVenda:dd/MM/yyyy} {ultima.HoraVenda}";
            _statusValor.Text = $"R$ {ultima.ValorFinal.ToString("N2", _ptBr)}";
            _statusSituacao.Text = ultima.StatusVenda switch
            {
                "F" => "Finalizada",
                "C" => "Cancelada",
                _ => ultima.StatusVenda,
            };
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao consultar status: {ex.Message}";
        }
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
