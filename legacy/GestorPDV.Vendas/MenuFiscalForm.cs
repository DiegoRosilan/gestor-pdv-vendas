using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// "F8 - Menu Fiscal": lista todas as notas (vendas finalizadas) do
/// movimento atual — COO, data/hora, valor e situação. A configuração do
/// modo de emissão fiscal saiu daqui e foi para F1 - Opções >
/// Configurações (ConfiguracoesForm).
///
/// NÃO fala com a SEFAZ/SAT de verdade — isso continua fora do escopo
/// desta versão (ver LEIA-ME). Esta tela só mostra o que já está gravado
/// em ecf_venda_cabecalho, não emite nada.
/// </summary>
public sealed class MenuFiscalForm : Form
{
    private readonly Database _db;
    private readonly int _idMovimento;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly DataGridView _grid = new();
    private readonly Label _erro = new();

    public MenuFiscalForm(Database db, int idMovimento)
    {
        _db = db;
        _idMovimento = idMovimento;

        Text = "GestorPDV - Menu Fiscal";
        ClientSize = new Size(600, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Notas Emitidas Neste Movimento", ClientSize.Width));

        var fechar = new Button { Text = "Fechar (ESC)", Size = new Size(120, 36), DialogResult = DialogResult.Cancel };
        var barraInferior = new Panel { Dock = DockStyle.Bottom, Height = 51, BackColor = PdvTheme.Borda };
        var barraInferiorFundo = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = PdvTheme.CorpoClaro };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 7, 10, 0) };
        flow.Controls.Add(fechar);
        barraInferiorFundo.Controls.Add(flow);
        barraInferior.Controls.Add(barraInferiorFundo);
        PdvTheme.EstilizarBotaoSecundario(fechar);
        CancelButton = fechar;
        Controls.Add(barraInferior);

        ConfigurarGrid();
        _grid.Location = new Point(15, 55);
        _grid.Size = new Size(570, 300);
        Controls.Add(_grid);

        _erro.Location = new Point(15, 363);
        _erro.Size = new Size(570, 40);
        _erro.ForeColor = PdvTheme.Perigo;
        Controls.Add(_erro);

        Shown += async (_, _) => await CarregarAsync();
    }

    private void ConfigurarGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        PdvTheme.EstilizarGrid(_grid);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Coo", HeaderText = "COO", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataHora", HeaderText = "Data/Hora", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "Valor", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Situacao", HeaderText = "Situação", Width = 150 });
    }

    private async Task CarregarAsync()
    {
        try
        {
            var notas = await _db.GetNotasEmitidasAsync(_idMovimento, CancellationToken.None);
            _grid.Rows.Clear();

            if (notas.Count == 0)
            {
                _erro.ForeColor = PdvTheme.TextoSecundario;
                _erro.Text = "Nenhuma nota emitida neste movimento ainda.";
                return;
            }

            foreach (var nota in notas)
            {
                var situacao = nota.StatusVenda switch
                {
                    "F" => "Finalizada",
                    "C" => "Cancelada",
                    _ => nota.StatusVenda,
                };
                _grid.Rows.Add(nota.Coo, $"{nota.DataVenda:dd/MM/yyyy} {nota.HoraVenda}", $"R$ {nota.ValorFinal.ToString("N2", _ptBr)}", situacao);
            }
        }
        catch (Exception ex)
        {
            _erro.ForeColor = PdvTheme.Perigo;
            _erro.Text = $"Erro ao consultar notas emitidas: {ex.Message}";
        }
    }
}
