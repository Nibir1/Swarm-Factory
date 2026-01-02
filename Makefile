# Create a resource group
resource-group:
	az group create --name rg-swarm-factory --location swedencentral

RESOURCE_GROUP := rg-swarm-factory
TEMPLATE_FILE := infra/main.bicep
PARAMETERS_FILE := infra/azure.parameters.json

# Deploy the Bicep template to the specified resource group
deploy:
	az deployment group create \
		--resource-group $(RESOURCE_GROUP) \
		--template-file $(TEMPLATE_FILE) \
		--parameters $(PARAMETERS_FILE)


.PHONY: resource-group deploy