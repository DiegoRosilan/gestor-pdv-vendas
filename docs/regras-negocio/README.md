# Regras de negócio

As regras vivem em `src/GestorPDV.Domain/Rules/` (código, fonte da
verdade) — este diretório é pra documentar o *porquê* em prosa, quando o
código sozinho não conta a história toda. Hoje:

- **VendaRules**: uma venda só fecha com ao menos um item não cancelado.
- **ProdutoRules**: produto inativo não pode ser vendido.
- **EstoqueRules**: o PDV nunca bloqueou venda por falta de estoque — só
  informa (estoque pode ficar negativo, igual ao sistema legado).
- **PagamentoRules**: a soma dos pagamentos lançados precisa cobrir o
  total a receber antes de finalizar.
- **Item cancelado** (F5): fica na lista da venda com o valor original
  ainda visível (tachado na tela), mas sai do total geral e não baixa
  estoque — grava `cancelado='S'` em `ecf_venda_detalhe` pra manter o
  histórico do cupom.

Limitações conhecidas (herdadas do sistema legado, ver
`legacy/GestorPDV.Vendas/LEIA-ME.txt` para a lista completa): sem cálculo
fiscal real, sem estorno de venda já finalizada, sem comparação entre
valor declarado e total do sistema no fechamento de caixa.
