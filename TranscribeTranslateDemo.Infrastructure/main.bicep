// targetScope = 'subscription'

@description('Location for all resources.')
param location string
param resourceName string

// resource ResourceGroup 'Microsoft.Resources/resourceGroups@2019-05-01' = {
//   name: resourceName
//   location: location  
// }

// module Resources './provisionResources.bicep' = {
//   name: '${resourceName}-ProvisionResources'
//   params: {
//     location: location
//     resourceName: resourceName
//   }
//   scope: ResourceGroup
// }














targetScope = 'subscription'

@description('Location for all resources.')
param location string = 'centralus'
param commonResourceName string = 'GSNVSearchDemo'

resource ResourceGroup 'Microsoft.Resources/resourceGroups@2019-05-01' = {
  name: commonResourceName
  location: location
}

module Resources './provisionResources.bicep' = {
  name: '${commonResourceName}-ProvisionResources'
  params: {
    location: location
    commonResourceName: toLower(commonResourceName)
  }
  scope: ResourceGroup
}
