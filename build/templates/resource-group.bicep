// Define the name of the resource group.
param resourceGroupName string

// Define the location for the deployment of the components.
param location string

targetScope='subscription'

module resourceGroup 'br/public:avm/res/resources/resource-group:0.2.3' = {
  name: 'resourceGroupDeployment'
  params: {
    name: resourceGroupName
    location: location
  }
}
