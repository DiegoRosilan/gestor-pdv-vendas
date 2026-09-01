using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// "F3 - Selecione o Desconto/Acréscimo...": aplica um desconto ou
/// acréscimo no total da venda (em R$ ou %, além do desconto por item já
/// lançado na tela anterior), lança a(s) forma(s) de pagamento e finaliza.
/// </summary>
public sealed class FechaVendaForm : Form
{
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly decimal _subtotalItens;
    private readonly List<FormaPagamento> _formas;

    private readonly ComboBox _tipoDescontoAcrescimo = new();
    private readonly NumericUpDown _valorDescontoAcrescimo = new();

    private readonly ComboBox _formaCombo = new();
    private readonly NumericUpDown _valor = new();
    private readonly ListBox _lancados = new();

    private readonly Label _totalVendaValor = new();
    private readonly Label _descontoValor = new();
    private readonly Label _totalReceberValor = new();
    private readonly Label _trocoValor = new();

    private readonly Label _parcelasLabel = new();
    private readonly NumericUpDown _parcelas = new();
    private readonly Label _vencimentoLabel = new();
    private readonly DateTimePicker _vencimento = new();

    private string? _observacao;

    public List<PagamentoLancado> Pagamentos { get; } = new();
    public decimal DescontoGeral { get; private set; }
    public decimal AcrescimoGeral { get; private set; }
    public string? Observacao => _observacao;
    public int ParcelasCrediario { get; private set; }
    public DateOnly PrimeiroVencimentoCrediario { get; private set; }

    public FechaVendaForm(decimal subtotalItens, List<FormaPagamento> formas)
    {
        _subtotalItens = subtotalItens;
        _formas = formas;

        Text = "F3 - Selecione o Desconto/Acréscimo...";
        ClientSize = new Size(760, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo(Text, ClientSize.Width));
        Controls.Add(CriarBarraTeclasFechamento());

        // ---- Coluna esquerda ----
        var lblDescAcr = new Label { Text = "Desconto/Acréscimo:", Location = new Point(20, 55), AutoSize = true };
        _tipoDescontoAcrescimo.Location = new Point(20, 75);
        _tipoDescontoAcrescimo.Width = 230;
        _tipoDescontoAcrescimo.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipoDescontoAcrescimo.Items.AddRange(new object[]
        {
            "Nenhum", "Desconto Dinheiro (R$)", "Desconto Percentual (%)",
            "Acréscimo Dinheiro (R$)", "Acréscimo Percentual (%)",
        });
        _tipoDescontoAcrescimo.SelectedIndex = 0;
        _tipoDescontoAcrescimo.SelectedIndexChanged += (_, _) => AtualizarResumo();

        _valorDescontoAcrescimo.Location = new Point(260, 75);
        _valorDescontoAcrescimo.Width = 100;
        _valorDescontoAcrescimo.DecimalPlaces = 2;
        _valorDescontoAcrescimo.Maximum = 999999;
        _valorDescontoAcrescimo.ValueChanged += (_, _) => AtualizarResumo();

        var lblForma = new Label { Text = "Forma de Pagamento e Valor:", Location = new Point(20, 115), AutoSize = true };
        _formaCombo.Location = new Point(20, 135);
        _formaCombo.Width = 230;
        _formaCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var f in _formas)
            _formaCombo.Items.Add(f.Descricao);
        if (_formaCombo.Items.Count > 0)
            _formaCombo.SelectedIndex = 0;

