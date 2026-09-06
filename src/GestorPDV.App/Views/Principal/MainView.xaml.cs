using System.Windows;
using GestorPDV.App.ViewModels;
using GestorPDV.App.Views.Vendas;

namespace GestorPDV.App.Views.Principal;

public partial class MainView : Window
{
    public MainView(MainViewModel viewModel, VendaView vendaView)
    {
        InitializeComponent();
        DataContext = viewModel;
        ConteudoPrincipal.Content = vendaView;
    }
}
