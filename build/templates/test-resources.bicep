// Define the location for the deployment of the components.
param location string = resourceGroup().location

// Define the name of the single Azure Service bus namespace.
param serviceBusNamespace string

// Define the name of the single Azure EventHubs namespace.
param eventHubsNamespace string

// Define the name of the storage account that will be created.
param storageAccountName string

// Define the name of the Key Vault.
param keyVaultName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

module serviceBus 'br/public:avm/res/service-bus/namespace:0.8.0' = {
  name: 'serviceBusDeployment'
  params: {
    name: serviceBusNamespace
    location: location
    skuObject: {
      name: 'Basic'
    }
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Azure Service Bus Data Owner'
      }
    ]
  }
}

module hubs 'br/public:avm/res/event-hub/namespace:0.7.0' = {
  name: 'eventHubsDeployment'
  params: {
    name: eventHubsNamespace
    location: location
    skuName: 'Basic'
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Azure Event Hubs Data Owner'
      }
    ]
  }
}

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccountDeployment'
  params: {
    name: storageAccountName
    location: location
    allowBlobPublicAccess: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
    ]
  }
}

module vault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'vaultDeployment'
  params: {
    name: keyVaultName
    location: location
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Key Vault Secrets officer'
      }
    ]
    secrets: [
    ]
  }
}
