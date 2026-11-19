# 02 — API REST

- **Status:** Concluída
- **Pontuação:** obrigatória (Funcionalidade 15%, Qualidade do Código 30%)

## Contexto

Os três endpoints existem, mas o POST recebe a entidade EF inteira (o cliente
pode enviar `status` ou `historico`), o enum sai como número, erros viram
`Exception` genérica, e as migrations rodam sem retry (a API cai se o banco
demorar a subir).

## Requisitos

- Endpoints `POST /orders`, `GET /orders`, `GET /orders/{id}` — desafio §1.
- Persistir com EF no PostgreSQL — desafio §1 *Regras de Negócio*.
- Migrations automáticas — desafio §3.

## Critérios de aceite

1. **Dado** `{ "cliente": "Ana", "produto": "Teclado", "valor": 199.9 }`, **quando** `POST /orders`, **então** responde `201 Created`, header `Location: /orders/{id}` e corpo com `status: "Pendente"`.
2. **Dado** um corpo com `status: "Finalizado"`, **quando** `POST /orders`, **então** o campo é ignorado e o pedido nasce `Pendente`.
3. **Dado** `cliente` vazio, `produto` com mais de 100 caracteres ou `valor <= 0`, **quando** `POST /orders`, **então** `400` com `ValidationProblemDetails` indicando o campo.
4. **Dado** pedidos existentes, **quando** `GET /orders`, **então** `200` com lista ordenada por `dataCriacao` decrescente.
5. **Dado** um id existente, **quando** `GET /orders/{id}`, **então** `200` com `historico` ordenado por data.
6. **Dado** um id inexistente (ou não-GUID), **quando** `GET /orders/{id}`, **então** `404` com `ProblemDetails`.
7. **Dado** um erro inesperado, **então** `500` com `ProblemDetails`, sem stack trace fora de Development.
7a. **Dado** uma regra de domínio violada que passou pela validação do DTO (ex.: `DomainValidationException`), **então** `400` com `ProblemDetails`; `InvalidStatusTransitionException` vira `409 Conflict`.
7b. O controller não contém regra de negócio nem acesso direto ao `DbContext`: só traduz HTTP ↔ caso de uso.
8. **Dado** o Postgres ainda iniciando, **quando** a API sobe, **então** ela tenta aplicar as migrations com retry e só aceita requisições depois de aplicá-las.

## Design

```
shared/Orders.Core/Application/Orders/
  CreateOrder.cs                    # CreateOrderCommand + CreateOrderHandler: Money.Of, Order.Create, SaveChanges
  OrderQueries.cs                   # GetById, List (AsNoTracking, projeção para OrderDto)
  OrderDto.cs                       # visão de leitura usada pela API e pelo worker/testes
  ServiceCollectionExtensions.cs    # AddOrdersApplication()

api/Orders.Api/
  Contracts/CreateOrderRequest.cs   # DataAnnotations: Required, MaxLength(100), Range(0.01, ...)
  Contracts/OrderResponse.cs        # id, cliente, produto, valor, status, dataCriacao, historico[]
  Controllers/OrdersController.cs   # substitui OrdersControllers.cs; só HTTP ↔ caso de uso
  Infrastructure/DomainExceptionHandler.cs  # IExceptionHandler: exceções de domínio → 400/409
  Infrastructure/DatabaseMigrator.cs
  Program.cs
  Orders.Api.http
```

Camada de aplicação (DDD tático, sem MediatR): classes simples registradas na DI
e injetadas no controller. A validação acontece em duas camadas com papéis
diferentes: o DTO dá feedback amigável por campo (400 com `ValidationProblemDetails`);
o domínio garante a invariante independentemente de quem chame.

Formato de resposta:

```json
{
  "id": "4f0c…",
  "cliente": "Ana",
  "produto": "Teclado",
  "valor": 199.90,
  "status": "Pendente",
  "dataCriacao": "2026-11-19T13:00:00+00:00",
  "historico": [{ "status": "Pendente", "data": "2026-11-19T13:00:00+00:00" }]
}
```

Decisões:
- Nome do atributo no JSON é `dataCriacao` (camelCase), equivalente ao `data_criacao` do enunciado; documentado no README.
- `GET /orders` também retorna `historico` (lista pequena; evita N+1 no frontend). Paginação fica fora do escopo.
- `JsonStringEnumConverter` global.
- `AddProblemDetails()` + `UseExceptionHandler()`; o controller não captura exceções genéricas.
- Retry de migration: até 10 tentativas, espera crescente, log a cada falha.
- CORS liberado só em Development (em Docker o frontend acessa via proxy do nginx — spec 09).

## Tasks

- [x] 2.1 DTOs `CreateOrderRequest` e `OrderResponse` (+ mapeamento a partir de `OrderDto`).
- [x] 2.2 Casos de uso `CreateOrderHandler` e `OrderQueries` em `Orders.Core/Application`.
- [x] 2.3 Reescrever `OrdersController` (renomear o arquivo) delegando aos casos de uso.
- [x] 2.4 `JsonStringEnumConverter`, `ProblemDetails`, `DomainExceptionHandler`, remoção do `throw new Exception`.
- [x] 2.5 `DatabaseMigrator` com retry, chamado antes de `app.Run()`.
- [x] 2.6 `Orders.Api.http` com exemplos reais (criar, listar, detalhar, inválido, 404).

## Verificação

- Critérios 1–7a: testes de integração da API (spec 12, `WebApplicationFactory` + Testcontainers).
- Critério 8: subir com `docker compose up` e conferir no log da API as tentativas até o Postgres ficar pronto.

## Notas de implementação

- **CORS removido**: o navegador sempre fala com a mesma origem (proxy do nginx no Docker e do Vite em dev), então CORS não é necessário.
- `[Produces("application/json")]` foi retirado dos controllers: ele forçava `application/json` também nos erros, que devem sair como `application/problem+json`.
- `[Range]` com `ParseLimitsInInvariantCulture`: em máquinas pt-BR o limite `"0.01"` não era lido.
- Verificado: critérios 1–7 por `OrdersApiTests` e manualmente com `curl`; critério 8 nos logs do Compose.
