# Сборка выполняется из папки backend: docker build -f src/RepairFlow.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файлы проектов — так слой с restore переиспользуется, пока не менялись зависимости.
COPY RepairFlow.sln ./
COPY Directory.Build.props ./
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

# 10000 — порт, который Render ждёт по умолчанию, если переменная PORT не задана.
# Берём его значением по умолчанию, чтобы образ работал и с блюпринтом, и на вручную
# созданном сервисе, и на любой площадке, которая передаёт свой PORT.
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    PORT=10000

COPY --from=build /app/publish .

RUN mkdir -p /app/storage

EXPOSE 10000

# Слушать надо 0.0.0.0, а не localhost: иначе снаружи в контейнер не достучаться и балансировщик
# вернёт 502. Форма «sh -c» обязательна — в exec-форме переменная PORT не раскроется.
CMD ["sh", "-c", "dotnet RepairFlow.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
