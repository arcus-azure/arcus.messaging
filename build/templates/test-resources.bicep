// Define the location for the deployment of the components.
param location string = resourceGroup().location

// Define the name of the single Azure Service bus namespace.
param serviceBusNamespace string

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
      name: 'Standard'
    }
    disableLocalAuth: false
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Azure Service Bus Data Owner'
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
