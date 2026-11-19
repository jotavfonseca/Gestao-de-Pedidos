# 05 — Health checks

- **Status:** Concluída
- **Pontuação:** obrigatória

## Contexto

Hoje `/health` só verifica o Postgres. Não há verificação da fila, não há
separação entre *liveness* e *readiness*, e o worker (que não é web) não tem
healthcheck no Compose.

## Requisitos

- Health checks para API, banco e fila — desafio §1 *Regras adicionais*.
- Healthchecks no Compose — desafio §3.

## Critérios de aceite

1. **Quando** `GET /health/live`, **então** `200` enquanto o processo da API estiver de pé (não depende de banco nem fila).
2. **Quando** `GET /health/ready` (ou `/health`), **então** o JSON lista `self`, `postgres` e `servicebus` com status e duração; `200` se todos `Healthy`, `503` se algum `Unhealthy`.
3. **Dado** o Postgres parado, **então** `/health/ready` responde `503` com `postgres: Unhealthy`.
4. **Dado** o emulador do Service Bus parado, **então** `/health/ready` responde `503` com `servicebus: Unhealthy`.
5. No Compose, `docker compose ps` mostra `healthy` para postgres, servicebus (ou sqlserver), api, worker, frontend e jaeger quando tudo está no ar.

## Design

- `api/Orders.Api/Health/ServiceBusHealthCheck.cs`: cria um receiver da fila `order-created` e faz `PeekMessageAsync` com timeout de 5 s. Escolhido porque o emulador **não** suporta as operações de administração usadas pelo pacote `AspNetCore.HealthChecks.AzureServiceBus`.
- Postgres: `AddNpgSql` (pacote `AspNetCore.HealthChecks.NpgSql`).
- Tags `live` / `ready` para separar os endpoints; resposta JSON própria (`status`, `totalDuration`, `checks[]`).
- Worker: arquivo de heartbeat atualizado a cada 10 s; healthcheck do Compose verifica se foi modificado no último minuto.
- Frontend (nginx): `wget -q --spider http://localhost/` (busybox do alpine).

## Tasks

- [x] 5.1 Endpoints `/health/live`, `/health/ready` e `/health` com writer JSON.
- [x] 5.2 `ServiceBusHealthCheck` com peek e timeout.
- [x] 5.3 Healthchecks de todos os serviços no Compose (feito junto com a spec 11).

## Verificação

- `curl -i localhost:5000/health/live` e `curl -s localhost:5000/health/ready | jq`.
- Critérios 3–4: `docker compose stop postgres` / `stop servicebus` e repetir o curl.
- Critério 5: `docker compose ps`.

## Notas de implementação

- O healthcheck do Compose da **API usa `/health/ready`**: o container só fica `healthy` quando PostgreSQL e Service Bus estão prontos, e worker/frontend esperam por isso.
- O emulador do Service Bus não tem healthcheck próprio (a imagem não traz ferramenta para isso); a prontidão dele é medida pelo check `servicebus` da API. Jaeger e pgAdmin também ficam sem healthcheck.
- Frontend: `wget` em `127.0.0.1` (em `localhost` o busybox pode tentar IPv6).
- Verificado: `docker compose ps` com api, worker, frontend, postgres e sqlserver `healthy`; `/health/ready` = 503 com o Service Bus parado.
