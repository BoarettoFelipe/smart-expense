FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY backend/src/SmartExpense.Domain/SmartExpense.Domain.csproj backend/src/SmartExpense.Domain/
COPY backend/src/SmartExpense.Application/SmartExpense.Application.csproj backend/src/SmartExpense.Application/
COPY backend/src/SmartExpense.Infrastructure/SmartExpense.Infrastructure.csproj backend/src/SmartExpense.Infrastructure/
COPY backend/src/SmartExpense.Api/SmartExpense.Api.csproj backend/src/SmartExpense.Api/

RUN dotnet restore backend/src/SmartExpense.Api/SmartExpense.Api.csproj

COPY backend/src/ backend/src/

RUN dotnet build backend/src/SmartExpense.Api/SmartExpense.Api.csproj \
    --configuration Release \
    --no-restore

FROM build AS publish

RUN dotnet publish backend/src/SmartExpense.Api/SmartExpense.Api.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migrations

RUN dotnet tool restore

ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "backend/src/SmartExpense.Infrastructure/SmartExpense.Infrastructure.csproj", "--startup-project", "backend/src/SmartExpense.Api/SmartExpense.Api.csproj", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS api

WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "SmartExpense.Api.dll"]
