# Gestão de Pedidos

> Repositório exemplo de um sistema de gerenciamento de pedidos composto por:
>
> * **API** em ASP.NET Core
> * **Worker** (serviço de background) em .NET
> * **Banco de dados** PostgreSQL
> * **pgAdmin** para administração do banco
> * **Frontend** em React

---

## Índice

* [Sobre](#sobre)
* [Pré-requisitos](#pré-requisitos)
* [Variáveis de ambiente (`.env`)](#variáveis-de-ambiente-env)
* [Rodando com Docker Compose (recomendado)](#rodando-com-docker-compose-recomendado)
* [Rodando localmente (sem Docker)](#rodando-localmente-sem-docker)
* [Migrações e banco de dados](#migrações-e-banco-de-dados)
* [Acessando o pgAdmin](#acessando-o-pgadmin)
* [Testando a API](#testando-a-api)
* [Comandos úteis](#comandos-úteis)
* [Solução de problemas comum](#solução-de-problemas-comum)
* [Contribuindo](#contribuindo)

---

## Sobre

Este projeto implementa um fluxo simples de gestão de pedidos: backend em ASP.NET Core, um worker para processamento assíncrono, banco PostgreSQL e um frontend em React. A forma mais simples de rodar tudo é com Docker Compose.

---

## Pré-requisitos

* Docker e Docker Compose instalados (versões recentes recomendadas)
* Caso rode localmente sem Docker: .NET SDK (6/7+ compatível com o projeto) e Node.js (16+ recomendado)

---

## Variáveis de ambiente (`.env`)

Coloque um arquivo `.env` na raiz do repositório com as variáveis abaixo (você já nos passou este arquivo — está pronto):

```env
# Postgres
POSTGRES_DB=ordersdb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# API / Worker (variáveis consumidas pelos containers .NET)
# usaremos esta variável no docker-compose para passar para o ASP.NET
CONNECTION_STR=Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
ASPNETCORE_ENVIRONMENT=Development

# PGADMIN
PGADMIN_DEFAULT_EMAIL=admin@admin.local
PGADMIN_DEFAULT_PASSWORD=admin

# FRONTEND (quando rodar em container, apontar para o serviço `api`)
REACT_APP_API_URL=http://api:80
```

> **Observação:** a `CONNECTION_STR` acima usa o hostname `postgres` — isto assume que o serviço Postgres no `docker-compose.yml` chama-se `postgres` (padrão comum). Se o `docker-compose.yml` usar nomes ou portas diferentes, ajuste conforme necessário.

---

## Rodando com Docker Compose (recomendado)

1. Garanta que o arquivo `.env` está na raiz do repositório.

2. Suba os serviços:

```bash
# no diretório raiz do repositório
docker compose up --build -d
```

3. Verifique o status:

```bash
docker compose ps
```

4. Acompanhe logs (ex.: API):

```bash
docker compose logs -f api
```

5. Se precisar parar e remover containers (mantendo volumes):

```bash
docker compose down
```

6. Para parar e remover volumes (dados do DB):

```bash
docker compose down -v
```

**Observações sobre portas:**

* O `docker-compose.yml` do projeto define quais portas ficam expostas localmente. Verifique esse arquivo para confirmar as portas do `api`, `frontend` e `pgadmin`. Caso o docker-compose use as convenções típicas, você poderá acessar `http://localhost:3000` (frontend), `http://localhost` (API em :80) e `http://localhost:8080` (pgAdmin). Se não tiver certeza, rode `docker compose ps` para ver as portas mapeadas.

---

## Rodando localmente (sem Docker)

> Use esta seção quando quiser executar apenas a API ou o frontend no seu ambiente de desenvolvimento.

### API / Worker (.NET)

1. Abra um terminal e navegue até a pasta da API (ex.: `src/Api` ou `Api` — verifique a estrutura do repositório).
2. Exporte a variável de conexão localmente (bash/powershell) ou crie um arquivo `appsettings.Development.json` com a connection string:

Em Linux/macOS (bash):

```bash
export CONNECTION_STR='Host=localhost;Database=ordersdb;Username=postgres;Password=postgres;Port=5432'
export ASPNETCORE_ENVIRONMENT=Development
```

No Windows PowerShell:

```powershell
$env:CONNECTION_STR = 'Host=localhost;Database=ordersdb;Username=postgres;Password=postgres;Port=5432'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
```

3. Restaurar e executar:

```bash
dotnet restore
dotnet build
dotnet run --project ./path/para/projeto.Api.csproj
```

O Worker (serviço de background) pode ser executado de forma semelhante, apontando para o projeto do worker:

```bash
dotnet run --project ./path/para/worker.csproj
```

### Frontend (React)

1. Abra um terminal em `frontend` (ou pasta equivalente).
2. Configure a variável `REACT_APP_API_URL` (ex.: `http://localhost:5000` ou para o endereço onde a API estiver rodando).
3. Instale dependências e rode:

```bash
npm install
npm start
```

O `npm start` geralmente abre o app em `http://localhost:3000`.

---

## Migrações e banco de dados

Se o projeto usa Entity Framework Core e houver migrations no repositório, você pode aplicar as migrations diretamente no container da API (após o `docker compose up`):

```bash
docker compose exec api dotnet ef database update
```

> Se o container `api` não tiver as ferramentas EF CLI, você pode executar as migrations localmente (com .NET SDK instalado) ou adicionar as ferramentas ao container (ajuste do Dockerfile).

Alguns projetos aplicam migrations automaticamente na inicialização. Verifique os logs da API para confirmar se as migrations foram aplicadas.

---

## Acessando o pgAdmin

1. Abra o endereço configurado no `docker-compose.yml` (ex.: `http://localhost:8080`).
2. Login: use `PGADMIN_DEFAULT_EMAIL` e `PGADMIN_DEFAULT_PASSWORD` do `.env`.
3. Adicione um servidor no pgAdmin com os dados do Postgres:

   * Host: `postgres` (quando estiver rodando via Docker Compose)
   * Port: `5432` (ou conforme `POSTGRES_PORT`)
   * Username / Password: do `.env` (ex.: `postgres`/`postgres`)

---

## Testando a API

* Verifique se há Swagger disponível (API em ambiente Development costuma expor):

```
http://<host_da_api>/swagger
```

* Exemplos de `curl` (ajuste as rotas conforme o projeto):

```bash
# listar pedidos (exemplo)
curl http://localhost/api/orders

# criar pedido (exemplo)
curl -X POST http://localhost/api/orders -H "Content-Type: application/json" -d '{"customer":"João","items":[]}'
```

> Substitua as rotas acima pelas rotas reais do projeto. Consulte o controller ou o Swagger para ver os endpoints disponíveis.

---

## Comandos úteis

```bash
# rebuild de imagens e subir em background
docker compose up --build -d

# logs em tempo real do serviço "api"
docker compose logs -f api

# executar migration dentro do container
docker compose exec api dotnet ef database update

# parar e remover containers + volumes de banco
docker compose down -v
```

---

## Solução de problemas comum

* **API não conecta ao Postgres**: provavelmente o Postgres ainda está inicializando. Verifique `docker compose logs postgres` e confirme as variáveis do `.env`. Também verifique se o `CONNECTION_STR` usa o host correto (`postgres` quando em Compose).
* **Ports em uso**: verifique `docker compose ps` e ajuste mapeamentos de portas no `docker-compose.yml` se necessário.
* **Migrations não aplicadas**: rode `dotnet ef database update` no container ou localmente.

---

## Contribuindo

Fique à vontade para abrir issues e pull requests. Para mudanças rápidas, descreva o que foi alterado e como testar localmente.

---

## Licença

Adicione aqui a licença do projeto (se houver).

---

> Se quiser, eu posso atualizar este README para incluir portas exatas, nomes de projetos e comandos precisos de `dotnet` com base no `docker-compose.yml` e na estrutura de pastas do seu repositório — cole aqui o `docker-compose.yml` ou autorize que eu o acesse.
