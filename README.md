# Here is resource for Mango.API Solution.

> [!NOTE]
> For BE

```bash
-- Running docker CLI
# 1. Start all containers
docker-compose -f docker-compose-dev.yml up -d

# 2. Wait for SQL Server to be ready (optional test)
docker-compose -f docker-compose-dev.yml exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -Q "SELECT @@VERSION" -C

# 3. Initialize databases (THIS IS THE MANUAL STEP)
docker-compose -f docker-compose-dev.yml exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -i /scripts/01-create-databases.sql -C

# 4. Verify databases were created
docker-compose -f docker-compose-dev.yml exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -Q "SELECT name FROM sys.databases WHERE name LIKE 'Mango_%'" -C

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
