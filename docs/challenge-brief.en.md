# Challenge brief (English summary)

Summary of the original brief, [`desafio-poc-tmb.md`](desafio-poc-tmb.md) (Portuguese),
for the **POC TMB** technical challenge. The original is kept unchanged.

> **Naming:** the brief uses Portuguese names. This project renamed them to English
> on purpose ([spec 15](../specs/15-english-adaptation.md)):
>
> | Brief | Project |
> |---|---|
> | `cliente`, `produto`, `valor`, `data_criacao` | `customer`, `product`, `amount`, `createdAt` |
> | `Pendente` → `Processando` → `Finalizado` | `Pending` → `Processing` → `Completed` |

## Goal

Build a simple order management system to create, list and view orders. Whenever an
order is created, the system sends a message to Azure Service Bus to simulate
asynchronous processing. A worker consumes the messages, processes the order and
updates its status.

## Required technologies

- **Backend:** C# (.NET 7 or later) + Entity Framework + PostgreSQL
- **Frontend:** React + TailwindCSS
- **Messaging:** Azure Service Bus
- **Infrastructure:** Docker / Docker Compose (preferred)

## Requirements

### 1. Backend (C# API)

| Method | Route | Description |
|---|---|---|
| POST | `/orders` | Creates an order |
| GET | `/orders` | Lists all orders |
| GET | `/orders/{id}` | Gets an order's details |

Business rules:
- Each order has `id`, customer, product, amount, status and creation date.
- Statuses: pending, processing, completed.
- On creation, persist with EF to PostgreSQL and publish a message to Azure Service Bus.
- A worker consumes messages, sets the status to "processing" and, 5 seconds later, to "completed".

Additional rules:
- Mandatory status sequence: pending → processing → completed.
- The Service Bus consumer must be idempotent.
- Include `CorrelationId=OrderId` and `EventType=OrderCreated`.
- Health checks for the API, the database and the queue.

### 2. Frontend (React + TailwindCSS)

- List orders in a responsive table.
- Create orders through a form.
- View order details.
- Visual feedback for status changes.

### 3. Infrastructure

- Docker Compose with API, Worker, Frontend, PostgreSQL and PgAdmin.
- `.env` for sensitive variables.
- Automatic migrations.
- Healthchecks in Compose.

### 4. Optional module — Ask about the orders (AI/Analytics)

Users ask natural-language questions about the orders, answered by an LLM (OpenAI,
Azure OpenAI, Gemini, etc.) that interprets the question, queries the database and
answers in a friendly way using real data. Examples:

- How many orders do we have today?
- What is the average time to approve orders?
- How many orders are pending?
- What is the total amount of completed orders this month?

### 5. Technical bonuses

| Bonus | Points |
|---|---|
| Outbox Pattern (transactional messaging) | +3 |
| Order status history | +3 |
| SignalR/WebSockets with fallback | +3 |
| Testcontainers | +1 |
| End-to-end tracing | +2 |
| Golden tests | +2 |
| AI/Analytics module with an LLM | +5 |

## Evaluation criteria

| Criterion | Weight |
|---|---|
| Code quality | 30% |
| Messaging & reliability | 20% |
| Functionality | 15% |
| Documentation & DX | 15% |
| Frontend & UX | 10% |
| Automated tests | 10% |

## Delivery

A GitHub repository with the full source code, a `README.md` with clear instructions
to run the project, a sample `.env` and simple architecture diagrams (a differentiator).
