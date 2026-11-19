# 06 — Tempo real com SignalR + fallback

- **Status:** Concluída
- **Pontuação:** bônus **SignalR/WebSockets com fallback (+3)**; apoia *Exibir feedback visual para mudanças de status* (Frontend & UX 10%)

## Contexto

O status muda no worker, que é um processo separado da API; hoje o frontend só
vê a mudança se o usuário recarregar a página.

## Requisitos

- Feedback visual para mudanças de status — desafio §2.
- SignalR/WebSockets com fallback — desafio §5.

## Critérios de aceite

1. **Dado** a tela de lista ou de detalhes aberta, **quando** o worker muda o status de um pedido, **então** a tela reflete a mudança em até ~2 s (intervalo do outbox + entrega), sem recarregar.
2. **Dado** WebSocket indisponível no navegador ou no proxy, **então** o SignalR negocia outro transporte (Server-Sent Events / long polling) automaticamente.
3. **Dado** a conexão com o hub caída (API reiniciando), **então** o frontend passa a fazer polling a cada 5 s, mostra o indicador "Polling" e tenta reconectar; ao reconectar, volta para "Tempo real" e para o polling.
4. **Dado** um pedido criado em outra aba, **então** ele aparece na lista aberta.

## Design

Backend (API):
- `Hubs/OrdersHub.cs` mapeado em `/hubs/orders`. Evento enviado ao cliente: `orderStatusChanged` com `{ orderId, status, changedAt }`.
- `Messaging/StatusChangedConsumer.cs`: `ServiceBusProcessor` na fila `order-status-changed` → `IHubContext<OrdersHub>.Clients.All.SendAsync(...)`.
- Criação de pedido também emite `orderStatusChanged` com `Pendente` (atende o critério 4).
- Por que via fila e não chamada HTTP do worker para a API: mantém o worker desacoplado e funciona com N instâncias da API (cada uma consome e notifica seus clientes; para múltiplas instâncias em produção, trocar por *topic* com uma *subscription* por instância ou Azure SignalR Service — documentado no README).

Frontend:
- `hooks/useOrderUpdates.js` com `@microsoft/signalr`: `withAutomaticReconnect()`, callbacks `onreconnecting`/`onreconnected`/`onclose`.
- Estado da conexão exposto num contexto (`RealtimeProvider`) → indicador no header.
- Enquanto desconectado: `setInterval` de 5 s recarrega os dados da tela atual.

## Tasks

- [x] 6.1 `OrdersHub` em `/hubs/orders`.
- [x] 6.2 `StatusChangedConsumer` e notificação na criação do pedido.
- [x] 6.3 `RealtimeProvider` + `useOrderUpdates` com reconexão e fallback para polling.
- [x] 6.4 Indicador "Tempo real / Polling" no header.
- [x] 6.5 nginx com upgrade de WebSocket em `/hubs/` (junto da spec 09).

## Verificação

- Critério 1: Compose no ar, abrir a lista, criar pedido em outra aba e observar Pendente → Processando → Finalizado.
- Critério 3: `docker compose restart api` com a tela aberta; indicador muda para "Polling" e volta.
- Critério 2: DevTools → *Network* → WS mostra a conexão; documentar o teste com `transport: LongPolling` forçado.

## Notas de implementação

- Arquivos reais: `frontend/src/realtime/RealtimeProvider.jsx`, `context.js` e `useOrderUpdates.js`. A primeira conexão é tentada a cada 5 s até dar certo (a reconexão automática do SignalR só cobre quedas depois de conectado); ao reconectar, a tela sincroniza uma vez.
- Verificado: negociação via nginx oferece WebSockets, ServerSentEvents e LongPolling; o consumidor `order-status-changed` aparece no trace do Jaeger.
- **Pendente de verificação manual no navegador:** critérios 1, 3 e 4 (atualização da tela, troca do indicador para "Polling" com `docker compose restart api`).
