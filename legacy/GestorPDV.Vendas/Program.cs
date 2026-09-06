namespace GestorPDV.Vendas;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppConfig config;
        try
        {
            config = AppConfig.Load("appsettings.json");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível carregar appsettings.json:{Environment.NewLine}{ex.Message}",
                "GestorPDV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var db = new Database(config.ConnectionString);

        try
        {
            // Igual ao sistema original: se já existe um movimento (turno de
            // caixa) aberto, pede só login/senha para entrar nele; se não
            // existe nenhum, pede pra abrir um novo turno.
            //
            // Task.Run(...).GetAwaiter().GetResult() aqui (em vez de só
            // ".GetAwaiter().GetResult()") é de propósito: mesmo não tendo
            // nenhuma janela aberta ainda neste ponto, rodar a consulta
            // dentro de Task.Run garante que ela execute numa thread sem
            // SynchronizationContext nenhum, então não corre risco de travar
            // independente de qualquer coisa que rode antes desta linha no
            // futuro. Os diálogos abaixo (que já têm janela e loop de
            // mensagens ativo) usam "await" de verdade, nunca bloqueiam.
            var idMovimentoAberto = Task.Run(() => db.GetMovimentoAbertoAsync(CancellationToken.None)).GetAwaiter().GetResult();

            Funcionario funcionario;
            int idOperador;
            int idMovimento;

            if (idMovimentoAberto is not null)
            {
                var info = Task.Run(() => db.GetMovimentoInfoAsync(idMovimentoAberto.Value, CancellationToken.None)).GetAwaiter().GetResult()
                    ?? throw new InvalidOperationException("Movimento aberto não encontrado.");

                using var loginForm = new ConfirmaLoginForm(db, info);
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.FuncionarioLogado is null)
                    return;

                funcionario = loginForm.FuncionarioLogado;
                idOperador = loginForm.IdOperador;
                idMovimento = idMovimentoAberto.Value;
            }
            else
            {
                using var aberturaForm = new AberturaTurnoForm(db);
                if (aberturaForm.ShowDialog() != DialogResult.OK || aberturaForm.IdMovimentoAberto is null)
                    return;

                idMovimento = aberturaForm.IdMovimentoAberto.Value;
                idOperador = aberturaForm.IdOperador;
                funcionario = aberturaForm.OperadorLogado
                    ?? throw new InvalidOperationException("Operador não identificado após abertura do turno.");
            }

            Application.Run(new VendaForm(db, config, funcionario, idOperador, idMovimento));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao iniciar o PDV:{Environment.NewLine}{ex.Message}",
                "GestorPDV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
