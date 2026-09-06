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
///
/// Layout desta tela segue à risca o print de referência do sistema
/// original: cabeçalho alto, grid de itens à esquerda (com o total geral
/// logo abaixo), e a coluna direita com a marca da empresa e os campos de
/// entrada (código, quantidade, valor unitário e valor total).
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
    private readonly Label _totalGeralValor = new();

    private readonly Label _clienteLabel = new();

    private Label _labelQuantidadeValor = null!;
    private Label _valorUnitarioValor = null!;
    private Label _valorTotalValor = null!;

    private readonly Label _statusLinha1 = new();
    private readonly Label _statusLinha2 = new();
    private readonly Label _statusOff = new();
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
        ClientSize = new Size(1365, 767);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = PdvTheme.CorpoClaro;
        Font = PdvTheme.FontePadrao;

        // Ordem importa: dois Dock=Bottom empilham na ordem de adição, o
        // primeiro fica na borda externa (a barra de teclas precisa ficar
        // por último, na base) — por isso ela entra antes da de status.
        Controls.Add(PdvTheme.CriarBarraFuncoes(this, TeclasFuncao()));
        Controls.Add(CriarBarraStatus());
        Controls.Add(CriarPainelEsquerdo());
        Controls.Add(CriarPainelDireito());
        Controls.Add(PdvTheme.CriarCabecalhoPrincipal("GestorPDV"));

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

    // ---- Cabeçalho / rodapé ----

    private Panel CriarBarraStatus()
    {
        var barra = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = PdvTheme.BarraEscura };

        var linha1 = new Panel { Dock = DockStyle.Top, Height = 20 };
        _statusLinha1.Dock = DockStyle.Fill;
        _statusLinha1.ForeColor = PdvTheme.TextoClaro;
        _statusLinha1.TextAlign = ContentAlignment.MiddleLeft;
        _statusLinha1.Padding = new Padding(10, 0, 0, 0);
        _statusLinha1.Font = new Font("Segoe UI", 8.5f);
        _statusOff.Dock = DockStyle.Right;
        _statusOff.Width = 60;
        _statusOff.TextAlign = ContentAlignment.MiddleRight;
        _statusOff.Padding = new Padding(0, 0, 10, 0);
        _statusOff.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        _statusOff.ForeColor = PdvTheme.Perigo;
        _statusOff.Text = "OFF"; // sem impressora fiscal/SAT real conectada nesta versão — ver LEIA-ME.
        linha1.Controls.Add(_statusLinha1);
        linha1.Controls.Add(_statusOff);

        var linha2 = new Panel { Dock = DockStyle.Top, Height = 20 };
        _statusLinha2.Dock = DockStyle.Fill;
        _statusLinha2.ForeColor = PdvTheme.TextoClaro;
        _statusLinha2.TextAlign = ContentAlignment.MiddleLeft;
        _statusLinha2.Padding = new Padding(10, 0, 0, 0);
        _statusLinha2.Font = new Font("Segoe UI", 8.5f);
        var versao = new Label
        {
            Dock = DockStyle.Right,
            Width = 90,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = PdvTheme.TextoClaro,
            Text = "v1.0.0",
        };
        linha2.Controls.Add(_statusLinha2);
        linha2.Controls.Add(versao);

        barra.Controls.Add(linha1);
        barra.Controls.Add(linha2);
        return barra;
    }

    private void AtualizarBarraStatus()
    {
        var terminal = _movimentoInfo?.Caixa ?? "-";
        var turno = _movimentoInfo?.Turno ?? "-";
        _statusLinha1.Text = $"{DateTime.Now:HH:mm:ss}    Terminal: {terminal}    Operador: {_funcionario.Nome}";
        _statusLinha2.Text = $"Turno: {turno}    Movimento nº {_idMovimento}";
    }

    private List<PdvTheme.TeclaFuncao> TeclasFuncao() => new()
    {
        new(1, "Opções", (_, _) => AbrirOpcoes()),
        new(2, "Produtos", (_, _) => AbrirBuscaProduto()),
        new(3, "Fecha\nVenda", (_, _) => FecharVenda()),
        new(4, "Cancela\nCupom", (_, _) => CancelarVenda()),
        new(5, "Cancela\nItem", (_, _) => CancelarItemSelecionado()),
        new(6, "Preços", null, Habilitada: false),
        new(7, "Clientes", (_, _) => IdentificarCliente()),
        new(8, "Menu\nFiscal", (_, _) => AbrirMenuFiscal()),
        new(9, "Pré-Venda", null, Habilitada: false),
        new(10, "Orçamento", null, Habilitada: false),
        new(11, "Vendedor", null, Habilitada: false),
        new(12, "Sair", (_, _) => Sair()),
    };

    // ---- Painel esquerdo: grid de itens + total geral ----

    private Panel CriarPainelEsquerdo()
    {
        ConfigurarGrid();
        _grid.Location = new Point(0, 0);
        _grid.Size = new Size(615, 460);
        _grid.SelectionChanged += Grid_SelectionChanged;
        _grid.CellDoubleClick += Grid_CellDoubleClick;

        var painelTotal = new Panel { Location = new Point(0, 460), Size = new Size(615, 51), BackColor = PdvTheme.CorpoClaro };
        var lblTotalCaption = new Label
        {
            Text = "Total R$:",
            Location = new Point(15, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = PdvTheme.TextoSecundario,
        };
        _totalGeralValor.Text = "0,00";
        _totalGeralValor.Location = new Point(150, 5);
        _totalGeralValor.Size = new Size(450, 40);
        _totalGeralValor.TextAlign = ContentAlignment.MiddleRight;
        _totalGeralValor.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        _totalGeralValor.ForeColor = PdvTheme.TextoPrimario;
        painelTotal.Controls.Add(lblTotalCaption);
        painelTotal.Controls.Add(_totalGeralValor);

        var painel = new Panel { Location = new Point(0, 170), Size = new Size(615, 511), BackColor = Color.White };
        painel.Controls.Add(_grid);
        painel.Controls.Add(painelTotal);
        return painel;
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item", Width = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", Width = 65 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "Descrição", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtde", Width = 50 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unitario", HeaderText = "Unitário", Width = 65 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desconto", HeaderText = "Desconto", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Situacao", HeaderText = "Situação", Width = 75 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total", Width = 75 });
    }

    // ---- Painel direito: marca + campos de entrada ----

    private Panel CriarPainelDireito()
    {
        var painel = new Panel { Location = new Point(615, 170), Size = new Size(750, 511), BackColor = PdvTheme.CorpoClaro };
        painel.Controls.Add(CriarLogoInstitucional());
        painel.Controls.Add(CriarPainelCampos());
        return painel;
    }

    /// <summary>
    /// Marca da empresa no centro do painel direito, igual ao print de
    /// referência — aqui como texto (não tenho o arquivo da logo real);
    /// se você me passar o PNG eu troco por ele.
    /// </summary>
    private Panel CriarLogoInstitucional()
    {
        var painel = new Panel { Location = new Point(40, 20), Size = new Size(670, 140), BackColor = Color.Transparent };
        var nome = new Label
        {
            Text = "ADSoftwares",
            Dock = DockStyle.Top,
            Height = 80,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 32, FontStyle.Bold | FontStyle.Italic),
            ForeColor = PdvTheme.BarraEscura,
        };
        var tagline = new Label
        {
            Text = "DESENVOLVIMENTO DE SISTEMAS  ·  MANUTENÇÃO DE COMPUTADORES",
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = PdvTheme.TextoSecundario,
        };
        // Dock=Top empilha na ordem de adição: "nome" primeiro fica em cima, "tagline" logo abaixo.
        painel.Controls.Add(nome);
        painel.Controls.Add(tagline);
        return painel;
    }

    private Panel CriarPainelCampos()
    {
        var painel = new Panel { Location = new Point(40, 190), Size = new Size(500, 260), BackColor = Color.Transparent };

        var lblCodigo = new Label { Text = "Código/Descrição do Produto:", Location = new Point(0, 0), AutoSize = true };
        _codigo.Location = new Point(0, 22);
        _codigo.Width = 430;
        _codigo.Font = new Font("Segoe UI", 11);
        _codigo.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = AdicionarPorCodigoAsync(); } };

        _buscarProduto.Text = "";
        _buscarProduto.Size = new Size(34, 28);
        _buscarProduto.Location = new Point(436, 22);
        _buscarProduto.Image = ButtonIcons.Search();
        _buscarProduto.Click += (_, _) => AbrirBuscaProduto();
        PdvTheme.EstilizarBotaoSecundario(_buscarProduto);

        var caixaQtd = PdvTheme.CriarCaixaLeitura("Quantidade", out var lblQtdValor);
        caixaQtd.Location = new Point(0, 75);
        _labelQuantidadeValor = lblQtdValor;

        var caixaUnit = PdvTheme.CriarCaixaLeitura("Valor Unitário", out var lblUnitValor);
        caixaUnit.Location = new Point(230, 75);
        _valorUnitarioValor = lblUnitValor;

        var caixaTotal = PdvTheme.CriarCaixaLeitura("Valor Total", out var lblTotalValor);
        caixaTotal.Location = new Point(230, 150);
        _valorTotalValor = lblTotalValor;

        _clienteLabel.Text = "Cliente: (nenhum identificado — F7)";
        _clienteLabel.Location = new Point(0, 230);
        _clienteLabel.AutoSize = true;
        _clienteLabel.Font = new Font("Segoe UI", 9.5f);

        painel.Controls.Add(lblCodigo);
        painel.Controls.Add(_codigo);
        painel.Controls.Add(_buscarProduto);
        painel.Controls.Add(caixaQtd);
        painel.Controls.Add(caixaUnit);
        painel.Controls.Add(caixaTotal);
        painel.Controls.Add(_clienteLabel);

        return painel;
    }

    // ---- Carrinho / grid ----

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
        var existente = _carrinho.FirstOrDefault(i => i.Produto.Id == produto.Id && !i.Cancelado);
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
            var linha = _grid.Rows[_grid.Rows.Add(
                item, codigo, i.Produto.Nome,
                i.Quantidade.ToString("0.###", _ptBr),
                i.ValorUnitario.ToString("N2", _ptBr),
                i.Desconto.ToString("N2", _ptBr),
                i.Cancelado ? "CANCELADO" : "",
                i.Total.ToString("N2", _ptBr))];

            if (i.Cancelado)
            {
                linha.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Strikeout);
                linha.DefaultCellStyle.ForeColor = PdvTheme.TextoSecundario;
                linha.Cells["Situacao"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
                linha.Cells["Situacao"].Style.ForeColor = PdvTheme.Perigo;
            }

            item++;
        }

        AtualizarTotalGeral();
    }

    private void AtualizarTotalGeral()
    {
        var total = _carrinho.Where(i => !i.Cancelado).Sum(i => i.Total);
        _totalGeralValor.Text = total.ToString("N2", _ptBr);
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _carrinho.Count)
            return;

        var item = _carrinho[index];
        _labelQuantidadeValor.Text = item.Quantidade.ToString("0.###", _ptBr);
        _valorUnitarioValor.Text = item.ValorUnitario.ToString("N2", _ptBr);
        _valorTotalValor.Text = item.Total.ToString("N2", _ptBr);
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _carrinho.Count)
            return;

        var item = _carrinho[e.RowIndex];
        if (item.Cancelado)
            return;

        EditarItem(item, e.RowIndex);
    }

    /// <summary>Ajuste de quantidade/desconto de um item já lançado — aberto com duplo clique na linha (item cancelado não pode ser editado).</summary>
    private void EditarItem(ItemCarrinho item, int index)
    {
        using var dlg = new Form
        {
            Text = "Editar item",
            ClientSize = new Size(320, 190),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = PdvTheme.CorpoClaro,
            Font = PdvTheme.FontePadrao,
        };

        var lblQtd = new Label { Text = "Quantidade:", Location = new Point(20, 25), AutoSize = true };
        var qtd = new NumericUpDown
        {
            Location = new Point(150, 22), Width = 130, DecimalPlaces = 3,
            Minimum = 0.001m, Maximum = 99999, Value = Math.Clamp(item.Quantidade, 0.001m, 99999),
        };

        var lblDesc = new Label { Text = "Desconto R$:", Location = new Point(20, 65), AutoSize = true };
        var desc = new NumericUpDown
        {
            Location = new Point(150, 62), Width = 130, DecimalPlaces = 2,
            Maximum = 999999, Value = Math.Clamp(item.Desconto, 0, 999999),
        };

        var ok = new Button { Text = "OK", Location = new Point(55, 130), Size = new Size(90, 36), DialogResult = DialogResult.OK };
        var cancelar = new Button { Text = "Cancelar", Location = new Point(165, 130), Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
        PdvTheme.EstilizarBotaoPrimario(ok);
        PdvTheme.EstilizarBotaoSecundario(cancelar);

        dlg.Controls.Add(lblQtd);
        dlg.Controls.Add(qtd);
        dlg.Controls.Add(lblDesc);
        dlg.Controls.Add(desc);
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancelar);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancelar;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        item.Quantidade = qtd.Value;
        item.Desconto = desc.Value;
        AtualizarGrid();
        if (index < _grid.Rows.Count)
            _grid.Rows[index].Selected = true;
    }

    /// <summary>
    /// F5 - Cancela Item: o item cancelado continua na tela (com a tarja
    /// "CANCELADO" e tachado), só sai do total da venda — igual ao
    /// comportamento pedido pelo usuário (não é o mesmo que excluir).
    /// </summary>
    private void CancelarItemSelecionado()
    {
        var index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _carrinho.Count)
            return;

        _carrinho[index].Cancelado = true;
        AtualizarGrid();
    }

    private void AbrirOpcoes()
    {
        using var form = new OpcoesMenuForm(_config);
        form.ShowDialog(this);
    }

    private void AbrirMenuFiscal()
    {
        using var form = new MenuFiscalForm(_db, _idMovimento);
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
        var itensValidos = _carrinho.Where(i => !i.Cancelado).ToList();
        if (itensValidos.Count == 0)
        {
            MessageBox.Show("Adicione ao menos um item (não cancelado) antes de fechar a venda.", "GestorPDV",
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

        var subtotal = itensValidos.Sum(i => i.Total);
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
            "Cancelar o cupom em andamento? Os itens lançados serão descartados.",
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
        if (_carrinho.Any(i => !i.Cancelado))
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
