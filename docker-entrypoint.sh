#!/bin/bash
set -e

echo "[EcomMonitor] Esperando SQL Server..."

until sqlcmd -S "$DB_HOST,1433" -U "$DB_USER" -P "$DB_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; do
    echo "[EcomMonitor] SQL Server no disponible aún, reintentando en 3s..."
    sleep 3
done

echo "[EcomMonitor] SQL Server listo."
echo "[EcomMonitor] Iniciando app..."

exec dotnet MonitorPedidos.Web.dll
