# database/

Este diretório **documenta** o schema PostgreSQL que o GestorPDV já usa em
produção — ele não é criado por este repositório nem por nenhum script
aqui dentro. O banco é o mesmo sistema Delphi original (ECF/PDV), com
tabelas como `ecf_funcionario`, `ecf_movimento`, `ecf_venda_cabecalho`
etc.; `src/GestorPDV.Infrastructure/Repositories` conecta nele via
`PostgreSqlConnectionFactory` (connection string em `appsettings.json`).

**Não gere um script "CREATE TABLE" a partir do que está aqui e rode
contra um banco real** — o que existe em `schema/` é uma referência das
colunas já validadas nas consultas do PDV (ver
`legacy/GestorPDV.Vendas/Database.cs` e seu LEIA-ME), não um DDL completo
(faltam tipos exatos, constraints, índices, triggers reais). Pra ter o
schema completo e atual, use `pg_dump --schema-only` direto no banco do
cliente.

- `schema/` — referência das tabelas/colunas conhecidas (ver
  `tabelas-conhecidas.md`).
- `tables/`, `views/`, `functions/`, `triggers/`, `indexes/` — reservados
  pra quando tivermos o DDL real (via pg_dump) pra versionar aqui, um
  arquivo por objeto.
- `seeds/` — dados de exemplo pra um banco de teste (nenhum ainda).
