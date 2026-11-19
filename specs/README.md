# Specs — Order Management

This project follows **Spec Driven Development (SDD)**: every change starts with a
specification in `specs/`, and the code is written to meet its acceptance criteria.

- [`constitution.md`](constitution.md) — fixed principles (stack, challenge rules, tactical DDD architecture, conventions, definition of done).
- [`current-state.md`](current-state.md) — assessment of the project before this round of specs.
- Original requirements: [`../docs/desafio-poc-tmb.md`](../docs/desafio-poc-tmb.md) (Portuguese) · [English summary](../docs/challenge-brief.en.md).

## Features

| # | Feature | Type | Points | Status |
|---|---------|------|--------|--------|
| 01 | [Shared core and domain (tactical DDD)](01-shared-core-and-domain.md) | Foundation | — | Done |
| 02 | [REST API](02-rest-api.md) | Required | — | Done |
| 03 | [Outbox + Service Bus](03-outbox-service-bus.md) | Required + bonus | +3 | Done |
| 04 | [Idempotent worker](04-idempotent-worker.md) | Required | — | Done |
| 05 | [Health checks](05-health-checks.md) | Required | — | Done |
| 06 | [Real time with SignalR + fallback](06-signalr-fallback.md) | Bonus | +3 | Done |
| 07 | [Status history](07-status-history.md) | Bonus | +3 | Done |
| 08 | [End-to-end tracing](08-tracing.md) | Bonus | +2 | Done |
| 09 | [Frontend (React + Tailwind)](09-frontend.md) | Required | — | Done |
| 10 | [AI module — Ask about your orders](10-ai-module.md) | Optional / bonus | +5 | In verification |
| 11 | [Infrastructure (Docker Compose)](11-infrastructure.md) | Required | — | Done |
| 12 | [Automated tests](12-tests.md) | Bonus | +3 | Done |
| 13 | [Documentation and DX](13-documentation.md) | Required | — | Done |
| 14 | [CI, English README and reproducible builds](14-ci-and-portfolio.md) | DX / presentation | — | Done |
| 15 | [English adaptation](15-english-adaptation.md) | DX / presentation | — | Done |

Statuses: `Planned` → `In progress` → `In verification` (implemented, some criterion still to verify) → `Done`.

## Execution order

01 → 02 → 03 → 04 → 05 → 07 → 06 → 08 → 10 → 09 → 11 → 12 → 13 → 14 → 15

The backend is built and tested before the frontend; Compose comes last to
validate everything together.

## Workflow

1. **Specify** — write or update the feature spec (requirements, acceptance criteria, design, tasks).
2. **Implement** — write code to meet the acceptance criteria, one task at a time.
3. **Verify** — run the spec's *Verification* section.
4. **Update** — tick the tasks `[x]`, update the status in the spec and in this table.

Scope changed during implementation? Update the spec **before** the code.

## Spec template

Each `NN-slug.md` has these sections:

- **Status** and **Score**
- **Context** — what exists today and why it should change
- **Requirements** — referencing the challenge section
- **Acceptance criteria** — Given / When / Then
- **Design** — files, components and decisions
- **Tasks** — checklist `- [ ] N.M`
- **Verification** — how to prove each criterion
- **Implementation notes** — what was verified and what changed from the plan

> **Naming note:** specs 01–14 were written while the domain still used the brief's
> Portuguese names (`cliente`, `Pendente`, …). They were translated together with
> the code in spec 15 and now use the English names (`customer`, `Pending`, …).
