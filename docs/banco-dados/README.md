# Banco de dados

A referência técnica (tabelas/colunas conhecidas) mora em
`database/schema/tabelas-conhecidas.md` — este diretório é pra
documentação narrativa (diagramas, decisões) quando isso existir.

Ponto central: o banco **já existe** (é o mesmo do sistema Delphi
original) — nenhuma migration deste repositório cria ou altera schema.
`src/GestorPDV.Infrastructure/Migrations/` fica reservado pra scripts
versionados quando/se isso for necessário.
