# Build context: repository root (needs shared/ and the Directory.*.props files)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY shared/Orders.Core/Orders.Core.csproj shared/Orders.Core/
COPY worker/Orders.Worker/Orders.Worker.csproj worker/Orders.Worker/
RUN dotnet restore worker/Orders.Worker/Orders.Worker.csproj

COPY shared/ shared/
COPY worker/ worker/
RUN dotnet publish worker/Orders.Worker/Orders.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

USER app
ENTRYPOINT ["dotnet", "Orders.Worker.dll"]
