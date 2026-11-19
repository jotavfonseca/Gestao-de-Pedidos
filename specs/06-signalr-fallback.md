# 06 — Real time with SignalR + fallback

- **Status:** Done
- **Score:** bonus **SignalR/WebSockets with fallback (+3)**; supports *Visual feedback for status changes* (Frontend & UX 10%)

## Context

The status changes in the worker, a separate process from the API; the frontend
only saw the change after a page reload.

## Requirements

- Visual feedback for status changes — challenge §2.
- SignalR/WebSockets with fallback — challenge §5.

## Acceptance criteria

1. **Given** the list or details page open, **when** the worker changes an order's status, **then** the page reflects it within ~2 s (outbox interval + delivery), without reloading.
2. **Given** WebSocket unavailable in the browser or the proxy, **then** SignalR negotiates another transport (Server-Sent Events / long polling) automatically.
3. **Given** the hub connection lost (API restarting), **then** the frontend polls every 5 s, shows the "Polling" indicator and tries to reconnect; once reconnected it goes back to "Live" and stops polling.
4. **Given** an order created in another tab, **then** it appears in the open list.

## Design

Backend (API):
- `Realtime/OrdersHub.cs` mapped to `/hubs/orders`. Event sent to clients: `orderStatusChanged` with `{ orderId, status, changedAt }`.
- `Realtime/StatusChangedConsumer.cs`: a `ServiceBusProcessor` on the `order-status-changed` queue → `IHubContext<OrdersHub>.Clients.All.SendAsync(...)`.
- Order creation also emits `orderStatusChanged` with `Pending` (criterion 4).
- Why a queue and not an HTTP call from the worker to the API: it keeps the worker decoupled. For several API instances in production, the queue should become a *topic* with one *subscription* per instance, or Azure SignalR Service — documented in the README.

Frontend:
- `realtime/RealtimeProvider.jsx` + `useOrderUpdates.js` with `@microsoft/signalr`: automatic reconnect and `onreconnecting`/`onreconnected`/`onclose` callbacks.
- Connection state exposed through a context → indicator in the header.
- While disconnected: a 5 s `setInterval` reloads the current page's data.

## Tasks

- [x] 6.1 `OrdersHub` at `/hubs/orders`.
- [x] 6.2 `StatusChangedConsumer` and the notification on order creation.
- [x] 6.3 `RealtimeProvider` + `useOrderUpdates` with reconnection and polling fallback.
- [x] 6.4 "Live / Polling" indicator in the header.
- [x] 6.5 nginx with WebSocket upgrade on `/hubs/` (with spec 09).

## Verification

- Criterion 1: Compose running, open the list, create an order in another tab and watch Pending → Processing → Completed.
- Criterion 3: `docker compose restart api` with the page open; the indicator switches to "Polling" and back.
- Criterion 2: DevTools → *Network* → WS shows the connection.

## Implementation notes

- The first connection is retried every 5 s until it succeeds (SignalR's automatic reconnect only covers drops after a successful connection); on reconnect the page syncs once.
- Verified: negotiation through nginx offers WebSockets, ServerSentEvents and LongPolling; the `order-status-changed` consumer shows up in the Jaeger trace.
- **Pending manual browser check:** criteria 1, 3 and 4 (page updates, indicator switching to "Polling" with `docker compose restart api`).
