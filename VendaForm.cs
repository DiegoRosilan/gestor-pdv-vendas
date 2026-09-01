using System.Globalization;

namespace GestorPDV.Vendas;

/// <summary>
/// Tela principal de venda: adicionar itens, ajustar quantidade/desconto,
/// identificar cliente, fechar a venda (desconto/acréscimo geral + forma
/// de pagamento) e finalizar. Ao finalizar, grava a venda com
/// status_venda='F' nas mesmas tabelas do GestorPDV original — se o
/// GestorPDV.Monitor estiver rodando (e já tiver criado o gatilho
/// trg_gestorpdv_venda_fechada na primeira vez que ele rodou), a venda cai
/// automaticamente na fila de impressão dele, sem nenhuma integração
/// extra necessária aqui.
/// </summary>
public sealed class VendaForm : Form
{
    private readonly Database _db;
    private readonly AppConfig _config;
    private readonly Funcionario _funcionario;
    private readonly int _idOperador;
    private readonly int _idMovimento;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly TextBox _codigo = new();
    private readonly Button _buscarProduto = new();

    private readonly DataGridView _grid = new();
    private readonly Button _atualizarItem = new();

    private readonly Label _clienteLabel = new();

    private Label _valorUnitarioValor = null!;
    private Label _valorTotalValor = null!;

    private readonly Label _statusBar = new();
    private readonly System.Windows.Forms.Timer _relogio = new();

    private readonly List<ItemCarrinho> _carrinho = new();
    private Cliente? _cliente;
    private MovimentoInfo? _movimentoInfo;

    public VendaForm(Database db, AppConfig config, Funcionario funcionario, int idOperador, int idMovimento)
    {
        _db = db;
        _config = config;
        _funcionario = funcionario;
        _idOperador = idOperador;
        _idMovimento = idMovimento;

        Text = "GestorPDV - Venda";
        ClientSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        Controls.Add(PdvTheme.CriarBarraTitulo("GestorPDV", ClientSize.Width));
        Controls.Add(PdvTheme.CriarBarraFuncoes(this, TeclasFuncao()));
        Controls.Add(CriarBarraStatus());
        Controls.Add(CriarPainelEntrada());

        ConfigurarGrid();
        _grid.Location = new Point(20, 60);
        _grid.Size = new Size(960, 300);
        _grid.SelectionChanged += Grid_SelectionChanged;
        Controls.Add(_grid);

        _clienteLabel.Text = "Cliente: (nenhum identificado — F7)";
        _clienteLabel.Location = new Point(20, 365);
        _clienteLabel.AutoSize = true;
        _clienteLabel.Font = new Font("Segoe UI", 9.5f);
        Controls.Add(_clienteLabel);

        AtualizarGrid();

        _relogio.Interval = 1000;
        _relogio.Tick += (_, _) => AtualizarBarraStatus();
        _relogio.Start();

        Shown += async (_, _) =>
        {
            try
            {
                _movimentoInfo = await _db.GetMovimentoInfoAsync(_idMovimento, CancellationToken.None);
            }
            catch { /* status bar só perde a informação de turno/terminal, não impede vender */ }
            AtualizarBarraStatus();
            _codigo.Focus();
        };

        FormClosing += (_, _) => _relogio.Stop();
    }

