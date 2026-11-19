# 14 — CI, README em inglês e builds reproduzíveis

- **Status:** Em verificação
- **Pontuação:** Documentação & DX 15%, Testes Automatizados 10% (apresentação do projeto)

## Contexto

O projeto roda e tem 52 testes, mas nada os executa automaticamente: quem
visita o repositório não tem como saber se a `main` está verde. O README só
existe em português, e o emulador do Service Bus usa a tag `latest`, o que pode
quebrar o `docker compose up` quando a Microsoft publicar uma versão nova.

## Requisitos

- Testes automatizados visíveis — critérios de avaliação (Testes Automatizados 10%).
- `README.md` com instruções claras — desafio *Entrega*.
- Docker Compose reproduzível — desafio §3.

## Critérios de aceite

1. **Dado** um push ou pull request para `main`, **então** o GitHub Actions roda: build e testes .NET (incluindo os de integração com Testcontainers), lint e build do frontend, e o build das imagens Docker.
2. Qualquer falha em um desses passos deixa o workflow vermelho.
3. O README (pt-BR e inglês) mostra o badge do workflow.
4. `README.en.md` tem o mesmo conteúdo do `README.md`, em inglês, e os dois linkam um para o outro no topo.
5. O emulador do Service Bus usa uma versão fixa (`2.0.1`, a mesma imagem validada em 19/11/2026).
6. O histórico do git tem commits organizados por camada/spec, em Conventional Commits, cada um compilando sozinho.

## Design

- `.github/workflows/ci.yml` com três jobs independentes:
  - `backend` — `ubuntu-latest`, .NET 8: `restore`, `build -c Release`, `test` (o runner tem Docker, então o Testcontainers funciona).
  - `frontend` — Node 22: `npm ci`, `npm run lint`, `npm run build`.
  - `docker` — `cp .env.example .env`, `docker compose config` e `docker compose build`.
- `concurrency` cancela execuções antigas do mesmo branch; resultados de teste (`.trx`) publicados como artefato.
- `docker-compose.yml`: `mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1`.
- Commits: como várias specs tocam os mesmos arquivos (ex.: `Program.cs` da API), os commits são agrupados por camada, cada um citando as specs que implementa, para que todo commit compile.

## Tasks

- [x] 14.1 Workflow `.github/workflows/ci.yml`.
- [x] 14.2 Validar o build e os testes em Linux (o runner do CI) antes do primeiro push.
- [x] 14.3 Fixar o emulador em `2.0.1`.
- [x] 14.4 `README.en.md` + seletor de idioma e badge nos dois READMEs.
- [x] 14.5 Commits organizados por camada/spec.

## Verificação

- Critérios 1–2: primeiro push do branch → aba *Actions* do GitHub.
- Critério 5: `docker compose config | grep servicebus-emulator`.
- Critério 6: `git log --oneline` e `git rebase -x "dotnet build Orders.sln" <base>` sem falhas.

## Notas de implementação

- Validado localmente em Linux (container `mcr.microsoft.com/dotnet/sdk:8.0` com acesso ao Docker): restore, build Release sem avisos e os 52 testes passando, inclusive Testcontainers.
- Emulador `2.0.1` conferido: é a mesma imagem (digest `sha256:5a96d893…`) que a `latest` validada; após a troca, `/health/ready` saudável.
- **Pendente:** o primeiro run no GitHub Actions (critérios 1–3) só acontece depois do push do branch. Os commits ficaram agrupados por camada citando as specs; cada um compila sozinho.
