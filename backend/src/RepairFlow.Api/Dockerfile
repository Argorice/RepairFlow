# Сборка выполняется из папки backend: docker build -f src/RepairFlow.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файлы проектов — так слой с restore переиспользуется, пока не менялись зависимости.
COPY RepairFlow.sln ./
COPY src/RepairFlow.Api/RepairFlow.Api.csproj src/RepairFlow.Api/
COPY tests/RepairFlow.Tests/RepairFlow.Tests.csproj tests/RepairFlow.Tests/
RUN dotnet restore src/RepairFlow.Api/RepairFlow.Api.csproj

COPY . .
RUN dotnet publish src/RepairFlow.Api/RepairFlow.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

COPY --from=build /app/publish .

RUN mkdir -p /app/storage

EXPOSE 8080

ENTRYPOINT ["dotnet", "RepairFlow.Api.dll"]