    private Panel CriarBarraStatus()
    {
        var barra = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Color.White };
        _statusBar.Dock = DockStyle.Fill;
        _statusBar.TextAlign = ContentAlignment.MiddleLeft;
        _statusBar.Padding = new Padding(10, 0, 0, 0);
        _statusBar.Font = new Font("Segoe UI", 8.5f);
        barra.Controls.Add(_statusBar);
        return barra;
    }

    private void AtualizarBarraStatus()
    {
        var turno = _movimentoInfo?.Turno ?? "-";
        var terminal = _movimentoInfo?.Caixa ?? "-";
        _statusBar.Text = $"{DateTime.Now:HH:mm:ss}    Terminal: {terminal}    Operador: {_funcionario.Nome}    Turno: {turno}    Movimento nº {_idMovimento}";
    }

    private List<PdvTheme.TeclaFuncao> TeclasFuncao() => new()
    {
        new(1, "Opções", null, Habilitada: false),
        new(2, "Produtos", (_, _) => AbrirBuscaProduto()),
        new(3, "Fecha\nVenda", (_, _) => FecharVenda()),
        new(4, "Cancela\nVenda", (_, _) => CancelarVenda()),
        new(5, "Cancela\nItem", (_, _) => RemoverItemSelecionado()),
        new(6, "Preços", null, Habilitada: false),
        new(7, "Clientes", (_, _) => IdentificarCliente()),
        new(8, "Menu\nFiscal", (_, _) => AbrirMenuFiscal()),
        new(9, "Pré-Venda", null, Habilitada: false),
        new(10, "Orçamento", null, Habilitada: false),
        new(11, "Vendedor", null, Habilitada: false),
        new(12, "Sair", (_, _) => Sair()),
    };

    private Panel CriarPainelEntrada()
    {
        var painel = new Panel { Location = new Point(20, 390), Size = new Size(960, 130) };

        var lblCodigo = new Label { Text = "Código/Descrição do Produto:", Location = new Point(0, 0), AutoSize = true };
        _codigo.Location = new Point(0, 20);
        _codigo.Width = 560;
        _codigo.Font = new Font("Segoe UI", 11);
        _codigo.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = AdicionarPorCodigoAsync(); } };

        _buscarProduto.Text = "";
        _buscarProduto.Size = new Size(34, 27);
        _buscarProduto.Location = new Point(560, 20);
        _buscarProduto.Image = ButtonIcons.Search();
        _buscarProduto.Click += (_, _) => AbrirBuscaProduto();

        var caixaQtd = PdvTheme.CriarCaixaLeitura("Quantidade", out var lblQtdValor);
        caixaQtd.Location = new Point(0, 60);
        _labelQuantidadeValor = lblQtdValor;

        var caixaUnit = PdvTheme.CriarCaixaLeitura("Valor Unitário", out var lblUnitValor);
        caixaUnit.Location = new Point(220, 60);
        _valorUnitarioValor = lblUnitValor;

        var caixaTotal = PdvTheme.CriarCaixaLeitura("Valor Total", out var lblTotalValor);
        caixaTotal.Location = new Point(440, 60);
        _valorTotalValor = lblTotalValor;

        var lblAjuste = new Label { Text = "Item selecionado — Qtd:", Location = new Point(660, 65), AutoSize = true };
        var qtdEdit = new NumericUpDown { Location = new Point(800, 62), Width = 70, DecimalPlaces = 3, Minimum = 0.001m, Maximum = 99999, Value = 1 };
        var lblDescEdit = new Label { Text = "Desc. R$:", Location = new Point(660, 95), AutoSize = true };
        var descEdit = new NumericUpDown { Location = new Point(800, 92), Width = 70, DecimalPlaces = 2, Maximum = 999999 };
        _atualizarItem.Text = "Atualizar";
        _atualizarItem.Width = 90;
        _atualizarItem.Location = new Point(880, 78);
        _atualizarItem.Enabled = false;
        _qtdEditControl = qtdEdit;
        _descEditControl = descEdit;
        _atualizarItem.Click += (_, _) => AtualizarItemSelecionado();

        painel.Controls.Add(lblCodigo);
        painel.Controls.Add(_codigo);
        painel.Controls.Add(_buscarProduto);
        painel.Controls.Add(caixaQtd);
        painel.Controls.Add(caixaUnit);
        painel.Controls.Add(caixaTotal);
        painel.Controls.Add(lblAjuste);
        painel.Controls.Add(qtdEdit);
        painel.Controls.Add(lblDescEdit);
        painel.Controls.Add(descEdit);
        painel.Controls.Add(_atualizarItem);

        return painel;
    }

    private Label _labelQuantidadeValor = null!;
    private NumericUpDown _qtdEditControl = null!;
    private NumericUpDown _descEditControl = null!;

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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item", Width = 50 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "Descrição", Width = 350 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtde", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unitario", HeaderText = "Unitário", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desconto", HeaderText = "Desconto", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total", Width = 100 });
    }

    private async Task AdicionarPorCodigoAsync()
    {
        var codigo = _codigo.Text.Trim();
        if (codigo.Length == 0)
            return;

        try
        {
            var produto = await _db.BuscarProdutoPorCodigoAsync(codigo, CancellationToken.None);
            if (produto is null)
            {
                MessageBox.Show($"Nenhum produto encontrado com o código \"{codigo}\".", "GestorPDV",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AdicionarProdutoAoCarrinho(produto);
            _codigo.Clear();
            _codigo.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao buscar produto: {ex.Message}", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AbrirBuscaProduto()
    {
        using var form = new ProdutoSearchForm(_db);
        if (form.ShowDialog(this) == DialogResult.OK && form.ProdutoSelecionado is not null)
            AdicionarProdutoAoCarrinho(form.ProdutoSelecionado);
    }

    private void AdicionarProdutoAoCarrinho(Produto produto)
    {
        var existente = _carrinho.FirstOrDefault(i => i.Produto.Id == produto.Id);
        if (existente is not null)
            existente.Quantidade += 1;
        else
            _carrinho.Add(new ItemCarrinho { Produto = produto, Quantidade = 1, ValorUnitario = produto.ValorVenda });

        AtualizarGrid();

        var ultimo = _carrinho.Last();
        _labelQuantidadeValor.Text = ultimo.Quantidade.ToString("0.###", _ptBr);
        _valorUnitarioValor.Text = ultimo.ValorUnitario.ToString("N2", _ptBr);
        _valorTotalValor.Text = ultimo.Total.ToString("N2", _ptBr);
    }

    private void AtualizarGrid()
    {
        _grid.Rows.Clear();
        var item = 1;
        foreach (var i in _carrinho)
        {
            var codigo = i.Produto.CodProduto ?? i.Produto.CodigoInterno ?? i.Produto.Id.ToString();
            _grid.Rows.Add(
                item, codigo, i.Produto.Nome,
                i.Quantidade.ToString("0.###", _ptBr),
                i.ValorUnitario.ToString("N2", _ptBr),
                i.Desconto.ToString("N2", _ptBr),
                i.Total.ToString("N2", _ptBr));
            item++;
        }
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        var temSelecao = index >= 0 && index < _carrinho.Count;
        _atualizarItem.Enabled = temSelecao;

        if (temSelecao)
        {
            var item = _carrinho[index];
            _qtdEditControl.Value = Math.Clamp(item.Quantidade, _qtdEditControl.Minimum, _qtdEditControl.Maximum);
            _descEditControl.Value = Math.Clamp(item.Desconto, _descEditControl.Minimum, _descEditControl.Maximum);
            _labelQuantidadeValor.Text = item.Quantidade.ToString("0.###", _ptBr);
            _valorUnitarioValor.Text = item.ValorUnitario.ToString("N2", _ptBr);
            _valorTotalValor.Text = item.Total.ToString("N2", _ptBr);
        }
    }

    private void AtualizarItemSelecionado()
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _carrinho.Count)
            return;

        _carrinho[index].Quantidade = _qtdEditControl.Value;
        _carrinho[index].Desconto = _descEditControl.Value;
        AtualizarGrid();
        _grid.Rows[index].Selected = true;
    }

    private void RemoverItemSelecionado()
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _carrinho.Count)
            return;

        _carrinho.RemoveAt(index);
        AtualizarGrid();
    }

    private void AbrirMenuFiscal()
    {
        using var form = new MenuFiscalForm(_db, _config, _idMovimento);
        form.ShowDialog(this);
    }

    private void IdentificarCliente()
    {
        using var form = new ClienteSearchForm(_db);
        if (form.ShowDialog(this) == DialogResult.OK && form.ClienteSelecionado is not null)
        {
            _cliente = form.ClienteSelecionado;
            _clienteLabel.Text = $"Cliente: {_cliente.Nome}";
        }
    }

    private async void FecharVenda()
    {
        if (_carrinho.Count == 0)
        {
            MessageBox.Show("Adicione ao menos um item antes de fechar a venda.", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        List<FormaPagamento> formas;
        try
        {
            formas = await _db.GetFormasPagamentoAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar formas de pagamento: {ex.Message}", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var subtotal = _carrinho.Sum(i => i.Total);
        using var form = new FechaVendaForm(subtotal, formas);
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var idVenda = await _db.FinalizarVendaAsync(
                _idMovimento, _funcionario.Id, _idOperador,
                _cliente, _carrinho, form.Pagamentos,
                form.DescontoGeral, form.AcrescimoGeral, form.Observacao,
                form.ParcelasCrediario, form.PrimeiroVencimentoCrediario,
                CancellationToken.None);

            MessageBox.Show($"Venda nº {idVenda} finalizada com sucesso.", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            LimparVenda();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao finalizar a venda:{Environment.NewLine}{ex.Message}", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CancelarVenda()
    {
        if (_carrinho.Count == 0)
            return;

        var confirmar = MessageBox.Show(
            "Cancelar a venda em andamento? Os itens lançados serão descartados.",
            "GestorPDV", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmar == DialogResult.Yes)
            LimparVenda();
    }

    private void LimparVenda()
    {
        _carrinho.Clear();
        _cliente = null;
        _clienteLabel.Text = "Cliente: (nenhum identificado — F7)";
        AtualizarGrid();
        _labelQuantidadeValor.Text = "0,00";
        _valorUnitarioValor.Text = "0,00";
        _valorTotalValor.Text = "0,00";
        _codigo.Focus();
    }

    private async void Sair()
    {
        if (_carrinho.Count > 0)
        {
            MessageBox.Show("Finalize ou cancele a venda em andamento antes de sair.", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var encerrar = MessageBox.Show(
            "Deseja encerrar o turno (fechar o caixa) antes de sair?",
            "GestorPDV", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        if (encerrar == DialogResult.Cancel)
            return;

        if (encerrar == DialogResult.Yes)
        {
            var info = _movimentoInfo ?? await _db.GetMovimentoInfoAsync(_idMovimento, CancellationToken.None);
            if (info is null)
            {
                MessageBox.Show("Não foi possível carregar os dados do movimento para encerrar.", "GestorPDV",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var form = new EncerraCaixaForm(_db, _funcionario, info);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            MessageBox.Show("Turno encerrado com sucesso.", "GestorPDV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        Close();
    }
}
