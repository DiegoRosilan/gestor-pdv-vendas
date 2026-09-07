# GestorPDV

PDV (ponto de venda) desktop para o sistema GestorPDV — login/turno de
caixa, tela de venda, fechamento de venda, encerramento de caixa e menu
fiscal, conectando no mesmo banco PostgreSQL do sistema Delphi original.

## Estrutura

```
GestorPDV.sln
├── src/
│   ├── GestorPDV.App/             WPF (MVVM) — a aplicação desktop
│   ├── GestorPDV.Domain/          entidades, regras de negócio, sem dependências
│   ├── GestorPDV.Application/     casos de uso, interfaces (contratos), DTOs
│   ├── GestorPDV.Infrastructure/  Npgsql, impressão, relatórios (FastReport)
│   └── GestorPDV.Monitor/         serviço à parte, fila de impressão
├── tests/                         um projeto xUnit por camada
├── database/                      documentação do schema (não cria/altera nada)
├── reports/                       especificação dos relatórios (DANFe/DAV/Venda/Caixa)
├── docs/                          arquitetura, regras de negócio, telas, manual
├── scripts/                       instalação, backup, deploy (nenhum ainda)
└── legacy/GestorPDV.Vendas/       versão WinForms anterior — mantida como referência
```

Ver `docs/arquitetura/README.md` para a regra de dependência entre camadas.

## Como rodar

Precisa do **SDK do .NET 8** e **Windows** (WPF e o legado WinForms só
rodam lá — nada disso foi compilado neste ambiente, que é Linux e sem
SDK; ver "Estado atual" abaixo).

1. Edite `src/GestorPDV.App/appsettings.json` com os dados de conexão do
   banco de **TESTE** (nunca aponte pra produção sem testar antes).
2. `dotnet restore GestorPDV.sln`
3. `dotnet build GestorPDV.sln`
4. `dotnet run --project src/GestorPDV.App`

O `GestorPDV.Monitor` roda separado (`dotnet run --project src/GestorPDV.Monitor`)
e usa seu próprio `appsettings.json`.

## Estado atual (esqueleto)

Esta é a estrutura completa (solução, 8 projetos, todas as pastas do
diagrama), com o suficiente implementado pra fazer sentido compilar e
rodar o fluxo de login → venda → fechamento contra um banco real — mas
ainda não é uma reescrita completa do legado. Faltando, entre outras
coisas:

- Identificar cliente (F7) e as configurações fiscais do F1 > Configurações
  — sem View WPF ainda (`Views/Clientes`, `Views/Configuracoes` vazias);
  o equivalente funcional existe no legado. O flyout F1 Opções
  (`VendaView.xaml`) já existe com a mesma listagem/layout do sistema de
  referência, mas cada item só mostra um aviso de "ainda não portado" ao
  clicar — nenhum caso de uso por trás deles foi implementado nesta
  arquitetura ainda. O Menu Fiscal (F8) já lista as notas emitidas do
  movimento atual (`Views/Fiscal/MenuFiscalView.xaml`) — mesmo escopo do
  legado, só mostra o que já está gravado, não emite nada de verdade.
- Impressão e relatórios (FastReport) — interfaces prontas
  (`IImpressaoService`, `IRelatorioService`), implementações são
  stubs que lançam `NotImplementedException` de propósito, com o motivo
  documentado em cada classe.
- Emissão fiscal real (NFC-e/SAT), TEF, PIX, módulo de combustível — fora
  de escopo, mesma decisão do legado (ver `legacy/GestorPDV.Vendas/LEIA-ME.txt`).
- Parcelamento de crediário em `VendaRepository.FinalizarAsync` — o
  legado já faz isso (`contas_pagar_receber`/`contas_parcelas`), ainda
  não foi portado pra cá.

Nada disso foi compilado nem rodado de verdade — este ambiente não tem
Windows nem o SDK do .NET disponível (instalação bloqueada pela rede da
sessão). Toda a estrutura (`.sln`, `.csproj`, bindings XAML, DI em
`App.xaml.cs`) foi revisada manualmente, mas só um `dotnet build` real no
Windows confirma que compila — e essa revisão manual já errou antes (ver
histórico de commits): trate qualquer erro de build como esperado até
confirmarmos juntos que compilou limpo.

## legacy/GestorPDV.Vendas

O PDV WinForms anterior a esta reestruturação continua no repositório —
não some nem para de existir, só para de receber desenvolvimento novo.
Serve de referência: as consultas SQL ali (`Database.cs`) já foram
validadas contra um backup real numa rodada anterior (ver o LEIA-ME
dentro da pasta), e boa parte foi portada pra
`src/GestorPDV.Infrastructure/Repositories` nesta reestruturação.
