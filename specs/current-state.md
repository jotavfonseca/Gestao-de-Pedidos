# Estado atual (diagnóstico de 19/11/2026)

Retrato do projeto **antes** das specs 01–13. Serve de linha de base: cada
problema listado aqui é resolvido por pelo menos uma spec.

## Resumo

O projeto **não roda de ponta a ponta**: o worker não compila, a API não publica
no Service Bus, o worker não está no Compose e a tela de detalhes não existe.

## Por área

### API — `api/Orders.Api`

| Existe | Problema | Resolvido em |
|--------|----------|--------------|
| `POST /orders`, `GET /orders`, `GET /orders/{id}` | POST recebe a entidade EF inteira (cliente pode mandar `status`, `historico`); captura exceção e relança `Exception` genérica | 02 |
| `ServiceBusPublisher` | **Nunca é chamado** ("Nenhuma publicação na fila"); cria um sender por envio e nunca descarta; usa `Subject` em vez de `EventType` | 03 |
| `OrderContext` + migration `InitialCreate` | `OrderStatusHistory` está na migration mas não tem `DbSet` | 01 |
| `OrderStatus` enum | Serializado como número; o frontend espera texto | 02 |
| `/health` | Só checa o Postgres; nada da fila | 05 |
| Migrations no startup | Sem retry: se o banco demorar, a API cai | 02 |
| `Orders.Api.http` | Aponta para `/weatherforecast` (template) | 02 |

### Worker — `worker/Orders.Worker`

| Existe | Problema | Resolvido em |
|--------|----------|--------------|
| `BackgroundService` com Pendente → Processando → Finalizado e 5 s | **Não compila**: usa `_context.Historico`, que não existe no `OrderContext` | 01, 04 |
| Injeção de `OrderContext` | `DbContext` scoped dentro de hosted service singleton → erro na inicialização | 04 |
| Leitura de pedidos | **Polling no banco**; não usa Service Bus; sem idempotência real | 04 |
| `ProjectReference` para a API | Worker depende do projeto Web; Dockerfile com contexto só do worker não enxerga a API | 01, 04 |
| — | **Não está no docker-compose** | 11 |

### Frontend — `frontend`

| Existe | Problema | Resolvido em |
|--------|----------|--------------|
| Lista de pedidos com badge | Sem atualização automática de status | 06, 09 |
| Formulário de criação | Sem validação além de `required` | 09 |
| `OrderDetails.jsx` | **É cópia do formulário de criação**; não exibe detalhes | 07, 09 |
| Tailwind | Não está ligado: `index.css` é CSS pré-compilado estático; `@tailwindcss/vite` instalado e não usado; `input.css`/`postcss.config.mjs` soltos | 09 |
| `services/api.js` | URL fixa `http://localhost:5000` | 09 |
| Dockerfile | Node 18 (Vite 7 exige Node ≥ 20) | 09 |
| — | Sobras do template Vite (`App.css`, `react.svg`, `.eslintrc.json` legado, título "Vite + React") | 09 |

### Infraestrutura

| Existe | Problema | Resolvido em |
|--------|----------|--------------|
| Compose com postgres, pgadmin, api, frontend | Sem worker e sem Service Bus; `version:` obsoleto; healthcheck do frontend usa `wget` sem garantia | 11 |
| `.env` | Não há `.env.example`; `.gitignore` usa `*.env` | 11 |

### Documentação e testes

| Existe | Problema | Resolvido em |
|--------|----------|--------------|
| `README.md` | Genérico; cita caminhos inexistentes (`./src/Api/Api.csproj`) e variáveis que não são usadas | 13 |
| — | **Nenhum teste automatizado** | 12 |
| — | Sem diagrama de arquitetura | 13 |