        _valor.Location = new Point(260, 135);
        _valor.Width = 100;
        _valor.DecimalPlaces = 2;
        _valor.Maximum = 999999;
        _valor.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Adicionar(_valor.Value); } };

        var lblValores = new Label { Text = "Valores Informados:", Location = new Point(20, 170), AutoSize = true };
        _lancados.Location = new Point(20, 190);
        _lancados.Size = new Size(340, 150);

        _parcelasLabel.Text = "Nº de parcelas (crediário):";
        _parcelasLabel.Location = new Point(20, 350);
        _parcelasLabel.AutoSize = true;
        _parcelasLabel.Visible = false;

        _parcelas.Location = new Point(200, 348);
        _parcelas.Width = 60;
        _parcelas.Minimum = 1;
        _parcelas.Maximum = 48;
        _parcelas.Value = 1;
        _parcelas.Visible = false;

        _vencimentoLabel.Text = "1º vencimento:";
        _vencimentoLabel.Location = new Point(20, 380);
        _vencimentoLabel.AutoSize = true;
        _vencimentoLabel.Visible = false;

        _vencimento.Location = new Point(200, 376);
        _vencimento.Width = 150;
        _vencimento.Format = DateTimePickerFormat.Short;
        _vencimento.Value = DateTime.Today.AddMonths(1);
        _vencimento.Visible = false;

        // ---- Coluna direita: Resumo da Venda ----
        var grpResumo = new GroupBox { Text = "", Location = new Point(400, 55), Size = new Size(340, 260) };
        var lblResumoTitulo = new Label
        {
            Text = "Resumo da Venda", Location = new Point(0, 5), Size = new Size(340, 25),
            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = PdvTheme.CaixaLeitura,
        };
        CriarLinhaResumo(grpResumo, "Total Venda:", _totalVendaValor, 45, Color.Black);
        CriarLinhaResumo(grpResumo, "Desconto:", _descontoValor, 90, Color.Firebrick);
        CriarLinhaResumo(grpResumo, "Total a Receber:", _totalReceberValor, 135, Color.DarkGreen);
        CriarLinhaResumo(grpResumo, "Troco:", _trocoValor, 180, Color.DarkBlue);
        grpResumo.Controls.Add(lblResumoTitulo);

        Controls.Add(lblDescAcr);
        Controls.Add(_tipoDescontoAcrescimo);
        Controls.Add(_valorDescontoAcrescimo);
        Controls.Add(lblForma);
        Controls.Add(_formaCombo);
        Controls.Add(_valor);
        Controls.Add(lblValores);
        Controls.Add(_lancados);
        Controls.Add(_parcelasLabel);
        Controls.Add(_parcelas);
        Controls.Add(_vencimentoLabel);
        Controls.Add(_vencimento);
        Controls.Add(grpResumo);

        AtualizarResumo();
    }

    private static void CriarLinhaResumo(Control container, string rotulo, Label valorLabel, int top, Color cor)
    {
        container.Controls.Add(new Label { Text = rotulo, Location = new Point(15, top), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        valorLabel.Location = new Point(180, top - 3);
        valorLabel.Size = new Size(140, 25);
        valorLabel.TextAlign = ContentAlignment.MiddleRight;
        valorLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        valorLabel.ForeColor = cor;
        container.Controls.Add(valorLabel);
    }

    private Panel CriarBarraTeclasFechamento()
    {
        var barra = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.White };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(15, 8, 0, 0) };

        void Item(string texto, EventHandler acao)
        {
            var lbl = new LinkLabel { Text = texto, AutoSize = true, Margin = new Padding(0, 0, 25, 0), LinkColor = Color.DarkSlateBlue };
            lbl.Click += acao;
            flow.Controls.Add(lbl);
        }

        Item("F2-Finaliza Rápido", (_, _) => FinalizaRapido());
        Item("F5-Excluir", (_, _) => Excluir_Click());
        Item("F7-Obs", (_, _) => Observacao_Click());
        Item("F12-Finaliza", (_, _) => Finalizar_Click());
        Item("ESC-Cancela", (_, _) => { DialogResult = DialogResult.Cancel; Close(); });

        barra.Controls.Add(flow);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.F2: e.Handled = true; FinalizaRapido(); break;
                case Keys.F5: e.Handled = true; Excluir_Click(); break;
                case Keys.F7: e.Handled = true; Observacao_Click(); break;
                case Keys.F12: e.Handled = true; Finalizar_Click(); break;
                case Keys.Escape: e.Handled = true; DialogResult = DialogResult.Cancel; Close(); break;
            }
        };

        return barra;
    }

    private decimal TotalAReceber
    {
        get
        {
            var total = _subtotalItens - DescontoGeral + AcrescimoGeral;
            return Math.Max(0m, total);
        }
    }

    private void AtualizarResumo()
    {
        DescontoGeral = 0;
        AcrescimoGeral = 0;
        switch (_tipoDescontoAcrescimo.SelectedIndex)
        {
            case 1: DescontoGeral = _valorDescontoAcrescimo.Value; break;
            case 2: DescontoGeral = Math.Round(_subtotalItens * _valorDescontoAcrescimo.Value / 100m, 2); break;
            case 3: AcrescimoGeral = _valorDescontoAcrescimo.Value; break;
            case 4: AcrescimoGeral = Math.Round(_subtotalItens * _valorDescontoAcrescimo.Value / 100m, 2); break;
        }

        _totalVendaValor.Text = _subtotalItens.ToString("N2", _ptBr);
        _descontoValor.Text = (DescontoGeral - AcrescimoGeral).ToString("N2", _ptBr);
        _totalReceberValor.Text = TotalAReceber.ToString("N2", _ptBr);

        var pago = Pagamentos.Sum(p => p.Valor);
        _trocoValor.Text = Math.Max(0m, pago - TotalAReceber).ToString("N2", _ptBr);

        var temCrediario = Pagamentos.Any(p => p.Forma.GeraParcelas);
        _parcelasLabel.Visible = temCrediario;
        _parcelas.Visible = temCrediario;
        _vencimentoLabel.Visible = temCrediario;
        _vencimento.Visible = temCrediario;

        _valor.Value = Math.Clamp(Math.Max(TotalAReceber - pago, 0), _valor.Minimum, _valor.Maximum);
    }

    private void AtualizarListaLancados()
    {
        _lancados.Items.Clear();
        if (Pagamentos.Count == 0)
        {
            _lancados.Items.Add("(Sem  pagamentos)");
        }
        else
        {
            foreach (var p in Pagamentos)
                _lancados.Items.Add($"{p.Forma.Descricao} - R$ {p.Valor.ToString("N2", _ptBr)}");
        }

        AtualizarResumo();
    }

    private void Adicionar(decimal valor)
    {
        if (_formaCombo.SelectedIndex < 0 || valor <= 0)
            return;

        Pagamentos.Add(new PagamentoLancado { Forma = _formas[_formaCombo.SelectedIndex], Valor = valor });
        AtualizarListaLancados();
    }

    private void FinalizaRapido()
    {
        // Lança o restante inteiro na forma selecionada e já finaliza.
        Adicionar(TotalAReceber - Pagamentos.Sum(p => p.Valor));
        Finalizar_Click();
    }

    private void Excluir_Click()
    {
        // >= Pagamentos.Count cobre o item "(Sem pagamentos)" mostrado
        // quando a lista está vazia — ele não corresponde a nenhum
        // pagamento de verdade.
        if (_lancados.SelectedIndex < 0 || _lancados.SelectedIndex >= Pagamentos.Count)
            return;

        Pagamentos.RemoveAt(_lancados.SelectedIndex);
        AtualizarListaLancados();
    }

    private void Observacao_Click()
    {
        using var dlg = new Form
        {
            Text = "Observação da venda",
            ClientSize = new Size(400, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };
        var texto = new TextBox { Location = new Point(15, 15), Size = new Size(370, 60), Multiline = true, Text = _observacao ?? "" };
        var ok = new Button { Text = "OK (F12)", Location = new Point(210, 90), Width = 90, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancelar", Location = new Point(305, 90), Width = 80, DialogResult = DialogResult.Cancel };
        dlg.Controls.Add(texto);
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _observacao = texto.Text.Trim();
    }

    private void Finalizar_Click()
    {
        _tipoDescontoAcrescimo.Focus(); // fecha edições pendentes do NumericUpDown
        AtualizarResumo();

        if (Pagamentos.Count == 0 || Pagamentos.Sum(p => p.Valor) < TotalAReceber)
        {
            MessageBox.Show("Lance forma(s) de pagamento que cubram o total a receber.", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (Pagamentos.Any(p => p.Forma.GeraParcelas))
        {
            ParcelasCrediario = (int)_parcelas.Value;
            PrimeiroVencimentoCrediario = DateOnly.FromDateTime(_vencimento.Value);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
