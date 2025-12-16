# Mango API Solution Setup Guide

## Prerequisites

- Docker Desktop
- PowerShell 7+
- .NET 8.0 SDK

## Quick Start

### 1. Environment Setup

```powershell
# Clone and navigate to project
cd c:\Workspace\Code\Mango_Microservices

# Verify Docker is running
docker --version
```

### 2. Start Infrastructure Services

```powershell
# Start all services
docker-compose up -d

# Alternative: Start specific services
docker-compose -f docker-compose-sql.yml up -d     # SQL Server only
docker-compose -f docker-compose-postgres.yml up -d # PostgreSQL only
```

### 3. Database Setup

#### SQL Server

```powershell
# Load environment variables
$env:DB_PASSWORD = (Get-Content .env | Where-Object { $_ -match "^DB_SQL_PASSWORD=" }) -replace "^DB_SQL_PASSWORD=",""

try {
    # Test connection
    docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P "$env:DB_PASSWORD" `
        -Q "SELECT @@VERSION" -C

    # Initialize database
    docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P "$env:DB_PASSWORD" `
        -i /scripts/01-create-databases.sql -C
} finally {
    # Clear sensitive data
    Remove-Item Env:\DB_PASSWORD -ErrorAction SilentlyContinue
}
```

#### PostgreSQL

```powershell
# 1. Start all containers
docker-compose -f docker-compose-postgres.yml up -d

# 2. Load environment variables securely
try {
    $env:PGPASSWORD = (Get-Content .env | Where-Object { $_ -match "^DB_POSTGRES_PASSWORD=" }) -replace "^DB_POSTGRES_PASSWORD=",""
    $env:PGUSER = (Get-Content .env | Where-Object { $_ -match "^DB_POSTGRES_USER=" }) -replace "^DB_POSTGRES_USER=",""
    $env:PGDATABASE = (Get-Content .env | Where-Object { $_ -match "^DB_POSTGRES_DB=" }) -replace "^DB_POSTGRES_DB=",""

    # 3. Test PostgreSQL connection
    docker-compose -f docker-compose-postgres.yml exec postgres `
        psql -U "$env:PGUSER" -d "$env:PGDATABASE" -c "SELECT version();"

    # 4. Initialize databases (if you have initialization scripts)
    docker-compose -f docker-compose-postgres.yml exec postgres `
        psql -U "$env:PGUSER" -d "$env:PGDATABASE" -f /scripts/01-create-databases.sql

    # 5. Verify databases were created
    docker-compose -f docker-compose-postgres.yml exec postgres `
        psql -U "$env:PGUSER" -d "$env:PGDATABASE" -c "SELECT datname FROM pg_database WHERE datname LIKE 'Mango_%';"

} finally {
    # 6. Clear sensitive environment variables
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\PGUSER -ErrorAction SilentlyContinue
    Remove-Item Env:\PGDATABASE -ErrorAction SilentlyContinue
}
```

### 4. Verify Services

```powershell
# Check all container statuses
docker-compose ps

# View logs
docker-compose logs -f

# Check specific service logs
docker-compose logs sqlserver
docker-compose logs postgres
docker-compose logs rabbitmq
docker-compose logs redis
```

### 5. Development Commands

```powershell
# Update database with migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Drop database
dotnet ef database drop --force

# Update EF Tools
dotnet tool update --global dotnet-ef
```

### 6. Cleanup

```powershell
# Stop all containers
docker-compose down

# Remove volumes (careful - destroys data)
docker-compose down -v

# Remove all containers and images
docker-compose down --rmi all
```

```bash
-cd API:
nuget:
	<PackageReference Include="AutoMapper" Version="14.0.0" />
	<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.13" />
	<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.17" />
	<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.5" />
	<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.5" />
	<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.5">
		<PrivateAssets>all</PrivateAssets>
		<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>
	<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />

-cmd of API:
-- dotnet tool install --global dotnet-ef --version 9.0.4
-- dotnet ef
-- dotnet ef migrations add InitialCreate -o Data/Migrations
-- dotnet ef database update

-want to delete migrations added ?
-- dotnet ef migrations remove

-want to restore all database?
-- dotnet ef database drop

-update latest version of ef
-- dotnet tool update --global dotnet-ef

-- migrations PostgreSQL, SQLServer

dotnet ef migrations add InitialCreate --context PostgreSqlAppDbContext --output-dir Migrations/PostgreSQL

dotnet ef migrations add InitialCreate --context SqlServerAppDbContext --output-dir Migrations/SqlServer

```

> Azure Service Bus

```bash
# 1. created
# 1.1. Azure: serviceBus, queue or TopicName
-- created new Service Bus (service)
-- created new Queue or Topic Name (include)
-- get config from Azure Service Bus: Settings: Shared access policies
# 1.2. Coding:  new classLibrary and IMessageBus, MessageBus | IAzureServiceBusConsumer, AzureServiceBusConsumer
# 1.2.1:  new classLibrary and IMessageBus, MessageBus
-- created new classLibrary => nuget: added Azure.Messaging.ServiceBus
-- created new IMessageBus and MessageBus
-- add config TopicAndQueueNames (map with name of Azure created)

-- MessageBus implement: create function PublishMessage (object message, string topicQueueName) :
	- created ServiceBusSender with topicQueueName
	- created ServiceBusMessage with message and with id == new generate UUID

-- created IAzureServiceBusConsumer, AzureServiceBusConsumer

# 1.2.2:  new IAzureServiceBusConsumer, AzureServiceBusConsumer
-- AzureServiceBusConsumer implement:
	# constructor
 	- CreateProcessor for Queue or Topic Name (ValidateEntityName)
	# Start function () => executed when this.IApplicationBuilder(this app) start
	- Start	Processor
	- Register function listening event from queue (PublishMessage executed) with _registerNameProcessor

	# Stop function () => executed when this.IApplicationBuilder(this app) stop
	- Stops the Azure Service Bus and disposes of the processors.

# 2. Coding: Implement service bus receiver
-- Reference classLibrary: Azure.Messaging.ServiceBus
-- Using PublishMessage function with message and topicQueueName needed.
```
