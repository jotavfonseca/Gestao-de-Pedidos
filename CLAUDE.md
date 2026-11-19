# Order Management — context for Claude

This project is the solution to the **POC TMB** technical challenge. The full brief
(requirements, business rules, bonuses and evaluation criteria) is in Portuguese at:

@docs/desafio-poc-tmb.md

An English summary is in `docs/challenge-brief.en.md`. Follow the brief's
requirements when making changes (required stack: .NET + EF + PostgreSQL,
React + TailwindCSS, Azure Service Bus, Docker Compose).

Everything in the project is in **English** (code, domain, API, UI, logs, comments,
docs, commits). The brief's Portuguese field and status names were renamed on
purpose — see `specs/15-english-adaptation.md` for the mapping.

Layout: `api/` (C# REST API), `worker/` (Service Bus consumer), `shared/` (domain,
DbContext, migrations and shared contracts), `tests/` (automated tests),
`frontend/` (React), `infra/` (Service Bus emulator and pgAdmin config),
`specs/` (specifications), `docker-compose.yml` at the root.

## Spec Driven Development

The project follows SDD. Before changing code:

1. Read `specs/constitution.md` (principles that apply to everything) and the spec
   of the feature involved in `specs/NN-*.md`. The index and statuses are in `specs/README.md`.
2. Implement to meet the spec's **acceptance criteria**, task by task.
3. When done, tick the tasks `[x]`, update the spec's **Status** and the table in
   `specs/README.md`.
4. If the scope changes, update the spec **before** the code. A change with no
   matching spec: write the spec first.
