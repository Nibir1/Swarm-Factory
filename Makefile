# ====================================================================================
# SWARM-FACTORY MAKEFILE
# ====================================================================================
# Orchestrates Infrastructure, Secrets, and Microservices for local development.
# ====================================================================================

# --- CONFIGURATION VARIABLES ---
RESOURCE_GROUP := rg-swarm-factory
LOCATION := swedencentral
TEMPLATE_FILE := infra/main.bicep
PARAMETERS_FILE := infra/azure.parameters.json

# UPDATE THESE AFTER FIRST DEPLOYMENT (Check Azure Portal or 'make infra' output)
# These specific names were taken from your previous prompt context.
COSMOSDB_ACCOUNT := cosmos-sf-dev-lbbaa4k3
EVENTHUB_NAMESPACE := evhns-sf-dev-lbbaa4k3

# --- INFRASTRUCTURE ---

.PHONY: resource-group infra

# 1. Create Resource Group
resource-group:
	@echo "Creating Resource Group: $(RESOURCE_GROUP)..."
	az group create --name $(RESOURCE_GROUP) --location $(LOCATION)

# 2. Deploy Infrastructure (Bicep)
infra:
	@echo "Deploying Azure Resources..."
	az deployment group create \
		--resource-group $(RESOURCE_GROUP) \
		--template-file $(TEMPLATE_FILE) \
		--parameters $(PARAMETERS_FILE)

# --- SECRETS & CONFIGURATION ---

.PHONY: get-cosmos-conn get-evh-conn help

# Get Cosmos DB Connection String (for TwinAPI and Processor)
get-cosmos-conn:
	@echo "Fetching Cosmos DB Connection String..."
	@az cosmosdb keys list \
		--resource-group $(RESOURCE_GROUP) \
		--name $(COSMOSDB_ACCOUNT) \
		--type connection-strings \
		--query "connectionStrings[0].connectionString" --output tsv

# Get Event Hub Connection String (for Processor and Simulator)
get-evh-conn:
	@echo "Fetching Event Hub Connection String..."
	@az eventhubs namespace authorization-rule keys list \
		--resource-group $(RESOURCE_GROUP) \
		--namespace-name $(EVENTHUB_NAMESPACE) \
		--name RootManageSharedAccessKey \
		--query primaryConnectionString --output tsv

# --- RUNTIME (RUN IN SEPARATE TERMINALS) ---

.PHONY: run-azurite run-api run-processor run-simulator run-frontend

# Terminal 1: Local Storage Emulator (Required for Azure Functions)
# Ensures a clean local workspace for Azurite in a temp folder to avoid git clutter
run-azurite:
	@echo "Starting Azurite (Local Storage Emulator)..."
	@if not exist .azurite mkdir .azurite
	azurite --location .azurite --debug .azurite/debug.log

# Terminal 2: The "Brain" (Web API)
run-api:
	@echo "Starting TwinAPI (.NET 8 Web API)..."
	dotnet run --project ./src/SwarmFactory.TwinAPI/SwarmFactory.TwinAPI.csproj --launch-profile https

# Terminal 3: The "Nervous System" (Azure Function)
# NOTE: Requires 'npm install -g azure-functions-core-tools@4'
run-processor:
	@echo "Starting Telemetry Processor (Azure Function)..."
	cd src/SwarmFactory.Processor && func start

# Terminal 4: The "Heartbeat" (IoT Simulator)
run-simulator:
	@echo "Starting IoT Simulator (Console App)..."
	dotnet run --project ./src/SwarmFactory.Simulator/SwarmFactory.Simulator.csproj

# Terminal 5: The "Face" (React Frontend)
run-frontend:
	@echo "Starting React Dashboard..."
	cd frontend && npm run dev

# --- UTILITIES ---

.PHONY: clean

clean:
	@echo "Cleaning build artifacts..."
	dotnet clean ./src/SwarmFactory.TwinAPI
	dotnet clean ./src/SwarmFactory.Processor
	dotnet clean ./src/SwarmFactory.Simulator
	@if exist .azurite rd /s /q .azurite