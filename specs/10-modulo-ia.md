# 10 — Módulo IA: "Pergunte sobre os pedidos"

- **Status:** Em verificação
- **Pontuação:** opcional / bônus **Módulo IA/Analytics com LLM (+5)**

## Contexto

Módulo novo. O usuário faz perguntas em linguagem natural e recebe respostas
com dados reais do banco.

## Requisitos

- LLM interpreta a pergunta, consulta o banco e responde de forma amigável com dados reais — desafio §4.
- Exemplos que devem funcionar: "Quantos pedidos temos hoje?", "Qual o tempo médio para aprovar os pedidos?", "Quantos pedidos estão pendentes?", "Qual o valor total de pedidos finalizados este mês?".

## Critérios de aceite

1. **Dado** a `ANTHROPIC_API_KEY` configurada, **quando** `POST /orders/ask` com `{ "question": "Quantos pedidos estão pendentes?" }`, **então** `200` com `{ answer, toolCalls[] }`, e o número na resposta bate com o banco.
2. As quatro perguntas de exemplo do desafio são respondidas corretamente (datas relativas como "hoje" e "este mês" interpretadas no fuso `America/Sao_Paulo`).
3. "Tempo médio para aprovar" é calculado pelo histórico (`Pendente` → `Finalizado`) e a resposta deixa claro o que foi medido.
4. **Segurança**: a LLM **não** executa SQL livre; só chama ferramentas fixas, somente leitura, com parâmetros validados. Pergunta pedindo para alterar ou apagar dados recebe uma recusa educada.
5. **Dado** uma pergunta fora do domínio (ex.: "qual a capital da França?"), **então** a resposta diz que só responde sobre pedidos.
6. **Dado** a chave ausente, **então** `503` com `ProblemDetails` "Módulo de IA desativado: configure ANTHROPIC_API_KEY"; a tela mostra esse aviso.
7. **Dado** um erro da API da Anthropic (limite, indisponibilidade), **então** `502`/`503` com mensagem amigável, sem vazar detalhes internos.
8. Pergunta vazia ou com mais de 500 caracteres → `400`.

## Design

```
api/Orders.Api/Ai/
  OrderAnalyticsService.cs   # loop de tool use (máx. 6 iterações)
  OrderAnalyticsTools.cs     # implementação das ferramentas com EF (AsNoTracking)
  AiOptions.cs               # ApiKey, Model, MaxTokens
api/Orders.Api/Contracts/AskRequest.cs, AskResponse.cs
frontend/src/pages/AskOrders.jsx
```

- SDK oficial **`Anthropic`** (NuGet) para C#; modelo `claude-opus-5-5` (configurável por `Ai__Model`).
- Endpoint beta de mensagens com `fallbacks: "default"` (beta `server-side-fallback-2026-07-01`): se o modelo recusar por política, o servidor tenta outro modelo na mesma chamada. Tratar `stop_reason == "refusal"` caso persista.
- **Ferramentas** (todas com filtros opcionais `status`, `from`, `to` em ISO-8601):
  - `count_orders` → quantidade.
  - `sum_order_values` → soma e ticket médio.
  - `avg_processing_time` → média, mínimo e máximo entre `Pendente` e `Finalizado`.
  - `orders_by_status` → contagem agrupada.
  - `list_recent_orders(limit ≤ 20)` → últimos pedidos (sem expor nada além dos campos do pedido).
- Prompt de sistema fixo (cacheável): papel, data/hora atual e fuso injetados como mensagem separada, instruções para responder em português, citar números exatos e nunca inventar dados.
- `toolCalls` retornado para transparência (a tela mostra "consultas realizadas").
- Sem chave: o serviço não é chamado; o endpoint responde 503 de imediato.

## Tasks

- [x] 10.1 Pacote `Anthropic`, `AiOptions` e registro condicional na DI.
- [x] 10.2 `OrderAnalyticsTools` com as 5 ferramentas e validação de parâmetros.
- [x] 10.3 `OrderAnalyticsService` com loop de tool use, limite de iterações, fallback e tratamento de recusa/erros.
- [x] 10.4 `POST /orders/ask` com validação e 503 sem chave.
- [x] 10.5 Tela "Pergunte" com exemplos clicáveis, histórico da conversa na sessão, consultas realizadas e aviso de desativado.
- [x] 10.6 Testes das ferramentas contra Postgres real (spec 12).

## Verificação

- Ferramentas: testes de integração com dados conhecidos (spec 12).
- Critérios 1–5: manual com chave configurada, usando as perguntas do desafio, conferindo os números no pgAdmin.
- Critério 6: sem a variável, `curl -X POST localhost:5000/orders/ask …` → 503.

## Notas de implementação

- Status **Em verificação**: tudo implementado e testado, mas as respostas do Claude (critérios 1–5 e 7) não foram exercitadas por falta de `ANTHROPIC_API_KEY` no ambiente de desenvolvimento.
- Verificado: critério 6 (503 sem chave, também em `OrdersApiTests`), critério 8 (400), e as 5 ferramentas em `OrderAnalyticsToolsTests` (contagens, somas, tempo médio pelo histórico, fuso de São Paulo, parâmetros inválidos).
- Endpoint extra `GET /orders/ask/status` para a tela saber se o módulo está ativo antes de perguntar.
- Para concluir: definir a chave no `.env`, `docker compose up -d api` e fazer as 4 perguntas do desafio na tela "Pergunte", conferindo os números no pgAdmin.
