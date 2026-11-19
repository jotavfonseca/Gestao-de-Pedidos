# 14 — CI, English README and reproducible builds

- **Status:** Done
- **Score:** Documentation & DX 15%, Automated Tests 10% (project presentation)

## Context

The project ran and had 52 tests, but nothing ran them automatically: a visitor had
no way to know whether `main` was green. The README only existed in Portuguese, and
the Service Bus emulator used the `latest` tag, which could break `docker compose up`
when Microsoft publishes a new version.

## Requirements

- Visible automated tests — evaluation criteria (Automated Tests 10%).
- `README.md` with clear instructions — challenge *Delivery*.
- Reproducible Docker Compose — challenge §3.

## Acceptance criteria

1. **Given** a push or pull request to `main`, **then** GitHub Actions runs: .NET build and tests (including Testcontainers integration tests), frontend lint and build, and the Docker image build.
2. Any failure in those steps turns the workflow red.
3. The README shows the workflow badge.
4. An English README exists and links to the other language at the top.
5. The Service Bus emulator uses a pinned version (`2.0.1`, the image validated on Nov 19, 2026).
6. The git history has commits organized by layer/spec, in Conventional Commits, each one compiling on its own.

## Design

- `.github/workflows/ci.yml` with three independent jobs:
  - `backend` — `ubuntu-latest`, .NET 8: `restore`, `build -c Release`, `test` (the runner has Docker, so Testcontainers works).
  - `frontend` — Node 22: `npm ci`, `npm run lint`, `npm run build`.
  - `docker` — `cp .env.example .env`, `docker compose config` and `docker compose build`.
- `concurrency` cancels older runs of the same branch; test results (`.trx`) uploaded as an artifact.
- `docker-compose.yml`: `mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1`.
- Commits: since several specs touch the same files (e.g. the API's `Program.cs`), commits are grouped by layer, each citing the specs it implements, so every commit compiles.

## Tasks

- [x] 14.1 `.github/workflows/ci.yml` workflow.
- [x] 14.2 Validate build and tests on Linux (the CI runner) before the first push.
- [x] 14.3 Pin the emulator to `2.0.1`.
- [x] 14.4 English README + language switcher and badge.
- [x] 14.5 Commits organized by layer/spec.

## Verification

- Criteria 1–2: first push → the *Actions* tab on GitHub.
- Criterion 5: `docker compose config | grep servicebus-emulator`.
- Criterion 6: `git log --oneline` and building each commit in a separate worktree.

## Implementation notes

- Validated locally on Linux (`mcr.microsoft.com/dotnet/sdk:8.0` container with Docker access): restore, Release build with no warnings and all tests passing, including Testcontainers.
- Emulator `2.0.1` checked: same image (digest `sha256:5a96d893…`) as the validated `latest`; `/health/ready` healthy after the switch.
- PR #1: all three CI jobs green, backend with 52 tests run and none skipped.
- Each code commit was built in a separate worktree: all compile.
- Spec 15 later made the English README the main one (`README.md`) and moved the Portuguese version to `README.pt-BR.md`.
