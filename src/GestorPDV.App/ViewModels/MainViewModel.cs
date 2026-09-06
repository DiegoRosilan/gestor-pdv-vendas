namespace GestorPDV.App.ViewModels;

/// <summary>
/// ViewModel da janela principal (MainView/Shell). Hoje só hospeda a
/// VendaViewModel — quando Clientes/Orçamentos/Pré-Vendas/Caixa/
/// Configurações ganharem telas de verdade, a navegação entre elas entra
/// aqui (troca de ConteudoAtual, por exemplo).
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public VendaViewModel Venda { get; }

    public MainViewModel(VendaViewModel venda) => Venda = venda;
}
