# 📦 Gestão de Pedidos

Sistema simples de **gestão de pedidos**, com **API**, **Frontend** e um **Worker** para processamento assíncrono.
O projeto está totalmente containerizado via **Docker Compose**.

---

## 🚀 Tecnologias

* **Backend (API e Worker):** .NET / C#
* **Frontend:** Vite + React
* **Banco de Dados:** SQL Server (Docker)
* **Orquestração:** Docker Compose

---

## ⚙️ Pré-requisitos

Antes de começar, você precisa ter instalado em sua máquina:

* [Docker](https://docs.docker.com/get-docker/)
* [Docker Compose](https://docs.docker.com/compose/install/)
* [Git](https://git-scm.com/downloads)

---

## 📂 Estrutura do projeto

```
Gestao-de-Pedidos/
├─ api/Orders.Api/        # API em .NET
├─ frontend/              # Frontend em React + Vite
├─ worker/Orders.Worker/  # Worker para processamento
├─ docker-compose.yml     # Orquestração dos serviços
```

---

## 🔑 Configuração de variáveis de ambiente

O projeto usa arquivos `.env` para configurar API e Frontend.
Existem modelos prontos (`.env.example`) em cada pasta.

### 1. API

Copie o arquivo de exemplo e ajuste as variáveis:

```bash
cp api/Orders.Api/.env.example api/Orders.Api/.env
```

Exemplo de configuração (`api/Orders.Api/.env`):

```ini
ASPNETCORE_URLS=http://+:5000
ASPNETCORE_ENVIRONMENT=Development
DB_CONNECTION_STRING=Server=orders_db;Database=ordersdb;User Id=sa;Password=YourPassword123;
JWT_SECRET=your_super_secret_key_here
JWT_ISSUER=GestaoDePedidos
JWT_AUDIENCE=GestaoDePedidosUsers
```

---

### 2. Frontend

Copie o arquivo de exemplo e ajuste as variáveis:

```bash
cp frontend/.env.example frontend/.env
```

Exemplo de configuração (`frontend/.env`):

```ini
VITE_API_URL=http://localhost:5000
VITE_ENV=development
VITE_AUTH_PROVIDER=local
```

---

## ▶️ Como rodar o projeto

1. Clone o repositório:

   ```bash
   git clone https://github.com/jotavfonseca/Gestao-de-Pedidos.git
   cd Gestao-de-Pedidos
   ```

2. Configure os `.env` conforme explicado acima.

3. Suba os containers com Docker Compose:

   ```bash
   docker-compose up --build
   ```

4. Acesse os serviços:

   * **API:** [http://localhost:5000](http://localhost:5000)
   * **Frontend:** [http://localhost:3000](http://localhost:3000)
   * **Banco SQL Server:** `localhost:1433`

---

## 🧪 Testando

* Você pode testar a API com ferramentas como [Postman](https://www.postman.com/) ou [Insomnia](https://insomnia.rest/).
* Se o frontend estiver rodando, acesse a interface em [http://localhost:3000](http://localhost:3000).

---

## 📌 Observações

* Os arquivos `.env` **não devem ser versionados**.
* Para produção, ajuste senhas e variáveis de ambiente com valores seguros.
* O worker compartilha o mesmo `.env` da API para acessar o banco.
