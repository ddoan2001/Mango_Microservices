#!/bin/bash
set -e

echo "Starting SQL Server initialization process..."

# Start SQL Server in background
echo "Starting SQL Server..."
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
sleep 30

# Test connection before running scripts
echo "Testing SQL Server connection..."
until /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -C > /dev/null 2>&1; do
    echo "Waiting for SQL Server to accept connections..."
    sleep 5
done

echo "SQL Server is ready! Running initialization scripts..."

# Run initialization scripts
for f in /docker-entrypoint-initdb.d/*.sql; do
    if [ -f "$f" ]; then
        echo "Running $f"
        /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -i "$f" -C
        echo "Completed $f"
    fi
done

echo "Database initialization completed successfully!"

# Keep SQL Server running in foreground
wait