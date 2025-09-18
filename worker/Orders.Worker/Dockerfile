# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todos os arquivos do Worker para o container
COPY . .

# Se a API está fora do contexto, precisa copiar manualmente
# COPY ../../api ../api   # só se quiser referenciar diretamente

RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Orders.Worker.dll"]
