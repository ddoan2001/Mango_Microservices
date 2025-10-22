# PostgreSQL Database Setup Script
$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptPath))
$envPath = Join-Path $rootPath ".env"
$dockerComposePath = Join-Path $rootPath "docker-compose-postgres.yml"

function Write-Log {
    param($Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'): $Message"
}

function Test-DockerRunning {
    try {
        docker info > $null 2>&1
        return $true
    }
    catch {
        return $false
    }
}

function Wait-PostgreSQL {
    param (
        [int]$maxAttempts = 30,
        [int]$delaySeconds = 2
    )
    
    for ($i = 1; $i -le $maxAttempts; $i++) {
        Write-Log "Attempt $i of $maxAttempts..."
        try {
            $result = docker-compose -f $dockerComposePath exec -T postgres pg_isready
            if ($result -like "*accepting connections*") { 
                Write-Log "PostgreSQL is ready!"
                return $true 
            }
        }
        catch {
            if ($i -eq $maxAttempts) { 
                throw "PostgreSQL failed to start after $maxAttempts attempts"
            }
            Start-Sleep -Seconds $delaySeconds
        }
    }
    return $false
}

try {
    # Check prerequisites
    Write-Log "Checking prerequisites..."
    if (-not (Test-DockerRunning)) {
        throw "Docker is not running. Please start Docker Desktop first."
    }
    if (-not (Test-Path $envPath)) {
        throw "Environment file not found at: $envPath"
    }
    if (-not (Test-Path $dockerComposePath)) {
        throw "Docker compose file not found at: $dockerComposePath"
    }

    # Load environment variables from .env
    Write-Log "Loading environment variables..."
    $envContent = Get-Content $envPath
    
    # Map PostgreSQL environment variables
    $env:PGUSER = ($envContent | Where-Object { $_ -match '^DB_POSTGRES_USER=(.+)$' } | ForEach-Object { $matches[1] })
    $env:PGPASSWORD = ($envContent | Where-Object { $_ -match '^DB_POSTGRES_PASSWORD=(.+)$' } | ForEach-Object { $matches[1] })
    $env:PGDATABASE = ($envContent | Where-Object { $_ -match '^DB_POSTGRES_DB=(.+)$' } | ForEach-Object { $matches[1] })
    $env:PGHOST = ($envContent | Where-Object { $_ -match '^DB_POSTGRES_HOST=(.+)$' } | ForEach-Object { $matches[1] })
    $env:PGPORT = ($envContent | Where-Object { $_ -match '^DB_POSTGRES_PORT=(.+)$' } | ForEach-Object { $matches[1] })

    Write-Log "Environment loaded:"
    Write-Log "  Host: $env:PGHOST"
    Write-Log "  Port: $env:PGPORT"
    Write-Log "  Database: $env:PGDATABASE"
    Write-Log "  User: $env:PGUSER"

    # Start containers
    Write-Log "Starting PostgreSQL containers..."
    docker-compose -f $dockerComposePath up -d postgres adminer
    
    # Wait for PostgreSQL
    Write-Log "Waiting for PostgreSQL to be ready..."
    Wait-PostgreSQL

    # Test connection
    Write-Log "Testing PostgreSQL connection..."
    $version = docker-compose -f $dockerComposePath exec -T postgres `
        psql -U "$env:PGUSER" -d "$env:PGDATABASE" -t -c "SELECT version();"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Log "Connected to PostgreSQL:`n$($version.Trim())"
    } else {
        throw "Failed to connect to PostgreSQL"
    }

    # First ensure dblink extension is available
    Write-Log "Installing dblink extension..."
    docker-compose -f $dockerComposePath exec -T postgres `
        psql -U "$env:PGUSER" -d "$env:PGDATABASE" -c "CREATE EXTENSION IF NOT EXISTS dblink;"

    # Initialize databases
    $scriptFile = Join-Path $scriptPath "..\Sqls\01-create-databases.sql"
    if (Test-Path $scriptFile) {
        Write-Log "Creating databases..."
        $output = Get-Content $scriptFile | docker-compose -f $dockerComposePath exec -T postgres `
            psql -U "$env:PGUSER" -d "$env:PGDATABASE" -v ON_ERROR_STOP=1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Database creation failed with exit code $LASTEXITCODE`n$output"
        }
        
        Write-Log "Database creation output:`n$output"

        # Verify databases
        Write-Log "Verifying created databases..."
        $dbList = docker-compose -f $dockerComposePath exec -T postgres `
            psql -U "$env:PGUSER" -d "$env:PGDATABASE" -t -c "
                SELECT 
                    datname as database_name,
                    pg_size_pretty(pg_database_size(datname)) as size
                FROM pg_database 
                WHERE datname LIKE 'Mango_%'
                ORDER BY datname;"
        
        if ($dbList) {
            Write-Log "Successfully created databases:`n$dbList"
        } else {
            Write-Log "Warning: No Mango databases found after creation"
        }
    }
    else {
        throw "Database initialization script not found at: $scriptFile"
    }

    Write-Log "Database setup completed successfully!"
}
catch {
    Write-Log "Error: $_"
    Write-Log $_.ScriptStackTrace
    exit 1
}
finally {
    # Clear sensitive environment variables
    @('PGUSER', 'PGPASSWORD', 'PGDATABASE', 'PGHOST', 'PGPORT') | ForEach-Object {
        Remove-Item "env:$_" -ErrorAction SilentlyContinue
    }
}