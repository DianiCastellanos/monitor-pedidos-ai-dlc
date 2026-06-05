# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MonitorPedidos.Domain/MonitorPedidos.Domain.csproj           src/MonitorPedidos.Domain/
COPY src/MonitorPedidos.Infrastructure/MonitorPedidos.Infrastructure.csproj src/MonitorPedidos.Infrastructure/
COPY src/MonitorPedidos.Web/MonitorPedidos.Web.csproj                 src/MonitorPedidos.Web/

RUN dotnet restore src/MonitorPedidos.Web/MonitorPedidos.Web.csproj

COPY src/ src/

RUN dotnet publish src/MonitorPedidos.Web/MonitorPedidos.Web.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV TZ=America/Bogota
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Instalar sqlcmd para esperar que SQL Server esté listo
RUN apt-get update && apt-get install -y curl gnupg2 && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/debian/11/prod.list > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ENV PATH="$PATH:/opt/mssql-tools18/bin"

COPY --from=build /app/publish .
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["/docker-entrypoint.sh"]
