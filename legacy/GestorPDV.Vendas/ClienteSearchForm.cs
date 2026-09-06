namespace GestorPDV.Vendas;

public sealed class ClienteSearchForm : Form
{
    private readonly Database _db;
    private readonly TextBox _termo = new();
    private readonly Button _buscar = new();
    private readonly ListBox _resultados = new();
    private readonly Button _selecionar = new();
    private readonly Button _cancelar = new();

    private List<Cliente> _lista = new();

    public Cliente? ClienteSelecionado { get; private set; }

    public ClienteSearchForm(Database db)
    {
        _db = db;

        Text = "Identificar cliente";
        ClientSize = new Size(420, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        var lbl = new Label { Text = "Nome ou CPF/CNPJ:", Location = new Point(20, 20), AutoSize = true };
        _termo.Location = new Point(20, 40);
        _termo.Width = 280;
        _termo.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = BuscarAsync(); } };

        _buscar.Text = "Buscar";
        _buscar.Width = 95;
        _buscar.Location = new Point(305, 39);
        _buscar.Image = ButtonIcons.Search();
        _buscar.TextImageRelation = TextImageRelation.ImageBeforeText;
        _buscar.Click += async (_, _) => await BuscarAsync();
        PdvTheme.EstilizarBotaoSecundario(_buscar);

        _resultados.Location = new Point(20, 75);
        _resultados.Size = new Size(380, 200);
        _resultados.DoubleClick += (_, _) => Selecionar_Click(null, EventArgs.Empty);

        _selecionar.Text = "Selecionar";
        _selecionar.Width = 110;
        _selecionar.Location = new Point(190, 285);
        _selecionar.Image = ButtonIcons.Person();
        _selecionar.TextImageRelation = TextImageRelation.ImageBeforeText;
        _selecionar.Click += Selecionar_Click;
        PdvTheme.EstilizarBotaoSecundario(_selecionar);

        _cancelar.Text = "Cancelar";
        _cancelar.Width = 100;
        _cancelar.Location = new Point(305, 285);
        _cancelar.Image = ButtonIcons.Close();
        _cancelar.TextImageRelation = TextImageRelation.ImageBeforeText;
        _cancelar.DialogResult = DialogResult.Cancel;
        PdvTheme.EstilizarBotaoSecundario(_cancelar);

        Controls.Add(lbl);
        Controls.Add(_termo);
        Controls.Add(_buscar);
        Controls.Add(_resultados);
        Controls.Add(_selecionar);
        Controls.Add(_cancelar);

        CancelButton = _cancelar;
        Shown += (_, _) => _termo.Focus();
    }

    private async Task BuscarAsync()
    {
        try
        {
            _lista = await _db.BuscarClientesAsync(_termo.Text.Trim(), CancellationToken.None);
            _resultados.Items.Clear();
            foreach (var c in _lista)
                _resultados.Items.Add($"{c.Nome}" + (string.IsNullOrWhiteSpace(c.CpfCnpj) ? "" : $" - {c.CpfCnpj}"));

            if (_lista.Count == 0)
                _resultados.Items.Add("(nenhum cliente encontrado)");
        }
        catch (Exception ex)
        {
            _resultados.Items.Clear();
            _resultados.Items.Add($"Erro ao buscar: {ex.Message}");
        }
    }

    private void Selecionar_Click(object? sender, EventArgs e)
    {
        if (_resultados.SelectedIndex < 0 || _resultados.SelectedIndex >= _lista.Count)
            return;

        ClienteSelecionado = _lista[_resultados.SelectedIndex];
        DialogResult = DialogResult.OK;
        Close();
    }
}
