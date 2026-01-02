# Create a resource group
resource-group:
	az group create --name rg-swarm-factory --location swedencentral

RESOURCE_GROUP := rg-swarm-factory
TEMPLATE_FILE := infra/main.bicep
PARAMETERS_FILE := infra/azure.parameters.json
COSMOSDB_NAME := cosmos-sf-dev-lbbaa4k3
EVENTHUB_NAMESPACE := evhns-sf-dev-lbbaa4k3

# Deploy the Bicep template to the specified resource group
deploy:
	@echo "Deploying Bicep template to resource group $(RESOURCE_GROUP)..."
	az deployment group create \
		--resource-group $(RESOURCE_GROUP) \
		--template-file $(TEMPLATE_FILE) \
		--parameters $(PARAMETERS_FILE)

# Retrieve the Cosmos DB connection string
connection-string:
	@echo "Cosmos DB Connection String:"
	az cosmosdb keys list \
		--resource-group $(RESOURCE_GROUP) \
		--name $(COSMOSDB_NAME) \
		--type connection-strings

# Run the SwarmFactory.TwinAPI project
TwinAPI-run:
	@echo "Running the SwarmFactory.TwinAPI project..."
	dotnet run --project ./src/SwarmFactory.TwinAPI/SwarmFactory.TwinAPI.csproj --launch-profile TwinAPI

# Retrieve the Event Hubs connection string
event-hubs-connection-string:
	@echo "Event Hubs Connection String:"
	az eventhubs namespace authorization-rule keys list \
  	--resource-group $(RESOURCE_GROUP) \
  	--namespace-name $(EVENTHUB_NAMESPACE) \
  	--name RootManageSharedAccessKey \
  	--query primaryConnectionString --output tsv


.PHONY: resource-group deploy connection-string TwinAPI-run event-hubs-connection-string