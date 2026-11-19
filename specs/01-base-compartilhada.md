# 01 — Base compartilhada e domínio (DDD tático)

- **Status:** Concluída
- **Pontuação:** fundação (habilita 02, 03, 04, 07); Qualidade do Código 30%

## Contexto

Domínio e `DbContext` vivem dentro do projeto Web da API, e o worker referencia
esse projeto inteiro. O worker usa `_context.Historico`, que não existe, e por
isso não compila. Qualquer código pode mudar `Status` livremente, então a
sequência obrigatória não é garantida em lugar nenhum. Seguimos o DDD tático
definido em [`constitution.md` §4](constitution.md#4-arquitetura-ddd-tático-sem-cerimônia-estratégica).

## Requisitos

- Atributos do pedido e status — desafio §1 *Regras de Negócio*.
- Sequência obrigatória Pendente → Processando → Finalizado — desafio §1 *Regras adicionais*.
- EF + PostgreSQL — desafio *Tecnologias Obrigatórias*.

## Critérios de aceite

Agregado:
1. **Dado** um pedido novo, **quando** `Order.Create`, **então** está `Pendente`, o histórico tem 1 entrada `Pendente` e há 1 evento de domínio `OrderCreated`.
2. **Dado** um pedido `Pendente`, **quando** `TransitionTo(Processando)`, **então** o status muda, o histórico ganha 1 entrada e é registrado 1 evento `OrderStatusChanged`.
3. **Dado** um pedido `Pendente`, **quando** `TransitionTo(Finalizado)`, **então** é lançada `InvalidStatusTransitionException` e nada muda (nem histórico, nem eventos).
4. **Dado** um pedido `Finalizado`, **quando** qualquer `TransitionTo`, **então** é lançada a exceção.
5. **Dado** `cliente` ou `produto` vazio ou com mais de 100 caracteres, **quando** `Order.Create`, **então** é lançada `DomainValidationException` com o campo.

Value object:
6. **Dado** valor `≤ 0`, **quando** `Money.Of(valor)`, **então** `DomainValidationException`.
7. **Dado** `Money.Of(10.005m)`, **então** o valor é arredondado para 2 casas (`10.01`, arredondamento bancário desativado — `MidpointRounding.AwayFromZero`).
8. Dois `Money` com o mesmo valor são iguais (igualdade por valor).

Persistência e eventos:
9. **Dado** o banco migrado, **então** existem `Orders`, `OrderStatusHistory` e `OutboxMessages`, com `Status` como texto e `Valor` como `numeric(18,2)`.
10. **Dado** um agregado com eventos pendentes, **quando** `SaveChangesAsync`, **então** cada evento vira um `OutboxMessage` **na mesma transação** e a lista de eventos do agregado é limpa.
11. **Dado** duas atualizações concorrentes do mesmo pedido, **quando** ambas salvam, **então** a segunda recebe `DbUpdateConcurrencyException`.

Arquitetura:
12. Nada em `Orders.Core.Domain` referencia EF Core, Npgsql ou Azure SDK (verificado por teste — spec 12).
13. Worker e API referenciam apenas `Orders.Core` (o worker não referencia a API).

## Design

Novo projeto `shared/Orders.Core` (classlib net8.0), separado por pasta:

```
Domain/                               # sem dependências de infraestrutura
  Abstractions/
    AggregateRoot.cs                  # Id + lista de IDomainEvent (Raise, ClearDomainEvents)
    IDomainEvent.cs                   # OccurredAt
    DomainException.cs                # base
  Orders/
    Order.cs                          # Create(cliente, produto, Money, agora); TransitionTo(status, agora)
    OrderStatus.cs                    # Pendente, Processando, Finalizado
    OrderStatusHistory.cs             # entidade filha do agregado
    Money.cs                          # value object (record), Of(decimal)
    Events/OrderCreated.cs            # OrderId, Cliente, Produto, Valor, DataCriacao
    Events/OrderStatusChanged.cs      # OrderId, De, Para, OccurredAt
    InvalidStatusTransitionException.cs
    DomainValidationException.cs
Application/                          # casos de uso (spec 02 e 04)
Contracts/                            # mensagens que trafegam no Service Bus
  OrderCreatedMessage.cs
  OrderStatusChangedMessage.cs
  MessagingConstants.cs               # filas e EventType
Infrastructure/
  Persistence/
    OrderContext.cs
    Configurations/OrderConfiguration.cs, OutboxMessageConfiguration.cs
    DesignTimeOrderContextFactory.cs
    Migrations/
  Outbox/
    OutboxMessage.cs                  # Id, Type, Destination, Payload, CorrelationId, TraceParent, CreatedAt, ProcessedAt, Attempts, LastError
    DomainEventsToOutboxInterceptor.cs  # SaveChangesInterceptor
    DomainEventMapper.cs              # evento de domínio → (fila, EventType, contrato)
Observability/
  Telemetry.cs                        # ActivitySource "Orders.Messaging" (spec 08)
```

Decisões:
- **Agregado `Order`**: setters privados; construtor privado para o EF; `Historico` exposto como `IReadOnlyCollection`. Mudança de status só por `TransitionTo`, que valida a sequência, grava o histórico e registra o evento.
- **`Money`** mapeado como *complex property* / conversão de valor para a coluna `Valor` (`numeric(18,2)`); no JSON da API continua número (`valor`).
- **Eventos de domínio × mensagens**: eventos de domínio são internos (`Domain/Orders/Events`); o que sai para a fila são **contratos** estáveis (`Contracts/`). O `DomainEventMapper` faz a tradução — o domínio pode evoluir sem quebrar consumidores.
- **Interceptor** (`SaveChangesInterceptor`): antes de salvar, coleta eventos dos agregados rastreados, cria os `OutboxMessage` e limpa os eventos. Mesma transação, sem código de mensageria nos casos de uso.
- **Relógio injetável**: métodos recebem `DateTimeOffset agora`; os casos de uso usam `TimeProvider` (testes determinísticos).
- **Concorrência otimista** com a coluna `xmin` do PostgreSQL.
- Status como `string` (`HasConversion<string>()`).
- Migrations antigas da API são **descartadas** e a `InitialCreate` é regenerada (banco descartável nesta fase).
- Nomes de classes e eventos em inglês (alinhados ao `EventType = OrderCreated` exigido pelo desafio); atributos de negócio em português.

## Tasks

- [x] 1.1 Criar `Orders.sln` na raiz com API, worker, core e testes.
- [x] 1.2 Criar `shared/Orders.Core` com as pastas `Domain/`, `Application/`, `Contracts/`, `Infrastructure/`; remover `Models/`, `Data/` e `Migrations/` da API.
- [x] 1.3 `AggregateRoot`, `IDomainEvent`, exceções de domínio.
- [x] 1.4 Value object `Money`.
- [x] 1.5 Agregado `Order` com `Create`, `TransitionTo`, histórico e eventos `OrderCreated` / `OrderStatusChanged`.
- [x] 1.6 `OrderContext` + configurações Fluent; status como texto; `Money` mapeado; `xmin` como concurrency token.
- [x] 1.7 `OutboxMessage` (com `Destination`) e índice em `(ProcessedAt, CreatedAt)`.
- [x] 1.8 Contratos de mensagem e `MessagingConstants`.
- [x] 1.9 `DomainEventMapper` e `DomainEventsToOutboxInterceptor`.
- [x] 1.10 Design-time factory e migration `InitialCreate` regenerada.

## Verificação

- `dotnet build Orders.sln` sem erros.
- Critérios 1–8: testes unitários de domínio (spec 12).
- Critérios 9–11: testes de integração com Testcontainers (spec 12).
- Critério 12: teste de arquitetura por reflexão (spec 12).
- Critério 13: `worker/Orders.Worker/Orders.Worker.csproj` só tem `ProjectReference` para `Orders.Core`.

## Notas de implementação

- `Money` foi mapeado como **complex property** do EF 8 (coluna `Valor`), não como conversão de valor: assim `o.Valor.Amount` é traduzido para SQL (somas do módulo IA rodam no banco).
- Versões: `Microsoft.Extensions.*` 10.0 (suportam `net8.0`) para usar o OpenTelemetry 1.15.3, que corrige o GHSA-4625-4j76-fww9. Versões centralizadas em `Directory.Packages.props`.
- `dotnet-ef` 8.0.11 fixado em `.config/dotnet-tools.json`.
- Verificado: critérios 1–8 por `OrderTests`/`MoneyTests`; 9–11 por `PersistenceTests` (Testcontainers); 12 por `LayeringTests`.
