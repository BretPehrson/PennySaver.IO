#!/bin/bash
set -e

echo "Starting SQL Server..."
/opt/mssql/bin/sqlservr &
SERVER_PID=$!

# Give SQL Server a moment to start
sleep 5

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
for i in {1..60}; do
  if /opt/mssql-tools18/bin/sqlcmd -S "127.0.0.1" -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" 2>/dev/null; then
    echo "SQL Server is ready!"
    break
  fi
  echo "Attempt $i: Waiting for SQL Server..."
  sleep 1
done

echo "Running initialization script..."
/opt/mssql-tools18/bin/sqlcmd -S "127.0.0.1" -U sa -P "$MSSQL_SA_PASSWORD" -C -i /docker-entrypoint-initdb.d/01-init.sql

echo "Initialization complete!"

# Keep SQL Server running in foreground
wait $SERVER_PID