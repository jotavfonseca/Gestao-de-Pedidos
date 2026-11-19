# 10 — AI module: "Ask about your orders"

- **Status:** In verification
- **Score:** optional / bonus **AI/Analytics module with an LLM (+5)**

## Context

New module. Users ask questions in natural language and get answers based on real
data from the database.

## Requirements

- The LLM interprets the question, queries the database and answers in a friendly way with real data — challenge §4.
- Example questions that must work (translated from the brief): "How many orders do we have today?", "What is the average time to approve orders?", "How many orders are pending?", "What is the total amount of completed orders this month?".

## Acceptance criteria

1. **Given** `ANTHROPIC_API_KEY` configured, **when** `POST /orders/ask` with `{ "question": "How many orders are pending?" }`, **then** `200` with `{ answer, toolCalls[] }`, and the number in the answer matches the database.
2. The four example questions are answered correctly (relative dates such as "today" and "this month" interpreted in the configured time zone, default `America/Sao_Paulo`).
3. "Average time to approve" is computed from the history (`Pending` → `Completed`) and the answer says what was measured.
4. **Safety**: the LLM does **not** run free-form SQL; it only calls fixed, read-only tools with validated parameters. A request to change or delete data gets a polite refusal.
5. **Given** an off-topic question (e.g. "what is the capital of France?"), **then** the answer says it only handles questions about orders.
6. **Given** no key, **then** `503` with `ProblemDetails` "AI module disabled: set ANTHROPIC_API_KEY"; the page shows this notice.
7. **Given** an Anthropic API error (rate limit, outage), **then** `502`/`503` with a friendly message, without leaking internal details.
8. Empty question or longer than 500 characters → `400`.

## Design

```
api/Orders.Api/Ai/
  OrderAnalyticsService.cs        # tool-use loop (max 6 iterations)
  OrderAnalyticsTools.cs          # tools implemented with EF (AsNoTracking)
  OrderAnalyticsToolDefinitions.cs
  AiOptions.cs                    # ApiKey, Model, MaxTokens, TimeZone
api/Orders.Api/Contracts/AskContracts.cs
frontend/src/pages/AskOrders.jsx
```

- Official **`Anthropic`** C# SDK (NuGet); model `claude-opus-5-5` (configurable via `Ai__Model`).
- Beta messages endpoint with `fallbacks: "default"` (beta `server-side-fallback-2026-07-01`): if the model refuses for policy reasons, the server retries with another model in the same call. `stop_reason == "refusal"` is handled if it persists.
- **Tools** (all with optional `status`, `from`, `to` filters in ISO-8601):
  - `count_orders` → count.
  - `sum_order_amounts` → total and average order value.
  - `avg_processing_time` → average, minimum and maximum between `Pending` and `Completed`.
  - `orders_by_status` → grouped counts.
  - `list_recent_orders(limit ≤ 20)` → latest orders (only order fields).
- Fixed system prompt: role, rules, answers in English, amounts in USD, exact numbers, never invent data. The current date and time zone go in the user message.
- `toolCalls` returned for transparency (the page shows "queries run").
- Without a key the service is never called; the endpoint returns 503 immediately.

## Tasks

- [x] 10.1 `Anthropic` package, `AiOptions` and conditional DI registration.
- [x] 10.2 `OrderAnalyticsTools` with the 5 tools and parameter validation.
- [x] 10.3 `OrderAnalyticsService` with the tool-use loop, iteration limit, fallback and refusal/error handling.
- [x] 10.4 `POST /orders/ask` with validation and 503 without a key.
- [x] 10.5 "Ask" page with clickable examples, conversation history in the session, queries run and a disabled notice.
- [x] 10.6 Tool tests against a real PostgreSQL (spec 12).

## Verification

- Tools: integration tests with known data (spec 12).
- Criteria 1–5: manual check with a key configured, using the example questions and comparing the numbers in pgAdmin.
- Criterion 6: without the variable, `curl -X POST localhost:5000/orders/ask …` → 503.

## Implementation notes

- Status **In verification**: everything is implemented and tested, but Claude's answers (criteria 1–5 and 7) have not been exercised because no `ANTHROPIC_API_KEY` was available in the development environment.
- Verified: criterion 6 (503 without a key, also in `OrdersApiTests`), criterion 8 (400), and the 5 tools in `OrderAnalyticsToolsTests` (counts, sums, average time from the history, time zone, invalid parameters).
- Extra endpoint `GET /orders/ask/status` lets the page know whether the module is enabled before asking.
- To finish: set the key in `.env`, run `docker compose up -d api` and ask the 4 example questions on the "Ask" page, comparing the numbers in pgAdmin.
