# Gestão de Pedidos

Sistema de exemplo para gerenciamento de pedidos composto por:

* **API** em ASP.NET Core (.NET 8.0)
* **Worker** (serviço em background) em .NET 8.0
* **Banco de dados** PostgreSQL
* **pgAdmin** para administração do banco
* **Frontend** em React (Node 22)

---

## Índice

* [Sobre](#sobre)
* [Pré-requisitos](#pré-requisitos)
* [Variáveis de ambiente (`.env`)](#variáveis-de-ambiente-env)
* [Rodando com Docker Compose](#rodando-com-docker-compose)
* [Rodando localmente (sem Docker)](#rodando-localmente-sem-docker)
* [Migrações e banco de dados](#migrações-e-banco-de-dados)
* [Acessando o pgAdmin](#acessando-o-pgadmin)
* [Testando a API](#testando-a-api)
* [Comandos úteis](#comandos-úteis)
* [Boas práticas de segurança](#boas-práticas-de-segurança)
* [Contribuição](#contribuição)

---

## Sobre

Implementação de exemplo para gestão de pedidos com separação entre API, worker, banco e frontend.

---

## Pré-requisitos

* Docker e Docker Compose (versões recentes)
* .NET SDK 8.0
* Node.js 22
* Git (para clonar o repositório)

---

## Variáveis de ambiente (`.env`)

Crie um arquivo `.env` na raiz do repositório com as variáveis abaixo (substitua senhas por valores fortes quando for para produção):

```env
# Postgres
POSTGRES_DB=ordersdb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# API / Worker (.NET)
CONNECTION_STR=Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
ASPNETCORE_ENVIRONMENT=Development

# pgAdmin
PGADMIN_DEFAULT_EMAIL=admin@admin.local
PGADMIN_DEFAULT_PASSWORD=admin

# Frontend
REACT_APP_API_URL=http://api:80
```

Também mantenha um arquivo `.env.example` (sem segredos) para referência.

---

## Rodando com Docker Compose

1. Coloque o `.env` na raiz do repositório.

2. Suba todos os serviços:

```bash
docker compose up --build -d
```

3. Verifique o status dos serviços:

```bash
docker compose ps
```

4. Verifique logs (ex.: serviço `api`):

```bash
docker compose logs -f api
```

5. Parar e remover containers (mantendo volumes):

```bash
docker compose down
```

6. Parar e remover containers e volumes (remove dados do banco):

```bash
docker compose down -v
```

---

## Rodando localmente (sem Docker)

### Banco

Instale e execute PostgreSQL localmente ou use uma instância remota. Ajuste `CONNECTION_STR` para apontar para `localhost` e porta correta.

### API / Worker (.NET 8.0)

Navegue até a pasta do projeto da API e do worker. Configure as variáveis de ambiente:

Linux / macOS (bash):

```bash
export CONNECTION_STR='Host=localhost;Database=ordersdb;Username=postgres;Password=postgres;Port=5432'
export ASPNETCORE_ENVIRONMENT=Development
```

Windows PowerShell:

```powershell
$env:CONNECTION_STR = 'Host=localhost;Database=ordersdb;Username=postgres;Password=postgres;Port=5432'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
```

Restaurar dependências e executar:

```bash
dotnet restore
dotnet build
dotnet run --project ./src/Api/Api.csproj
```

Worker:

```bash
dotnet run --project ./src/Worker/Worker.csproj
```

### Frontend (React + Node 22)

Navegue até a pasta do frontend, instale dependências e execute:

```bash
npm install
npm start
```

O frontend normalmente ficará disponível em `http://localhost:3000`.

---

## Migrações e banco de dados

Se o projeto usa Entity Framework Core e contém migrations, aplique-as:

1. No ambiente local (com .NET SDK instalado):

```bash
dotnet tool restore
dotnet ef database update --project ./src/Api/Api.csproj --startup-project ./src/Api/Api.csproj
```

2. Dentro do container (após `docker compose up`):

```bash
docker compose exec api dotnet ef database update
```

Verifique os logs da API para confirmar aplicação de migrations automáticas (se implementado).

---

## Acessando o pgAdmin

* URL: conforme mapeamento do `docker-compose.yml` (ex.: `http://localhost:8080`).
* Usuário: `PGADMIN_DEFAULT_EMAIL` do `.env`.
* Senha: `PGADMIN_DEFAULT_PASSWORD` do `.env`.

Ao adicionar um servidor no pgAdmin, configure:

* Host: `postgres` (quando em Docker Compose)
* Port: `5432`
* Username / Password: conforme `.env`

---

## Testando a API

* Swagger (se exposto em Development): `http://<host_da_api>/swagger`

Exemplos de `curl` (ajuste endpoints conforme implementados):

```bash
curl http://localhost/api/orders

curl -X POST http://localhost/api/orders -H "Content-Type: application/json" -d '{"customer":"João","items":[]}'
```

---

## Comandos úteis

```bash
# Subir e rebuild
docker compose up --build -d

# Logs do serviço api
docker compose logs -f api

# Executar migrations no container
docker compose exec api dotnet ef database update

# Parar e remover containers + volumes
docker compose down -v
```

---

## Boas práticas de segurança

* **Não versionar** o arquivo `.env`. Adicione `.env` ao `.gitignore`.
* Mantenha um `.env.example` sem segredos.
* Use senhas fortes para `POSTGRES_PASSWORD` e `PGADMIN_DEFAULT_PASSWORD`.
* Em produção, utilize um gerenciador de segredos (ex.: Vault, AWS Secrets Manager, Azure Key Vault).
* Separe credenciais entre serviços (não reutilize a mesma senha para múltiplos serviços).

---

## Contribuição

Siga o fluxo padrão: abra issues descrevendo o problema/feature, crie branches com mudanças claras e envie pull requests com descrição e instruções de teste.

---

## Licença

Adicione a licença do projeto, se aplicável.
