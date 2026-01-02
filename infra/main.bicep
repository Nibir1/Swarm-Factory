/* SWARM-FACTORY INFRASTRUCTURE
  ----------------------------
  Target: Azure PaaS (Serverless)
  Description: Event Hubs, Cosmos DB (serverless), Azure Functions
*/

@description('The location for all resources.')
param location string = resourceGroup().location

@description('Deployment environment name (dev, prod).')
param envName string = 'dev'

@description('A unique suffix to ensure global uniqueness.')
param uniqueSuffix string = uniqueString(resourceGroup().id)

// ------------------------------------------------------------
// NAMING (Azure-safe, student-safe)
// ------------------------------------------------------------
var appCode = 'sf' // swarm-factory short code
var envCode = envName == 'prod' ? 'p' : 'd'
var shortSuffix = substring(uniqueSuffix, 0, 8)

var storageName = 'st${appCode}${envCode}${shortSuffix}'
var cosmosAccountName = 'cosmos-${appCode}-${envName}-${shortSuffix}'
var eventHubNamespaceName = 'evhns-${appCode}-${envName}-${shortSuffix}'
var functionAppName = 'func-${appCode}-${envName}-${shortSuffix}'
var appServicePlanName = 'asp-${appCode}-${envName}-${shortSuffix}'

// ------------------------------------------------------------
// 1. STORAGE ACCOUNT (Azure Functions requirement)
// ------------------------------------------------------------
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// ------------------------------------------------------------
// 2. COSMOS DB (Serverless NoSQL)
// ------------------------------------------------------------
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosAccount
  name: 'SwarmDb'
  properties: {
    resource: {
      id: 'SwarmDb'
    }
  }
}

resource machinesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDb
  name: 'machines'
  properties: {
    resource: {
      id: 'machines'
      partitionKey: {
        paths: [
          '/type'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource alertsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDb
  name: 'alerts'
  properties: {
    resource: {
      id: 'alerts'
      partitionKey: {
        paths: [
          '/machineId'
        ]
        kind: 'Hash'
      }
    }
  }
}

// ------------------------------------------------------------
// 3. EVENT HUBS (Telemetry ingestion)
// ------------------------------------------------------------
resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: eventHubNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: eventHubNamespace
  name: 'telemetry'
  properties: {
    messageRetentionInDays: 1
    partitionCount: 2
  }
}

resource consumerGroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-11-01' = {
  parent: eventHub
  name: 'func-processor'
  properties: {}
}

// ------------------------------------------------------------
// 4. AZURE FUNCTION APP (Compute)
// ------------------------------------------------------------
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'CosmosConnection'
          value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'EventHubConnection'
          value: listKeys('${eventHubNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', '2021-11-01').primaryConnectionString
        }
      ]
    }
    httpsOnly: true
  }
}

// ------------------------------------------------------------
// OUTPUTS
// ------------------------------------------------------------
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
output eventHubNamespace string = eventHubNamespace.name
output functionAppName string = functionApp.name
