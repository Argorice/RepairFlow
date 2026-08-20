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

# Npgsql при старте пробует загрузить GSSAPI (Kerberos-аутентификация). В slim-образе библиотеки нет,
# и в лог падает «Cannot load library libgssapi_krb5.so.2». На работу это не влияет — подключение
# идёт по паролю, — но чистый лог стоит двух мегабайт.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# 10000 — порт, который Render ждёт по умолчанию, если переменная PORT не задана.
# Берём его значением по умолчанию, чтобы образ работал и с блюпринтом, и на вручную
# созданном сервисе, и на любой площадке, которая передаёт свой PORT.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV PORT=10000

# ASP.NET по умолчанию следит за appsettings.json через inotify, чтобы перечитывать конфигурацию
# на лету. На плотно упакованных хостах лимит inotify-инстансов (128 на пользователя) бывает уже
# выбран соседями, и приложение падает ещё внутри WebApplication.CreateBuilder.
# В контейнере перечитывать конфиг незачем: он неизменен от старта до старта.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

COPY --from=build /app/publish .

RUN mkdir -p /app/storage

EXPOSE 10000

# Слушать надо 0.0.0.0, а не localhost: иначе снаружи в контейнер не достучаться и балансировщик
# вернёт 502. Форма «sh -c» обязательна — в exec-форме переменная PORT не раскроется.
CMD ["sh", "-c", "dotnet RepairFlow.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
