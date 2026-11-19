# Build context: repository root (needs shared/ and the Directory.*.props files)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore in its own layer: only reruns when a .csproj/.props file changes
COPY Directory.Build.props Directory.Packages.props ./
COPY shared/Orders.Core/Orders.Core.csproj shared/Orders.Core/
COPY api/Orders.Api/Orders.Api.csproj api/Orders.Api/
RUN dotnet restore api/Orders.Api/Orders.Api.csproj

COPY shared/ shared/
COPY api/ api/
RUN dotnet publish api/Orders.Api/Orders.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
