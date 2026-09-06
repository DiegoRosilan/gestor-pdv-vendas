# Arquitetura

Clean Architecture em 4 camadas (`src/`):

```
GestorPDV.Domain          -> sem dependências (entidades, regras, value objects)
GestorPDV.Application     -> depende só de Domain (casos de uso, interfaces, DTOs)
GestorPDV.Infrastructure  -> depende de Application+Domain (Npgsql, impressão, relatórios)
GestorPDV.App (WPF)       -> depende de tudo (composição/DI em App.xaml.cs, OnStartup)
GestorPDV.Monitor         -> depende de tudo (serviço à parte, sem UI)
```

Regra de dependência: as camadas de fora podem depender das de dentro,
nunca o contrário. `Application` define interfaces em `Interfaces/` que
só `Infrastructure` implementa — é assim que dá pra trocar o banco (ou
testar com mocks) sem tocar em regra de negócio.

`legacy/GestorPDV.Vendas/` é o WinForms anterior a esta reestruturação —
continua no repositório como referência (consultas já validadas contra o
banco real) mas não recebe mais desenvolvimento novo.

Ver também `docs/banco-dados/` e `docs/regras-negocio/`.
