# api/Orders.Api/Dockerfile
# Build stage (SDK 8.0)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o arquivo de projeto (a instrução espera que o Dockerfile esteja dentro da pasta do projeto)
COPY *.csproj ./

# Restaura pacotes (rápido porque só usa o csproj)
RUN dotnet restore

# Copia todo o código fonte
COPY . ./

# Publica em Release para /app
RUN dotnet publish -c Release -o /app

# Runtime stage (ASP.NET 8.0)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instala curl (útil para healthchecks no container)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copia artefatos publicados
COPY --from=build /app ./

# Habilita ASP.NET para escutar na porta 80
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Ajuste "Orders.Api.dll" para o nome correto do seu assembly (igual ao nome do csproj)
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
