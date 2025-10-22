# SQL Server Database Setup Script
$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptPath))
$envPath = Join-Path $rootPath ".env"
$dockerComposePath = Join-Path $rootPath "docker-compose-sql.yml"

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

function Wait-SqlServer {
    param (
        [int]$maxAttempts = 30,
        [int]$delaySeconds = 2
    )
    
    for ($i = 1; $i -le $maxAttempts; $i++) {
        Write-Log "Attempt $i of $maxAttempts..."
        try {
            $result = docker-compose -f $dockerComposePath exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd `
                -S "localhost,1433" `
                -U sa `
                -P "$env:SQLPASSWORD" `
                -Q "SELECT 1" `
                -C `
                -t 30 `
                -N `
                -b `
                -o "/tmp/sqloutput.txt" `
                -v TRUST="TrustServerCertificate=yes"
            if ($LASTEXITCODE -eq 0) { 
                Write-Log "SQL Server is ready!"
                return $true 
            }
        }
        catch {
            Write-Log "Connection attempt failed: $($_.Exception.Message)"
            if ($i -eq $maxAttempts) { 
                throw "SQL Server failed to start after $maxAttempts attempts"
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
    
    # Map SQL Server environment variables
    $env:SQLPASSWORD = ($envContent | Where-Object { $_ -match '^DB_SQL_PASSWORD=(.+)$' } | ForEach-Object { $matches[1] })
    $env:SQLHOST = ($envContent | Where-Object { $_ -match '^DB_SQL_HOST=(.+)$' } | ForEach-Object { $matches[1] })
    $env:SQLPORT = ($envContent | Where-Object { $_ -match '^DB_SQL_PORT=(.+)$' } | ForEach-Object { $matches[1] })

    Write-Log "Environment loaded:"
    Write-Log "  Host: $env:SQLHOST"
    Write-Log "  Port: $env:SQLPORT"
    Write-Log "  User: sa"

    # Start containers
    Write-Log "Starting SQL Server container..."
    docker-compose -f $dockerComposePath up -d sqlserver
    
    # Wait for SQL Server
    Write-Log "Waiting for SQL Server to be ready..."
    Wait-SqlServer

    # Initialize databases
    $scriptFile = Join-Path $scriptPath "..\Sqls\01-create-databases.sql"
    if (Test-Path $scriptFile) {
        Write-Log "Creating databases..."
        # Verify SQL file exists in container
        Write-Log "Verifying SQL script in container..."
        $checkFile = docker-compose -f $dockerComposePath exec -T sqlserver ls /scripts/01-create-databases.sql
        if ($LASTEXITCODE -ne 0) {
            throw "SQL script not found in container. Check volume mapping in docker-compose-sql.yml"
        }

        Write-Log "Executing SQL script..."
        $output = docker-compose -f $dockerComposePath exec -T sqlserver `
            /opt/mssql-tools18/bin/sqlcmd `
            -S "localhost,1433" `
            -U sa `
            -P "$env:SQLPASSWORD" `
            -i "/scripts/01-create-databases.sql" `
            -C `
            -t 30 `
            -N `
            -b `
            -h-1 `
            -W `
            -v TRUST="TrustServerCertificate=yes"
        
        if ($LASTEXITCODE -ne 0) {
            throw "Database creation failed with exit code $LASTEXITCODE`n$output"
        }
        
        # Format and display the output
        $formattedOutput = $output -split "`n" | ForEach-Object {
            $line = $_.Trim()
            if ($line -match '^NOTICE:|^Database Name|^-{20,}|^Mango_') {
                $line
            }
        } | Where-Object { $_ }
        Write-Log ($formattedOutput -join "`n")
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
    @('SQLPASSWORD', 'SQLHOST', 'SQLPORT') | ForEach-Object {
        Remove-Item "env:$_" -ErrorAction SilentlyContinue
    }
}