using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using GestorPDV.Application.DTOs;
using GestorPDV.Application.Services;
using GestorPDV.Domain.Entities;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// Tela de venda — porta o comportamento de VendaForm.cs (legacy) pra
/// MVVM: item cancelado (F5) fica na lista (tachado/tarja "CANCELADO" —
/// ver ItemVendaDto.Cancelado, o binding de estilo fica na VendaView) e
/// some do total; F4 descarta o cupom inteiro; F3 abre o fechamento
/// (FechamentoVendaViewModel), que ainda precisa ser aberto pela View já
/// que ela quem sabe mostrar o diálogo modal.
///
/// Também expõe o "destaque" do item atual (descrição grande no
/// cabeçalho + caixas de Quantidade/Valor Unitário/Valor Total/Estoque) e
/// a barra de status (relógio, terminal, operador, turno) — mesma
/// informação que VendaForm.AtualizarBarraStatus mostrava, igual ao
/// print de referência do sistema original.
/// </summary>
public sealed class VendaViewModel : ViewModelBase, IDisposable
{
    private readonly VendaService _vendaService;
    private readonly CaixaService _caixaService;
    private readonly string _nomeOperador;
    private readonly DispatcherTimer _relogio;
    private readonly string? _logoPath;

    private Venda _venda;
    private string _codigo = "";
    private string _mensagem = "";
    private ItemVendaDto? _itemSelecionado;
    private string _descricaoAtual = "";
    private decimal _quantidadeAtual;
    private decimal _valorUnitarioAtual;
    private decimal _valorTotalAtual;
    private decimal _estoqueAtual;
    private string _horaAtual = DateTime.Now.ToString("HH:mm:ss");
    private string _terminal = "-";
    private string _turno = "-";

    public VendaViewModel(VendaService vendaService, CaixaService caixaService, int idMovimento, int idFuncionario, int idOperador, string nomeOperador, string? logoPath = null)
    {
        _vendaService = vendaService;
        _caixaService = caixaService;
        _nomeOperador = nomeOperador;
        _logoPath = logoPath;
        _venda = _vendaService.NovaVenda(idMovimento, idFuncionario, idOperador);

        AdicionarPorCodigoCommand = RelayCommand.CreateAsync(_ => AdicionarPorCodigoAsync(), _ => !string.IsNullOrWhiteSpace(Codigo));
        CancelarItemCommand = RelayCommand.Create(_ => CancelarItemSelecionado(), _ => ItemSelecionado is { Cancelado: false });
        CancelarCupomCommand = RelayCommand.Create(_ => CancelarCupom());

        _relogio = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _relogio.Tick += (_, _) => AtualizarHora();
        _relogio.Start();

        _ = CarregarStatusAsync();
    }

    public ObservableCollection<ItemVendaDto> Itens { get; } = new();

    public string Codigo { get => _codigo; set => SetProperty(ref _codigo, value); }
    public string Mensagem { get => _mensagem; set => SetProperty(ref _mensagem, value); }
    public string TotalGeral => _venda.Total.ToString();

    public ItemVendaDto? ItemSelecionado
    {
        get => _itemSelecionado;
        set
        {
            if (SetProperty(ref _itemSelecionado, value) && value is not null)
                AtualizarDestaque(value);
        }
    }

    /// <summary>Nome do produto em destaque no cabeçalho — o item recém-adicionado, ou o que o operador clicar na lista.</summary>
    public string DescricaoAtual { get => _descricaoAtual; private set => SetProperty(ref _descricaoAtual, value); }
    public decimal QuantidadeAtual { get => _quantidadeAtual; private set => SetProperty(ref _quantidadeAtual, value); }
    public decimal ValorUnitarioAtual { get => _valorUnitarioAtual; private set => SetProperty(ref _valorUnitarioAtual, value); }
    public decimal ValorTotalAtual { get => _valorTotalAtual; private set => SetProperty(ref _valorTotalAtual, value); }
    public decimal EstoqueAtual { get => _estoqueAtual; private set => SetProperty(ref _estoqueAtual, value); }

    /// <summary>Linha 1 da barra de status: relógio + terminal + operador, igual a VendaForm.AtualizarBarraStatus.</summary>
    public string StatusLinha1 => $"{_horaAtual}    Terminal: {_terminal}    Operador: {_nomeOperador}";

    /// <summary>Linha 2: turno + número do movimento.</summary>
    public string StatusLinha2 => $"Turno: {_turno}    Movimento nº {IdMovimento}";

