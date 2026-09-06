using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// "Inicia Movimento do Terminal de Caixa": mostrada quando não existe
/// nenhum movimento aberto no sistema. Escolhe o turno e o terminal
/// (caixa), informa o fundo de caixa inicial (suprimento) e confirma com
/// login/senha do operador + senha do gerente/supervisor.
/// </summary>
public sealed class AberturaTurnoForm : Form
{
    private readonly Database _db;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly DataGridView _turnos = new();
    private readonly ComboBox _caixas = new();
    private readonly NumericUpDown _suprimento = new();
    private readonly TextBox _loginOperador = new();
    private readonly TextBox _senhaOperador = new();
    private readonly TextBox _senhaGerente = new();
    private readonly Label _erro = new();

    private List<Turno> _listaTurnos = new();
    private List<Caixa> _listaCaixas = new();

    public int? IdMovimentoAberto { get; private set; }
    public Funcionario? OperadorLogado { get; private set; }
    public int IdOperador { get; private set; }

    public AberturaTurnoForm(Database db)
    {
        _db = db;

        Text = "GestorPDV - Abertura de turno";
        ClientSize = new Size(480, 590);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Inicia Movimento do Terminal de Caixa", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraAcoesDialogo(this, Confirmar));

        var lblTurnos = new Label { Text = "Turno(s):", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        ConfigurarGridTurnos();
        _turnos.Location = new Point(20, 75);
        _turnos.Size = new Size(440, 110);

        var lblCaixa = new Label { Text = "Terminal (caixa):", Location = new Point(20, 195), AutoSize = true };
        _caixas.Location = new Point(150, 192);
        _caixas.Width = 200;
        _caixas.DropDownStyle = ComboBoxStyle.DropDownList;

        var grpSuprimento = new GroupBox { Text = "Fundo de Caixa (Suprimento/Reforço)", Location = new Point(20, 225), Size = new Size(440, 55) };
        _suprimento.Location = new Point(15, 22);
        _suprimento.Width = 150;
        _suprimento.DecimalPlaces = 2;
        _suprimento.Maximum = 999999;
        grpSuprimento.Controls.Add(_suprimento);

        var grpOperador = new GroupBox { Text = "Dados Operador", Location = new Point(20, 290), Size = new Size(440, 110) };
        var lblLoginOp = new Label { Text = "Login:", Location = new Point(15, 25), AutoSize = true };
        _loginOperador.Location = new Point(120, 22);
        _loginOperador.Width = 290;
        var lblSenhaOp = new Label { Text = "Senha:", Location = new Point(15, 60), AutoSize = true };
        _senhaOperador.Location = new Point(120, 57);
        _senhaOperador.Width = 290;
        _senhaOperador.UseSystemPasswordChar = true;
        grpOperador.Controls.Add(lblLoginOp);
        grpOperador.Controls.Add(_loginOperador);
        grpOperador.Controls.Add(lblSenhaOp);
        grpOperador.Controls.Add(_senhaOperador);

        var grpGerente = new GroupBox { Text = "Dados Gerente/Supervisor", Location = new Point(20, 410), Size = new Size(440, 65) };
        var lblSenhaGer = new Label { Text = "Senha:", Location = new Point(15, 28), AutoSize = true };
        _senhaGerente.Location = new Point(120, 25);
        _senhaGerente.Width = 290;
        _senhaGerente.UseSystemPasswordChar = true;
        grpGerente.Controls.Add(lblSenhaGer);
        grpGerente.Controls.Add(_senhaGerente);

        _erro.Location = new Point(20, 480);
        _erro.Size = new Size(440, 40);
        _erro.ForeColor = Color.Firebrick;

        Controls.Add(lblTurnos);
        Controls.Add(_turnos);
        Controls.Add(lblCaixa);
        Controls.Add(_caixas);
        Controls.Add(grpSuprimento);
        Controls.Add(grpOperador);
        Controls.Add(grpGerente);
        Controls.Add(_erro);

        Shown += async (_, _) => await CarregarListasAsync();
    }

    private void ConfigurarGridTurnos()
    {
        _turnos.AutoGenerateColumns = false;
        _turnos.AllowUserToAddRows = false;
        _turnos.AllowUserToDeleteRows = false;
        _turnos.ReadOnly = true;
        _turnos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _turnos.MultiSelect = false;
        _turnos.RowHeadersVisible = false;
        PdvTheme.EstilizarGrid(_turnos);

        _turnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "Descrição", Width = 200 });
        _turnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Inicio", HeaderText = "Hora Início", Width = 110 });
        _turnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fim", HeaderText = "Hora Fim", Width = 110 });
    }

    private async Task CarregarListasAsync()
    {
        try
        {
            _listaTurnos = await _db.GetTurnosAsync(CancellationToken.None);
            _turnos.Rows.Clear();
            foreach (var t in _listaTurnos)
                _turnos.Rows.Add(t.Descricao, t.HoraInicio, t.HoraFim);
            if (_turnos.Rows.Count > 0)
                _turnos.Rows[0].Selected = true;

            _listaCaixas = await _db.GetCaixasAsync(CancellationToken.None);
            _caixas.Items.Clear();
            foreach (var c in _listaCaixas)
                _caixas.Items.Add(c.Nome);
            if (_caixas.Items.Count > 0)
                _caixas.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao conectar: {ex.Message}";
        }
    }

    private async Task<bool> Confirmar()
    {
        var indiceTurno = _turnos.CurrentRow?.Index ?? -1;
        if (indiceTurno < 0 || indiceTurno >= _listaTurnos.Count)
        {
            _erro.Text = "Selecione um turno.";
            return false;
        }

        if (_caixas.SelectedIndex < 0)
        {
            _erro.Text = "Selecione o terminal (caixa).";
            return false;
        }

        var loginOperador = _loginOperador.Text.Trim();
        if (loginOperador.Length == 0)
        {
            _erro.Text = "Informe o login do operador.";
            return false;
        }

        try
        {
            var operador = await _db.GetFuncionarioPorLoginAsync(loginOperador, CancellationToken.None);
            if (operador is null)
            {
                _erro.Text = "Operador não encontrado.";
                return false;
            }

            if (!string.IsNullOrEmpty(operador.Senha) && operador.Senha != _senhaOperador.Text)
            {
                _erro.Text = "Senha do operador incorreta.";
                return false;
            }

            var gerente = await _db.ValidarGerenteAsync(_senhaGerente.Text, CancellationToken.None);
            if (gerente is null)
            {
                _erro.Text = "Senha do gerente/supervisor incorreta.";
                return false;
            }

            var turno = _listaTurnos[indiceTurno];
            var caixa = _listaCaixas[_caixas.SelectedIndex];
            IdMovimentoAberto = await _db.AbrirCaixaAsync(
                operador.Id, gerente.Id, caixa.Id, turno.Id, _suprimento.Value,
                CancellationToken.None);
            IdOperador = await _db.GetOrCreateOperadorAsync(operador.Id, CancellationToken.None);
            OperadorLogado = operador;
            return true;
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao abrir caixa: {ex.Message}";
            return false;
        }
    }
}
