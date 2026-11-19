# Constituição do projeto

Princípios que valem para **todas** as features. Uma spec não pode contrariá-los;
se for preciso, altere este arquivo primeiro, de forma explícita.

## 1. Stack obrigatória (desafio)

| Camada | Tecnologia |
|--------|-----------|
| Backend | C# / .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL |
| Mensageria | Azure Service Bus (emulador oficial localmente; namespace real via connection string) |
| Frontend | React + TailwindCSS (Vite) |
| Infra | Docker / Docker Compose |
| IA (opcional) | Claude via SDK oficial `Anthropic` para C# |

## 2. Regras de negócio inegociáveis

1. Pedido tem `id`, `cliente`, `produto`, `valor`, `status`, `dataCriacao`.
2. Status: `Pendente`, `Processando`, `Finalizado` — **somente** nessa sequência.
3. Criar pedido = persistir no PostgreSQL via EF **e** publicar no Service Bus.
4. Mensagens levam `CorrelationId = OrderId` e `EventType = OrderCreated`.
5. O worker é **idempotente**: reprocessar a mesma mensagem nunca gera transição inválida nem histórico duplicado.
6. Health checks cobrem API, banco e fila.

## 3. Estrutura do repositório

```
api/Orders.Api/          API REST, outbox publisher, SignalR, módulo IA
worker/Orders.Worker/    consumidor do Service Bus
shared/Orders.Core/      Domain/ (agregado, value objects, eventos), Application/ (casos de uso),
                         Infrastructure/ (DbContext, migrations, outbox), Contracts/ (mensagens)
tests/Orders.Tests/      unitários, golden, integração (Testcontainers)
frontend/                React + Tailwind
specs/                   especificações (SDD)
docs/                    enunciado do desafio
```

## 4. Arquitetura: DDD tático, sem cerimônia estratégica

O domínio é pequeno (um agregado, três status, uma regra central), então
aplicamos **só os padrões táticos de DDD que resolvem problemas reais**:

| Usamos | Para quê |
|--------|----------|
| **Agregado rico** (`Order`) | Invariantes protegidas no próprio modelo: setters privados, factory `Order.Create`, mudança de status só por `TransitionTo` |
| **Value object** (`Money`) | Valor monetário sempre positivo e com 2 casas, garantido no domínio e não só no DTO |
| **Eventos de domínio** | O agregado registra o que aconteceu (`OrderCreated`, `OrderStatusChanged`); a infraestrutura converte em mensagens de outbox no `SaveChanges` |
| **Camada de aplicação fina** | Casos de uso (criar, consultar, processar) fora dos controllers e do host do worker |
| **Exceções de domínio** | Violação de regra é erro de domínio, mapeado para HTTP na borda |
| **Linguagem ubíqua** | Termos do negócio do desafio (`Cliente`, `Produto`, `Valor`, `Pendente`…) |

Deliberadamente **não** usamos:

- **Repositórios genéricos sobre o EF** — o `DbContext` já é unidade de trabalho e repositório.
- **MediatR / CQRS formal** — três casos de uso não justificam o pipeline.
- **Um projeto por camada** — `Orders.Core` separa `Domain/` (sem dependência de EF ou Service Bus) e `Infrastructure/` por pasta; a regra de dependência é verificada por teste (spec 12).
- **Bounded contexts / context map** — existe um contexto só.

Se o domínio crescer (pagamentos, estoque, clientes), essas decisões devem ser revistas aqui.

## 5. Convenções de código

- **Domínio protege as regras**: transições de status só via `Order.TransitionTo`; nada de `order.Status = ...` fora do domínio.
- **Domínio sem infraestrutura**: nada em `Orders.Core/Domain` referencia EF Core, Npgsql ou Azure SDK.
- **Contratos explícitos**: a API nunca expõe nem recebe entidades EF diretamente — usa DTOs.
- **JSON em camelCase**, enums como texto.
- **Configuração por ambiente**: segredos e endereços vêm de variáveis de ambiente (`.env`); nada sensível versionado.
- **Sem código morto**: o que foi substituído é removido, não comentado.
- Nomes de domínio em português (como no desafio); código, infraestrutura e termos técnicos em inglês.
- Logs estruturados (`ILogger` com templates), sempre com `OrderId` quando houver.

## 6. Convenções de commit

- Conventional Commits em português: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- Um commit por feature/task coerente, referenciando a spec (ex.: `feat(worker): consumidor idempotente [spec 04]`).
- Nada direto na `main` sem revisão; trabalhar em branch.

## 7. Definição de pronto

Uma feature está **Concluída** quando:

1. Todas as tasks da spec estão `[x]`.
2. Todos os critérios de aceite foram verificados (teste automatizado ou passo manual descrito na spec).
3. `dotnet build` sem erros e `dotnet test` verde; no frontend, `npm run lint` e `npm run build` limpos.
4. A spec e a tabela de [`README.md`](README.md) estão atualizadas.
5. Se mudou como rodar ou configurar algo, o `README.md` da raiz e o `.env.example` foram atualizados.
