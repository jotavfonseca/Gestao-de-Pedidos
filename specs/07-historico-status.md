# 07 — Histórico de status do pedido

- **Status:** Concluída
- **Pontuação:** bônus **Histórico de status do pedido (+3)**

## Contexto

A entidade `OrderStatusHistory` e a tabela já existem na migration, mas o
`OrderContext` não expõe o `DbSet`, a API não retorna o histórico e a tela de
detalhes não existe.

## Requisitos

- Histórico de status do pedido — desafio §5.
- Visualizar detalhes do pedido — desafio §2.

## Critérios de aceite

1. **Dado** um pedido criado, **então** o histórico tem `Pendente` com a data de criação.
2. **Dado** um pedido finalizado, **então** o histórico tem exatamente `Pendente`, `Processando`, `Finalizado`, em ordem cronológica.
3. **Quando** `GET /orders/{id}`, **então** a resposta inclui `historico: [{ status, data }]` ordenado por data.
4. **Dado** a tela de detalhes, **então** aparece uma linha do tempo com cada status, data/hora local e o tempo decorrido entre etapas.
5. **Dado** a tela de detalhes aberta, **quando** o status muda, **então** a nova entrada aparece na linha do tempo sem recarregar (spec 06).

## Design

- Gravação centralizada em `Order.TransitionTo` (spec 01): impossível mudar status sem gerar histórico.
- Retorno no `OrderResponse` (spec 02).
- Componente `StatusTimeline` no frontend (spec 09).
- O histórico também alimenta a métrica "tempo médio de processamento" do módulo IA (spec 10).

## Tasks

- [x] 7.1 Histórico gravado em toda transição (depende de 1.3).
- [x] 7.2 `historico` no `OrderResponse` e no `GET /orders/{id}` (depende de 2.1).
- [x] 7.3 Componente `StatusTimeline` na tela de detalhes (depende de 9.4).

## Verificação

- Critérios 1–2: testes de domínio e do handler do worker (spec 12).
- Critério 3: teste de integração da API (spec 12).
- Critérios 4–5: manual no frontend com o Compose no ar.

## Notas de implementação

- Verificado: critérios 1–3 por testes automatizados e no Compose (histórico com Pendente, Processando, Finalizado).
- **Pendente de verificação manual no navegador:** critérios 4–5 (linha do tempo e atualização sem recarregar).
