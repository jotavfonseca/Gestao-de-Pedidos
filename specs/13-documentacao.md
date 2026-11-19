# 13 — Documentação e DX

- **Status:** Concluída
- **Pontuação:** obrigatória (Documentação & DX 15%) + diagramas (diferencial de entrega)

## Contexto

O README atual é genérico, cita caminhos que não existem (`./src/Api/Api.csproj`)
e variáveis que não são usadas, e não tem diagrama.

## Requisitos

- `README.md` com instruções claras para rodar — desafio *Entrega*.
- Exemplo de `.env` — desafio *Entrega* (spec 11).
- Diagramas simples de arquitetura — desafio *Entrega* (diferencial).

## Critérios de aceite

1. Alguém sem contexto consegue rodar o projeto seguindo **só** o README, com 2 comandos após o clone.
2. O README tem diagrama de arquitetura e diagrama de sequência do fluxo de um pedido (Mermaid, renderizado pelo GitHub).
3. Tabela de URLs: frontend, Swagger, health, pgAdmin, Jaeger.
4. Seção de variáveis de ambiente em sintonia com o `.env.example`.
5. Seções: rodar sem Docker, rodar os testes, decisões de design (outbox, idempotência, emulador, SignalR, segurança do módulo IA), mapeamento requisito do desafio → onde foi implementado, troubleshooting.
5a. Seção **"Arquitetura e DDD"**: quais padrões táticos foram usados (agregado, value object, eventos de domínio, camada de aplicação), quais foram deliberadamente deixados de fora (repositórios genéricos, MediatR/CQRS, projeto por camada, bounded contexts) e por quê — resumo de [`constitution.md` §4](constitution.md#4-arquitetura-ddd-tático-sem-cerimônia-estratégica). Inclui o fluxo *agregado → evento de domínio → outbox → Service Bus*.
6. Todos os comandos do README foram executados e funcionam.
7. `CLAUDE.md` descreve a estrutura atual e o fluxo SDD com `specs/`.

## Design

Estrutura do README:

1. Visão geral e funcionalidades
2. Arquitetura (diagrama de componentes + sequência)
3. Como rodar (Docker) — 2 comandos
4. URLs
5. Variáveis de ambiente
6. API (endpoints, exemplos `curl`, formato JSON)
7. Rodar sem Docker
8. Testes
9. Arquitetura e DDD (padrões usados e não usados) + demais decisões de design
10. Requisitos do desafio × implementação (com link para as specs)
11. Troubleshooting
12. Estrutura de pastas

## Tasks

- [x] 13.1 Reescrever `README.md` conforme a estrutura acima.
- [x] 13.2 Atualizar `CLAUDE.md` com `shared/`, `tests/` e o fluxo SDD de `specs/`.
- [x] 13.3 README aponta para `specs/` como fonte das decisões.
- [x] 13.5 Seção "Arquitetura e DDD" com diagrama do fluxo agregado → evento → outbox → fila.
- [x] 13.4 Remover o `frontend/README.md` do template Vite.

## Verificação

- Executar cada comando do README num clone limpo.
- Visualizar o README no GitHub (ou num visualizador Mermaid) para conferir os diagramas.

## Notas de implementação

- Executados: `cp .env.example .env`, `docker compose up --build -d`, `docker compose ps/logs`, `curl` dos exemplos, `dotnet test`, `npm run lint/build`, `dotnet run --project worker/Orders.Worker`.
- Não executados nesta rodada: `dotnet run --project api/Orders.Api` fora do Docker (porta 5000 ocupada pelo container), `npm run dev` e a renderização dos diagramas Mermaid no GitHub.
