using System.Collections.ObjectModel;
using System.Globalization;
using GestorPDV.Application.Services;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// "F8 - Menu Fiscal": lista as notas (vendas finalizadas) do movimento
/// atual — mesmo papel de MenuFiscalForm no legado. Só mostra o que já
/// está gravado em ecf_venda_cabecalho (via VendaService.GetNotasEmitidasAsync,
/// que já existia antes desta View); não fala com SEFAZ/SAT de verdade —
/// isso continua fora de escopo (ver legacy/GestorPDV.Vendas/LEIA-ME.txt).
/// A configuração do modo de emissão fiscal não mora aqui, foi pro
/// submenu Configurações do F1 (ver Views/Configuracoes, ainda não portado).
/// </summary>
public sealed class MenuFiscalViewModel : ViewModelBase
{
    private readonly VendaService _vendaService;
    private readonly int _idMovimento;
    private readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    private string? _mensagem;

    public MenuFiscalViewModel(VendaService vendaService, int idMovimento)
    {
        _vendaService = vendaService;
        _idMovimento = idMovimento;
    }

    public ObservableCollection<NotaFiscalLinha> Notas { get; } = new();

    public string? Mensagem { get => _mensagem; set => SetProperty(ref _mensagem, value); }

    public async Task CarregarAsync()
    {
        Mensagem = null;
        try
        {
            var notas = await _vendaService.GetNotasEmitidasAsync(_idMovimento, CancellationToken.None);
            Notas.Clear();

            if (notas.Count == 0)
            {
                Mensagem = "Nenhuma nota emitida neste movimento ainda.";
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
                Notas.Add(new NotaFiscalLinha(
                    nota.Coo,
                    $"{nota.DataVenda:dd/MM/yyyy} {nota.HoraVenda}",
                    $"R$ {nota.ValorFinal.ToString("N2", _ptBr)}",
                    situacao));
            }
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao consultar notas emitidas: {ex.Message}";
        }
    }
}

/// <summary>Linha já formatada pro grid (valor/situação como texto) — evita converter no XAML.</summary>
public sealed record NotaFiscalLinha(int Coo, string DataHora, string Valor, string Situacao);
