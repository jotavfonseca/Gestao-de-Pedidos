# 15 — English adaptation for an international audience

- **Status:** In verification
- **Score:** Documentation & DX, Frontend & UX (presentation to US recruiters)

## Context

The project will be shown to US recruiters. The domain, API, UI, logs, specs and
documentation were in Portuguese because the TMB challenge brief is in Portuguese.
The owner chose a **full** translation, including the domain and the API contract,
knowing that this **intentionally deviates** from the brief's Portuguese field names
(`cliente`, `produto`, `valor`, `data_criacao`) and statuses (`Pendente`,
`Processando`, `Finalizado`).

## Requirements

- Everything a reader sees is in English: README, specs, UI, API contract,
  error messages, logs, code comments, AI answers, commit messages.
- The original challenge brief stays untouched (`docs/desafio-poc-tmb.md`), with an
  English summary next to it and a clear note about the renamed fields.
- A Portuguese README remains available as a secondary file.
- Existing databases keep working: renaming is done by a migration, not by
  recreating the schema.

## Name mapping

| Before (pt-BR) | After (en) |
|---|---|
| `Cliente` / `cliente` | `Customer` / `customer` |
| `Produto` / `produto` | `Product` / `product` |
| `Valor` / `valor` | `Amount` / `amount` |
| `DataCriacao` / `dataCriacao` | `CreatedAt` / `createdAt` |
| `Historico` / `historico` | `StatusHistory` / `statusHistory` |
| `OrderStatusHistory.Data` / `data` | `ChangedAt` / `changedAt` |
| `Pendente` → `Processando` → `Finalizado` | `Pending` → `Processing` → `Completed` |
| `OrderStatusChanged.De` / `.Para` | `From` / `To` |
| message `statusAnterior` | `previousStatus` |
| AI tool output (`quantidade`, `valorTotal`, …) | `count`, `totalAmount`, … |

Currency formatting in the UI moves from BRL (`pt-BR`) to USD (`en-US`). Amounts are
stored without currency, so no data changes. The AI module's time zone stays
configurable (`Ai__TimeZone`, default `America/Sao_Paulo`).

## Acceptance criteria

1. **Given** a request `{ "customer": "Jane Doe", "product": "Keyboard", "amount": 349.90 }`, **when** `POST /orders`, **then** `201` with `status: "Pending"` and `statusHistory`.
2. **Given** an order being processed, **then** it goes `Pending → Processing → Completed`.
3. **Given** a database created by the previous version, **when** the API starts, **then** a migration renames the columns and converts stored statuses (`Pendente` → `Pending`, etc.) without data loss.
4. Validation and domain errors are in English (e.g., `"customer": ["Customer is required."]`).
5. The UI, toasts, empty states and the Ask page are in English; amounts show as USD.
6. The AI module answers in English and its tools return English field names.
7. `README.md` is in English; `README.pt-BR.md` is the Portuguese version; both link to each other.
8. All specs are in English with English file names; links across the repo are updated.
9. Golden files reflect the new contract; all tests pass; CI is green.

## Design

- Domain and contracts renamed per the table above; golden files regenerated.
- Migration `RenameToEnglish` (hand-reviewed): `RenameColumn` for each column and
  index, plus `UPDATE` statements mapping the status values in `Orders` and
  `OrderStatusHistory`. `Down` reverses both.
- Messages already waiting in the outbox in the old format are not converted (the
  project has no production data); this is documented.
- Specs renamed: `01-shared-core-and-domain.md`, `02-rest-api.md`,
  `03-outbox-service-bus.md`, `04-idempotent-worker.md`, `05-health-checks.md`,
  `06-signalr-fallback.md`, `07-status-history.md`, `08-tracing.md`, `09-frontend.md`,
  `10-ai-module.md`, `11-infrastructure.md`, `12-tests.md`, `13-documentation.md`,
  `14-ci-and-portfolio.md`.
- The constitution switches the "domain language" rule to English and records why.

## Tasks

- [x] 15.1 Rename domain, application, contracts and messages.
- [x] 15.2 Migration `RenameToEnglish` with status value conversion.
- [x] 15.3 API, worker and AI module in English (errors, logs, prompt, tool output).
- [x] 15.4 Tests and golden files updated.
- [x] 15.5 Frontend in English (en-US, USD).
- [x] 15.6 Infra comments, `.env.example`, pgAdmin server name.
- [x] 15.7 Specs translated and renamed; constitution updated.
- [x] 15.8 `README.md` in English, `README.pt-BR.md`, English summary of the brief, `CLAUDE.md`.
- [ ] 15.9 New PR with CI green.

## Verification

- `dotnet test` (all green) and `npm run lint && npm run build`.
- Criterion 3: start the new API against the database created by the previous version (the running Compose volume) and check the data.
- `docker compose up --build -d`, create an order, watch `Pending → Processing → Completed`.

## Implementation notes

- `Money.Amount` became `Money.Value`, so the aggregate reads `order.Amount.Value` instead of `order.Amount.Amount`.
- The AI tool was renamed `sum_order_values` → `sum_order_amounts`; tool outputs use `count`, `totalAmount`, `averageAmount`, `ordersConsidered`, `averageSeconds`…
- Migration `RenameToEnglish`: EF detected every column and index rename (no drop/add); the status conversion is a hand-written `UPDATE` in both directions.
- Verified criterion 3 twice: `MigrationTests` (Portuguese data → upgrade → read back in English → rollback) and on the real Compose database, where the 4 orders created by the Portuguese version came back as `Completed` with their translated history.
- Verified: 53 tests passing; frontend lint/build clean; full flow in Compose (`Pending → Processing → Completed`, English validation errors, "Order Management" title).
- The Compose project name and repository name stay `gestao-de-pedidos` / `Gestao-de-Pedidos` (renaming the GitHub repository is the owner's call). The local `.env` (git-ignored) keeps its Portuguese comments.
- Commits from this spec on are in English; earlier commit messages stay in Portuguese (history is not rewritten).
