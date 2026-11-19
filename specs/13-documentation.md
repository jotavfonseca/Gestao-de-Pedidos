# 13 — Documentation and DX

- **Status:** Done
- **Score:** required (Documentation & DX 15%) + diagrams (delivery differentiator)

## Context

The previous README was generic, referenced paths that did not exist
(`./src/Api/Api.csproj`) and unused variables, and had no diagram.

## Requirements

- `README.md` with clear instructions to run the project — challenge *Delivery*.
- A sample `.env` — challenge *Delivery* (spec 11).
- Simple architecture diagrams — challenge *Delivery* (differentiator).

## Acceptance criteria

1. Someone with no context can run the project following **only** the README, with 2 commands after cloning.
2. The README has an architecture diagram and a sequence diagram of an order's lifecycle (Mermaid, rendered by GitHub).
3. A URL table: frontend, Swagger, health, pgAdmin, Jaeger.
4. An environment variables section in sync with `.env.example`.
5. Sections: running without Docker, running the tests, design decisions (outbox, idempotency, emulator, SignalR, AI module safety), challenge requirement → implementation mapping, troubleshooting.
5a. An **"Architecture and DDD"** section: which tactical patterns were used (aggregate, value object, domain events, application layer), which were deliberately left out (generic repositories, MediatR/CQRS, one project per layer, bounded contexts) and why — a summary of [`constitution.md` §4](constitution.md#4-architecture-tactical-ddd-no-strategic-ceremony), including the *aggregate → domain event → outbox → Service Bus* flow.
6. Every command in the README was run and works.
7. `CLAUDE.md` describes the current structure and the SDD workflow with `specs/`.

## Design

README structure:

1. Overview and features
2. Architecture (component + sequence diagrams)
3. Getting started (Docker) — 2 commands
4. URLs
5. Environment variables
6. API (endpoints, `curl` examples, JSON shape)
7. Running without Docker
8. Tests
9. Architecture and DDD + other design decisions
10. Challenge requirements × implementation (linking the specs)
11. Troubleshooting
12. Project structure

## Tasks

- [x] 13.1 Rewrite `README.md` following the structure above.
- [x] 13.2 Update `CLAUDE.md` with `shared/`, `tests/` and the SDD workflow in `specs/`.
- [x] 13.3 README points to `specs/` as the source of the decisions.
- [x] 13.4 Remove the Vite template's `frontend/README.md`.
- [x] 13.5 "Architecture and DDD" section with the aggregate → event → outbox → queue diagram.

## Verification

- Run every README command on a fresh clone.
- View the README on GitHub (or a Mermaid viewer) to check the diagrams.

## Implementation notes

- Run: `cp .env.example .env`, `docker compose up --build -d`, `docker compose ps/logs`, the `curl` examples, `dotnet test`, `npm run lint/build`, `dotnet run --project worker/Orders.Worker`.
- Not run: `dotnet run --project api/Orders.Api` outside Docker (port 5000 taken by the container), `npm run dev`.
- Mermaid diagrams render on GitHub (checked after the first push).
