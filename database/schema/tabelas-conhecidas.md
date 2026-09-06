# Tabelas conhecidas (referência, não DDL)

Colunas confirmadas por uso real em consultas já validadas (ver
`legacy/GestorPDV.Vendas/Database.cs` e `src/GestorPDV.Infrastructure/Repositories`).
Faltam tipos exatos, PKs/FKs e constraints — para isso, `pg_dump --schema-only`
no banco real.

## ecf_funcionario
`id`, `nome`, `login`, `senha` (texto puro), `sit` ('A'=ativo), `vendedor`,
`caixa`, `gerente` ('S'/'N').

## ecf_operador
`id`, `id_ecf_funcionario`, `sit`. Cada funcionário que opera o PDV precisa
de uma linha aqui (criada sob demanda no primeiro uso).

## ecf_turno
`id`, `descricao`, `hora_inicio`, `hora_fim`.

## ecf_caixa
`id`, `nome` (terminal/caixa físico).

## ecf_empresa
`id` (usada só pra pegar a empresa "atual": `ORDER BY id LIMIT 1`).

## ecf_impressora
`id`, `marca`, `modelo` (0 = "NENHUM", sem impressora fiscal real associada).

## ecf_movimento
`id`, `id_ecf_empresa`, `id_ecf_turno`, `id_ecf_impressora`, `id_ecf_operador`,
`id_ecf_caixa`, `id_gerente_supervisor`, `status_movimento` ('A'=aberto/'F'=fechado),
`data_abertura`, `hora_abertura`, `data_fechamento`, `hora_fechamento`,
`total_suprimento`, `total_venda`, `total_desconto`, `total_recebido`, `total_crediario`.

## ecf_fechamento
`id_ecf_movimento`, `tipo_pagamento`, `valor` (um "encerrante" declarado no
fechamento de caixa).

## ecf_tipo_pagamento
`id`, `descricao`, `gera_parcelas` ('S'/'N'), `sit` (≠'I' = ativa).

## ecf_venda_cabecalho
`id`, `id_cliente`, `id_ecf_funcionario`, `id_ecf_operador`, `id_ecf_movimento`,
`cfop` (fixo 5102 nesta versão), `coo` (sequencial próprio, não fiscal),
`data_venda`, `hora_venda`, `valor_venda`, `desconto`, `acrescimo`,
`valor_final`, `valor_recebido`, `troco`, `status_venda` ('F'=finalizada/'C'=cancelada),
`nome_cliente`, `cpf_cnpj_cliente`, `operador_inc`, `modelo_cupom`, `observacao`.

## ecf_venda_detalhe
`id_ecf_produto`, `id_ecf_venda_cabecalho`, `cfop`, `item` (nº sequencial),
`quantidade`, `valor_unitario`, `valor_total`, `total_item`, `desconto`,
`cancelado` ('S'/'N'), `movimenta_estoque` ('S'/'N'), `hora`.

## ecf_total_tipo_pgto
`id_ecf_venda_cabecalho`, `id_ecf_tipo_pagamento`, `valor`, `data_venda`.

## produto
`id`, `cod_produto`, `codigo_interno`, `gtin`, `nome`, `valor_venda`,
`qtd_estoque`, `id_unidade_produto`, `localizacao`, `produto_pdv` ('S'/'N'),
`inativo` ('S'/'N').

## unidade_produto
`id`, `sigla`.

## cliente
`id`, `cod_cliente`, `nome`, `cpf_cnpj`, `sit` ('A'=ativo).

## contas_pagar_receber / contas_parcelas
Usadas pra crediário (pagamento com `gera_parcelas='S'`) — ver
`legacy/GestorPDV.Vendas/Database.cs.FinalizarVendaAsync` pros campos
exatos; ainda não portadas pra `VendaRepository` nesta estrutura nova.

## Fora do escopo desta documentação
O `.exe` original do sistema (analisado antes deste projeto começar)
revelou um sistema bem mais amplo — módulo de posto de combustível
(`ecf_posto_bico`, `ecf_posto_tanque`, `ecf_posto_abastecimento`), SAT,
NFC-e, TEF, PIX (`ecf_configuracao_pix`), NFS-e. Nenhuma dessas tabelas
foi validada contra um banco real ainda — só nomes vistos nas strings do
executável.
