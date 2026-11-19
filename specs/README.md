# Specs — Gestão de Pedidos

Este projeto segue **Spec Driven Development (SDD)**: toda mudança começa por uma
especificação aqui em `specs/`, e o código é escrito para cumprir os critérios de
aceite dela.

- [`constitution.md`](constitution.md) — princípios fixos (stack, regras do desafio, arquitetura DDD tática, convenções, definição de pronto).
- [`current-state.md`](current-state.md) — diagnóstico do projeto antes desta rodada de specs.
- Requisitos originais: [`../docs/desafio-poc-tmb.md`](../docs/desafio-poc-tmb.md).

## Features

| # | Feature | Tipo | Pontos | Status |
|---|---------|------|--------|--------|
| 01 | [Base compartilhada e domínio (DDD tático)](01-base-compartilhada.md) | Fundação | — | Concluída |
| 02 | [API REST](02-api-rest.md) | Obrigatória | — | Concluída |
| 03 | [Outbox + Service Bus](03-outbox-service-bus.md) | Obrigatória + bônus | +3 | Concluída |
| 04 | [Worker idempotente](04-worker-idempotente.md) | Obrigatória | — | Concluída |
| 05 | [Health checks](05-health-checks.md) | Obrigatória | — | Concluída |
| 06 | [Tempo real com SignalR + fallback](06-signalr-fallback.md) | Bônus | +3 | Concluída |
| 07 | [Histórico de status](07-historico-status.md) | Bônus | +3 | Concluída |
| 08 | [Tracing ponta a ponta](08-tracing.md) | Bônus | +2 | Concluída |
| 09 | [Frontend (React + Tailwind)](09-frontend.md) | Obrigatória | — | Concluída |
| 10 | [Módulo IA — Pergunte sobre os pedidos](10-modulo-ia.md) | Opcional / bônus | +5 | Em verificação |
| 11 | [Infraestrutura (Docker Compose)](11-infraestrutura.md) | Obrigatória | — | Concluída |
| 12 | [Testes automatizados](12-testes.md) | Bônus | +3 | Concluída |
| 13 | [Documentação e DX](13-documentacao.md) | Obrigatória | — | Concluída |
| 14 | [CI, README em inglês e builds reproduzíveis](14-ci-e-portfolio.md) | DX / apresentação | — | Em verificação |

Status possíveis: `Planejada` → `Em andamento` → `Em verificação` (implementada, falta verificar algum critério) → `Concluída`.

## Ordem de execução

01 → 02 → 03 → 04 → 05 → 07 → 06 → 08 → 10 → 09 → 11 → 12 → 13

O backend compila e é testado antes do frontend; o Compose vem por último para
validar tudo junto.

## Fluxo de trabalho

1. **Especificar** — escrever ou ajustar a spec da feature (requisitos, critérios de aceite, design, tasks).
2. **Implementar** — escrever o código para cumprir os critérios de aceite, uma task por vez.
3. **Verificar** — rodar a seção *Verificação* da spec.
4. **Atualizar** — marcar as tasks `[x]`, mudar o status na spec e nesta tabela.

Mudou o escopo durante a implementação? Atualize a spec **antes** do código.

## Modelo de spec

Cada `NN-slug.md` tem as seções:

- **Status** e **Pontuação**
- **Contexto** — o que existe hoje e por que mudar
- **Requisitos** — com referência à seção do desafio
- **Critérios de aceite** — Dado / Quando / Então
- **Design** — arquivos, componentes e decisões
- **Tasks** — checklist `- [ ] N.M`
- **Verificação** — como provar cada critério
