namespace GestorPDV.Vendas;

/// <summary>
/// "Movimento Aberto - Confirme Dados": mostrada quando já existe um
/// movimento (turno de caixa) aberto no sistema. Mostra os dados desse
/// movimento e pede login/senha do operador para entrar nele.
/// </summary>
public sealed class ConfirmaLoginForm : Form
{
    private readonly Database _db;
    private readonly MovimentoInfo _movimento;

    private readonly TextBox _login = new();
    private readonly TextBox _senha = new();
    private readonly Label _erro = new();

    public Funcionario? FuncionarioLogado { get; private set; }
    public int IdOperador { get; private set; }

    public ConfirmaLoginForm(Database db, MovimentoInfo movimento)
    {
        _db = db;
        _movimento = movimento;

        Text = "GestorPDV - Login";
        ClientSize = new Size(460, 430);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("Movimento Aberto - Confirme Dados", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraAcoesDialogo(this, Confirmar));

        var subTitulo = new Label
        {
            Text = "Movimento aberto - confirme dados para acessar o movimento",
            Location = new Point(20, 55),
            Size = new Size(420, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        };

        var grpMovimento = new GroupBox { Text = "", Location = new Point(20, 80), Size = new Size(420, 130) };
        AdicionarInfo(grpMovimento, "Turno:", movimento.Turno, 15);
        AdicionarInfo(grpMovimento, "Terminal:", movimento.Caixa, 40);
        AdicionarInfo(grpMovimento, "Impressora:", movimento.Impressora, 65);
        AdicionarInfo(grpMovimento, "Data Início:", movimento.DataAbertura?.ToString("dd/MM/yyyy") ?? "", 90);
        AdicionarInfo(grpMovimento, "Hora Início:", movimento.HoraAbertura ?? "", 90, segundaColuna: true);

        var grpLogin = new GroupBox { Text = "Dados para Login", Location = new Point(20, 220), Size = new Size(420, 110) };
        var lblLogin = new Label { Text = "Operador:", Location = new Point(15, 25), AutoSize = true };
        _login.Location = new Point(120, 22);
        _login.Width = 280;

        var lblSenha = new Label { Text = "Senha:", Location = new Point(15, 60), AutoSize = true };
        _senha.Location = new Point(120, 57);
        _senha.Width = 280;
        _senha.UseSystemPasswordChar = true;

        grpLogin.Controls.Add(lblLogin);
        grpLogin.Controls.Add(_login);
        grpLogin.Controls.Add(lblSenha);
        grpLogin.Controls.Add(_senha);

        _erro.Location = new Point(20, 335);
        _erro.Size = new Size(420, 35);
        _erro.ForeColor = Color.Firebrick;

        Controls.Add(subTitulo);
        Controls.Add(grpMovimento);
        Controls.Add(grpLogin);
        Controls.Add(_erro);

        Shown += (_, _) => _login.Focus();
    }

    private static void AdicionarInfo(Control container, string rotulo, string valor, int top, bool segundaColuna = false)
    {
        var left = segundaColuna ? 220 : 15;
        container.Controls.Add(new Label { Text = rotulo, Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });
        container.Controls.Add(new Label { Text = valor, Location = new Point(left + 85, top), AutoSize = true });
    }

    private async Task<bool> Confirmar()
    {
        var login = _login.Text.Trim();
        if (login.Length == 0)
        {
            _erro.Text = "Informe o login do operador.";
            return false;
        }

        Funcionario? funcionario;
        try
        {
            funcionario = await _db.GetFuncionarioPorLoginAsync(login, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _erro.Text = $"Erro ao consultar o banco: {ex.Message}";
            return false;
        }

        if (funcionario is null)
        {
            _erro.Text = "Operador não encontrado.";
            return false;
        }

        if (!string.IsNullOrEmpty(funcionario.Senha) && funcionario.Senha != _senha.Text)
        {
            _erro.Text = "Senha incorreta.";
            return false;
        }

        IdOperador = await _db.GetOrCreateOperadorAsync(funcionario.Id, CancellationToken.None);
        FuncionarioLogado = funcionario;
        return true;
    }
}
