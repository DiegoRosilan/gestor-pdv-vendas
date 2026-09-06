using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// "Encerra Movimento de Caixa": o operador declara quanto conferiu de
/// cada forma de pagamento (os "encerrantes") e confirma com a própria
/// senha mais a senha do gerente/supervisor. Não compara o declarado com
/// o total do sistema — isso ficaria para um relatório de conferência à
/// parte, fora do escopo desta versão.
/// </summary>
public sealed class EncerraCaixaForm : Form
{
    private readonly Database _db;
    private readonly Funcionario _funcionarioLogado;
    private readonly MovimentoInfo _movimento;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ComboBox _tipoLancamento = new();
    private readonly NumericUpDown _valorDocumento = new();
    private readonly Button _adicionar = new();
    private readonly Button _remover = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _senhaOperador = new();
    private readonly TextBox _senhaGerente = new();
    private readonly Label _erro = new();

    private List<FormaPagamento> _formas = new();
    private readonly List<Encerrante> _encerrantes = new();

    public EncerraCaixaForm(Database db, Funcionario funcionarioLogado, MovimentoInfo movimento)
    {
        _db = db;
        _funcionarioLogado = funcionarioLogado;
        _movimento = movimento;

        Text = "GestorPDV - Encerramento de turno";
        ClientSize = new Size(520, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Encerra Movimento de Caixa", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraAcoesDialogo(this, Confirmar));

        var grpMovimento = new GroupBox { Text = "Dados do Movimento Aberto", Location = new Point(20, 55), Size = new Size(480, 110) };
        AdicionarInfo(grpMovimento, "Turno:", movimento.Turno, 20);
        AdicionarInfo(grpMovimento, "Terminal:", movimento.Caixa, 45);
        AdicionarInfo(grpMovimento, "Impressora:", movimento.Impressora, 70);
        AdicionarInfo(grpMovimento, "Operador:", funcionarioLogado.Nome, 20, segundaColuna: true);
        AdicionarInfo(grpMovimento, "Data Movimento:", DateTime.Today.ToString("dd/MM/yyyy"), 45, segundaColuna: true);

        var grpEncerrantes = new GroupBox { Text = "Encerrantes do Movimento", Location = new Point(20, 175), Size = new Size(480, 260) };

        var lblTipo = new Label { Text = "Tipo de Lançamento:", Location = new Point(15, 25), AutoSize = true };
        _tipoLancamento.Location = new Point(15, 45);
        _tipoLancamento.Width = 220;
        _tipoLancamento.DropDownStyle = ComboBoxStyle.DropDownList;

        var lblValor = new Label { Text = "Valor do Documento:", Location = new Point(250, 25), AutoSize = true };
        _valorDocumento.Location = new Point(250, 45);
        _valorDocumento.Width = 120;
        _valorDocumento.DecimalPlaces = 2;
        _valorDocumento.Maximum = 999999;

        _adicionar.Text = "Adicionar";
        _adicionar.Width = 100;
        _adicionar.Location = new Point(15, 80);
        _adicionar.Image = ButtonIcons.Plus();
        _adicionar.TextImageRelation = TextImageRelation.ImageBeforeText;
        _adicionar.Click += Adicionar_Click;
        PdvTheme.EstilizarBotaoPrimario(_adicionar);

        _remover.Text = "Excluir";
        _remover.Width = 100;
        _remover.Location = new Point(125, 80);
        _remover.Image = ButtonIcons.Trash();
        _remover.TextImageRelation = TextImageRelation.ImageBeforeText;
        _remover.Enabled = false;
        _remover.Click += Remover_Click;
        PdvTheme.EstilizarBotaoSecundario(_remover);

        ConfigurarGrid();
        _grid.Location = new Point(15, 115);
        _grid.Size = new Size(450, 130);
        _grid.SelectionChanged += (_, _) => _remover.Enabled = _grid.CurrentRow is not null;

        grpEncerrantes.Controls.Add(lblTipo);
        grpEncerrantes.Controls.Add(_tipoLancamento);
        grpEncerrantes.Controls.Add(lblValor);
        grpEncerrantes.Controls.Add(_valorDocumento);
        grpEncerrantes.Controls.Add(_adicionar);
        grpEncerrantes.Controls.Add(_remover);
        grpEncerrantes.Controls.Add(_grid);

        var grpLogin = new GroupBox { Text = "Dados do Login", Location = new Point(20, 445), Size = new Size(480, 55) };
        var lblSenhaOp = new Label { Text = "Senha do Operador:", Location = new Point(15, 25), AutoSize = true };
        _senhaOperador.Location = new Point(160, 22);
        _senhaOperador.Width = 300;
        _senhaOperador.UseSystemPasswordChar = true;
        grpLogin.Controls.Add(lblSenhaOp);
        grpLogin.Controls.Add(_senhaOperador);

        var grpGerente = new GroupBox { Text = "Dados Gerente ou Supervisor", Location = new Point(20, 510), Size = new Size(480, 55) };
        var lblSenhaGer = new Label { Text = "Senha do Administrador:", Location = new Point(15, 25), AutoSize = true };
        _senhaGerente.Location = new Point(185, 22);
        _senhaGerente.Width = 275;
        _senhaGerente.UseSystemPasswordChar = true;
        grpGerente.Controls.Add(lblSenhaGer);
        grpGerente.Controls.Add(_senhaGerente);

        _erro.Location = new Point(20, 570);
        _erro.Size = new Size(480, 35);
        _erro.ForeColor = Color.Firebrick;

        Controls.Add(grpMovimento);
        Controls.Add(grpEncerrantes);
        Controls.Add(grpLogin);
        Controls.Add(grpGerente);
        Controls.Add(_erro);

        Shown += async (_, _) => await CarregarFormasAsync();
    }

    private static void AdicionarInfo(Control container, string rotulo, string valor, int top, bool segundaColuna = false)
    {
        var left = segundaColuna ? 250 : 15;
        container.Controls.Add(new Label { Text = rotulo, Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });
        container.Controls.Add(new Label { Text = valor, Location = new Point(left + 90, top), AutoSize = true });
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo Pagamento", Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "R$ Valor", Width = 150 });
    }

    private async Task CarregarFormasAsync()
    {
        try
        {
            _formas = await _db.GetFormasPagamentoAsync(CancellationToken.None);
            _tipoLancamento.Items.Clear();
            foreach (var f in _formas)
                _tipoLancamento.Items.Add(f.Descricao);
            if (_tipoLancamento.Items.Count > 0)
                _tipoLancamento.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao conectar: {ex.Message}";
        }
    }

    private void Adicionar_Click(object? sender, EventArgs e)
    {
        if (_tipoLancamento.SelectedIndex < 0 || _valorDocumento.Value <= 0)
            return;

        var descricao = _formas[_tipoLancamento.SelectedIndex].Descricao;
        _encerrantes.Add(new Encerrante(descricao, _valorDocumento.Value));
        _grid.Rows.Add(descricao, _valorDocumento.Value.ToString("N2", _ptBr));
        _valorDocumento.Value = 0;
    }

    private void Remover_Click(object? sender, EventArgs e)
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _encerrantes.Count)
            return;

        _encerrantes.RemoveAt(index);
        _grid.Rows.RemoveAt(index);
    }

    private async Task<bool> Confirmar()
    {
        if (_encerrantes.Count == 0)
        {
            _erro.Text = "Informe ao menos um encerrante antes de confirmar.";
            return false;
        }

        if (string.IsNullOrEmpty(_funcionarioLogado.Senha) == false && _funcionarioLogado.Senha != _senhaOperador.Text)
        {
            _erro.Text = "Senha do operador incorreta.";
            return false;
        }

        try
        {
            var gerente = await _db.ValidarGerenteAsync(_senhaGerente.Text, CancellationToken.None);
            if (gerente is null)
            {
                _erro.Text = "Senha do gerente/supervisor incorreta.";
                return false;
            }

            await _db.EncerrarCaixaAsync(_movimento.Id, _encerrantes, CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao encerrar o caixa: {ex.Message}";
            return false;
        }
    }
}
