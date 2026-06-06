#!/bin/bash
set -e

DB_PROVIDER="${DB_PROVIDER:-sqlserver}"

# En Supabase: arrancar la app DIRECTAMENTE, sin esperar ni mirar SQL Server.
# (SQL Server requiere VPN y no es alcanzable en producción/Render).
if [ "$(echo "$DB_PROVIDER" | tr '[:upper:]' '[:lower:]')" = "supabase" ]; then
    echo "[EcomMonitor] DB_PROVIDER=supabase — omitiendo SQL Server, iniciando app directamente..."
    exec dotnet MonitorPedidos.Web.dll
fi

# En SQL Server: esperar a que la BD esté lista, pero con un máximo de reintentos
# para no quedar atrapado en un bucle infinito si la BD nunca responde.
echo "[EcomMonitor] DB_PROVIDER=sqlserver — esperando SQL Server (máx. 10 intentos)..."
ATTEMPTS=0
MAX_ATTEMPTS=10
until sqlcmd -S "$DB_HOST,1433" -U "$DB_USER" -P "$DB_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; do
    ATTEMPTS=$((ATTEMPTS + 1))
    if [ "$ATTEMPTS" -ge "$MAX_ATTEMPTS" ]; then
        echo "[EcomMonitor] SQL Server no respondió tras $MAX_ATTEMPTS intentos — iniciando en modo degradado."
        break
    fi
    echo "[EcomMonitor] SQL Server no disponible aún (intento $ATTEMPTS/$MAX_ATTEMPTS), reintentando en 3s..."
    sleep 3
done

echo "[EcomMonitor] Iniciando app..."
exec dotnet MonitorPedidos.Web.dll
