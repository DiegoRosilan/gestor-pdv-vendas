using System.Windows;
using GestorPDV.App.ViewModels;
using GestorPDV.App.Views.Login;
using GestorPDV.App.Views.Principal;
using GestorPDV.App.Views.Vendas;
using GestorPDV.Application.Interfaces;
using GestorPDV.Application.Services;
using GestorPDV.Application.UseCases.Vendas;
using GestorPDV.Infrastructure.Database;
using GestorPDV.Infrastructure.Printing;
using GestorPDV.Infrastructure.Repositories;
using GestorPDV.Infrastructure.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorPDV.App;

/// <summary>
/// Ponto de entrada de verdade (ver StartupObject no .csproj — App.xaml
/// não gera Main sozinho aqui). Mesmo fluxo do legacy/GestorPDV.Vendas:
/// carrega configuração, resolve o login e só então mostra a janela
/// principal — mas via DI (Microsoft.Extensions.DependencyInjection) em
/// vez de instanciar tudo à mão.
/// </summary>
internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        var app = new App();
        app.InitializeComponent();

        IConfiguration configuration;
        ServiceProvider provider;
        try
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();
            ConfigurarServicos(services, configuration);
            provider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível carregar appsettings.json:{Environment.NewLine}{ex.Message}",
                "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var loginViewModel = provider.GetRequiredService<LoginViewModel>();
        var loginView = new LoginView(loginViewModel);
        if (loginView.ShowDialog() != true || loginViewModel.FuncionarioLogado is not { } funcionario)
            return;

        var caixaService = provider.GetRequiredService<CaixaService>();
        var funcionarios = provider.GetRequiredService<IFuncionarioRepository>();

        int idMovimento;
        try
        {
            var movimentoAberto = await caixaService.GetMovimentoAbertoAsync(CancellationToken.None);
            if (movimentoAberto is null)
            {
                // AberturaTurnoForm do legado ainda não tem View correspondente
                // nesta estrutura (ver Views/Caixa/.gitkeep) — sem turno aberto
                // não tem como abrir a tela de venda ainda.
                MessageBox.Show(
                    "Não há nenhum turno de caixa aberto, e a tela de abertura de turno "
                    + "ainda não foi portada pra esta versão WPF. Abra um turno pelo "
                    + "sistema legado (legacy/GestorPDV.Vendas) antes de usar esta versão.",
                    "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            idMovimento = movimentoAberto.Value;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao consultar o movimento de caixa:{Environment.NewLine}{ex.Message}",
                "GestorPDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var idOperador = await funcionarios.GetOrCreateOperadorAsync(funcionario.Id, CancellationToken.None);

        var vendaService = provider.GetRequiredService<VendaService>();
        var produtoService = provider.GetRequiredService<ProdutoService>();
        var vendaViewModel = new VendaViewModel(vendaService, idMovimento, funcionario.Id, idOperador);
        var vendaView = new VendaView(vendaViewModel, vendaService, produtoService);
        var mainViewModel = new MainViewModel(vendaViewModel);
        var mainView = new MainView(mainViewModel, vendaView);

        app.Run(mainView);
    }

    private static void ConfigurarServicos(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionString"]
            ?? throw new InvalidOperationException("ConnectionString não configurada em appsettings.json.");

        services.AddSingleton(configuration);
        services.AddSingleton(new PostgreSqlConnectionFactory(connectionString));
        services.AddSingleton<TransactionManager>();

        services.AddSingleton<IProdutoRepository, ProdutoRepository>();
        services.AddSingleton<IClienteRepository, ClienteRepository>();
        services.AddSingleton<IFuncionarioRepository, FuncionarioRepository>();
        services.AddSingleton<IFormaPagamentoRepository, FormaPagamentoRepository>();
        services.AddSingleton<ICaixaRepository, CaixaRepository>();
        services.AddSingleton<IVendaRepository, VendaRepository>();
        services.AddSingleton<IImpressaoService, PrintQueueService>();
        services.AddSingleton<IRelatorioService, FastReportService>();

        services.AddSingleton<AbrirVenda>();
        services.AddSingleton<AdicionarItem>();
        services.AddSingleton<FinalizarVenda>();
        services.AddSingleton<CancelarVenda>();

        services.AddSingleton<VendaService>();
        services.AddSingleton<ProdutoService>();
        services.AddSingleton<ClienteService>();
        services.AddSingleton<EstoqueService>();
        services.AddSingleton<CaixaService>();
        services.AddSingleton<ImpressaoService>();

        services.AddTransient<LoginViewModel>();
    }
}
