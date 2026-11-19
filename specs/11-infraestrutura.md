# 11 — Infraestrutura (Docker Compose)

- **Status:** Concluída
- **Pontuação:** obrigatória (Documentação & DX 15%)

## Contexto

O Compose não sobe o worker nem o Service Bus, usa `version:` obsoleto, não há
`.env.example`, e os Dockerfiles de API e worker não enxergam código
compartilhado.

## Requisitos

- Compose com API, Worker, Frontend, PostgreSQL e PgAdmin — desafio §3.
- `.env` para variáveis sensíveis e exemplo de `.env` — desafio §3 e *Entrega*.
- Migrations automáticas — desafio §3 (spec 02).
- Healthchecks no Compose — desafio §3 (spec 05).

## Critérios de aceite

1. **Dado** um clone limpo, **quando** `cp .env.example .env && docker compose up --build -d`, **então** em poucos minutos todos os serviços estão `healthy` e o fluxo completo funciona sem nenhuma conta Azure.
2. Serviços: `postgres`, `pgadmin`, `sqlserver`, `servicebus`, `api`, `worker`, `frontend`, `jaeger`.
3. Ordem de subida garantida por `depends_on: condition: service_healthy` (api depois de postgres e servicebus; worker depois de api; frontend depois de api).
4. pgAdmin abre já com o servidor Postgres cadastrado.
5. Todas as senhas e chaves vêm do `.env`; `.env` é ignorado pelo git e `.env.example` é versionado.
6. Trocar `SERVICEBUS_CONNECTION_STRING` por um namespace Azure real funciona sem mudar código (as filas precisam existir no namespace).
7. `docker compose down -v` remove tudo, inclusive dados.

## Design

| Serviço | Imagem | Porta host | Healthcheck |
|---------|--------|-----------|-------------|
| postgres | `postgres:16-alpine` | 5432 | `pg_isready` |
| pgadmin | `dpage/pgadmin4` | 8080 | — |
| sqlserver | `mcr.microsoft.com/mssql/server:2022-latest` | — | `sqlcmd` |
| servicebus | `mcr.microsoft.com/azure-messaging/servicebus-emulator` | 5672 | via api `/health/ready` |
| api | build `api/Orders.Api/Dockerfile` (contexto raiz) | 5000 → 8080 | `curl /health/live` |
| worker | build `worker/Orders.Worker/Dockerfile` (contexto raiz) | — | heartbeat |
| frontend | build `frontend/Dockerfile` | 3000 → 80 | `wget` |
| jaeger | `jaegertracing/all-in-one` | 16686 | — |

- `servicebus/config.json`: namespace `sbemulatorns`, filas `order-created` e `order-status-changed` (`MaxDeliveryCount: 5`, `LockDuration: PT30S`, dead-letter ao expirar).
- `pgadmin/servers.json` montado em `/pgadmin4/servers.json`.
- `.dockerignore` na raiz (bin, obj, node_modules, .git, .env).
- Variáveis (`.env.example`): `POSTGRES_*`, `PGADMIN_*`, `MSSQL_SA_PASSWORD`, `SERVICEBUS_CONNECTION_STRING`, `ANTHROPIC_API_KEY` (opcional), `ASPNETCORE_ENVIRONMENT`, `WORKER_PROCESSING_DELAY_SECONDS`.
- `.gitignore`: trocar `*.env` por `.env` e `.env.*` com exceção `!.env.example`.

## Tasks

- [x] 11.1 Reescrever `docker-compose.yml` com os 8 serviços e healthchecks.
- [x] 11.2 `servicebus/config.json` com as filas.
- [x] 11.3 `pgadmin/servers.json`.
- [x] 11.4 `.env.example` e correção do `.gitignore`.
- [x] 11.5 `.dockerignore` na raiz; Dockerfiles de API e worker com contexto na raiz e restore em camada separada.

## Verificação

- Clone limpo → `cp .env.example .env && docker compose up --build -d` → `docker compose ps` todos `healthy`.
- Fluxo da spec 04 ponta a ponta.
- `http://localhost:8080` (pgAdmin) com o servidor já listado.

## Notas de implementação

- Arquivos de configuração ficam em `infra/` (`infra/servicebus/config.json`, `infra/pgadmin/servers.json`).
- Volume do Postgres renomeado para `orders_pgdata`: o volume `gestao-de-pedidos_postgres_data` da versão anterior (PostgreSQL 15 + schema antigo) é incompatível e foi mantido intacto, sem uso.
- Imagens: `postgres:16-alpine`, `dpage/pgadmin4:8`, `mssql/server:2022-latest`, `servicebus-emulator:2.0.1`, `jaegertracing/all-in-one:1.62.0`.
- Verificado: `docker compose config`, build das 3 imagens e subida completa com o fluxo ponta a ponta funcionando.