    /// <summary>
    /// Logo institucional, configurável via appsettings.json ("LogoPath")
    /// — futuramente trocável pela tela de Configurações (F1), ainda não
    /// portada. Sem logo configurada, a VendaView mostra um texto de
    /// marca-d'água no lugar (ver TemLogo).
    /// </summary>
    public string? LogoPath => _logoPath;
    public bool TemLogo => !string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath);

    public ICommand AdicionarPorCodigoCommand { get; }
    public ICommand CancelarItemCommand { get; }
    public ICommand CancelarCupomCommand { get; }

    /// <summary>Busca por termo achou mais de um produto — a View decide o que fazer (normalmente abre a busca já filtrada por esse termo).</summary>
    public event EventHandler<string>? VariosProdutosEncontrados;

    /// <summary>Exposto pra FechamentoVendaViewModel/VendaView montarem o fechamento (F3) sem essa ViewModel precisar conhecer a tela de fechamento.</summary>
    public Venda VendaAtual => _venda;

    /// <summary>Mesmo movimento em toda a vida da ViewModel (CancelarCupom preserva IdMovimento) — exposto pra VendaView montar o Menu Fiscal (F8).</summary>
    public int IdMovimento => _venda.IdMovimento;

    private async Task AdicionarPorCodigoAsync()
    {
        Mensagem = "";
        var termo = Codigo.Trim();
        if (termo.Length == 0)
            return;

        // Aceita código, código de barras (gtin) e código interno (busca
        // exata) ou nome do produto (ILIKE) — mesmo campo "Código/Descrição
        // do Produto" serve pros dois tipos de busca, ver AdicionarItem.
        var resultado = await _vendaService.AdicionarItemPorTermoAsync(_venda, termo, CancellationToken.None);

        if (resultado.ProdutoAdicionado is not null)
        {
            Codigo = "";
            AtualizarLista();

            // Destaque segue o item recém-lançado (não o selecionado na
            // lista, que continua sendo o que estava antes) — mesmo
            // comportamento de VendaForm.AdicionarProdutoAoCarrinho no legado.
            if (Itens.Count > 0)
                AtualizarDestaque(Itens[^1]);
            return;
        }

        if (resultado.Candidatos.Count > 0)
        {
            VariosProdutosEncontrados?.Invoke(this, termo);
            return;
        }

        Mensagem = $"Nenhum produto encontrado com \"{termo}\".";
    }

    private void CancelarItemSelecionado()
    {
        if (ItemSelecionado is null)
            return;

        var indice = ItemSelecionado.NumeroItem - 1;
        _venda.CancelarItem(indice);
        AtualizarLista();
    }

    private void CancelarCupom() => ReiniciarVenda();

    /// <summary>Chamado pela View depois que FechamentoVendaView confirma a venda — mesmo estado inicial de uma venda nova, pro próximo cliente.</summary>
    public void LimparAposFinalizar() => ReiniciarVenda();

    private void ReiniciarVenda()
    {
        _venda = new Venda { IdMovimento = _venda.IdMovimento, IdFuncionario = _venda.IdFuncionario, IdOperador = _venda.IdOperador };
        Codigo = "";
        Mensagem = "";
        AtualizarLista();
        AtualizarDestaque(null);
    }

    private void AtualizarDestaque(ItemVendaDto? item)
    {
        DescricaoAtual = item?.Descricao ?? "";
        QuantidadeAtual = item?.Quantidade ?? 0;
        ValorUnitarioAtual = item?.ValorUnitario ?? 0;
        ValorTotalAtual = item?.Total ?? 0;
        EstoqueAtual = item?.Estoque ?? 0;
    }

    private void AtualizarLista()
    {
        Itens.Clear();
        var numero = 1;
        foreach (var item in _venda.Itens)
        {
            var codigo = item.Produto.CodProduto ?? item.Produto.CodigoInterno ?? item.Produto.Id.ToString();
            Itens.Add(new ItemVendaDto(
                numero, codigo, item.Produto.Nome, item.Quantidade, item.Produto.UnidadeSigla,
                item.ValorUnitario.Valor, item.Desconto.Valor, item.Total.Valor, item.Produto.QtdEstoque, item.Cancelado));
            numero++;
        }

        OnPropertyChanged(nameof(TotalGeral));
    }

    private async Task CarregarStatusAsync()
    {
        try
        {
            var info = await _caixaService.GetMovimentoInfoAsync(IdMovimento, CancellationToken.None);
            if (info is null) return;

            _terminal = info.Terminal;
            _turno = info.Turno;
            OnPropertyChanged(nameof(StatusLinha1));
            OnPropertyChanged(nameof(StatusLinha2));
        }
        catch
        {
            // Barra de status só perde terminal/turno, não impede vender — mesmo comportamento do legado.
        }
    }

    private void AtualizarHora()
    {
        _horaAtual = DateTime.Now.ToString("HH:mm:ss");
        OnPropertyChanged(nameof(StatusLinha1));
    }

    public void Dispose() => _relogio.Stop();
}
