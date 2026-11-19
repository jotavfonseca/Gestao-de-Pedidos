# Gestão de Pedidos — contexto para o Claude

Este projeto é a solução do desafio técnico **POC TMB**. A especificação completa
(requisitos, regras de negócio, bônus e critérios de avaliação) está em:

@docs/desafio-poc-tmb.md

Ao fazer alterações, siga os requisitos desse documento (stack obrigatória:
.NET + EF + PostgreSQL, React + TailwindCSS, Azure Service Bus, Docker Compose).

Estrutura: `api/` (API REST em C#), `worker/` (consumidor do Service Bus),
`shared/` (domínio, DbContext, migrations e contratos compartilhados),
`tests/` (testes automatizados), `frontend/` (React), `infra/` (config do emulador
do Service Bus e do pgAdmin), `specs/` (especificações),
`docker-compose.yml` na raiz.

## Spec Driven Development

O projeto segue SDD. Antes de alterar código:

1. Leia `specs/constitution.md` (princípios que valem para tudo) e a spec da
   feature envolvida em `specs/NN-*.md`. O índice e o status estão em `specs/README.md`.
2. Implemente para cumprir os **critérios de aceite** da spec, task por task.
3. Ao concluir, marque as tasks `[x]`, atualize o **Status** da spec e a tabela
   de `specs/README.md`.
4. Se o escopo mudar, atualize a spec **antes** do código. Mudança sem spec
   correspondente: crie a spec primeiro.
