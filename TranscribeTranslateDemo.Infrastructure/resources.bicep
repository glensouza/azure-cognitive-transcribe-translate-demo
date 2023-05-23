targetScope = 'resourceGroup'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Base Name of Resources')
param commonResourceName string = 'GSTransDemo'
var resourceName = toLower(commonResourceName)

var speechServiceName = '${resourceName}speech'
var speechServiceSKU = 'F0'

var logAnalyticsName = '${resourceName}log'
var logAnalyticsSKU = 'PerGB2018'
var applicationInsightsName = '${resourceName}insights'

var storageSKU = 'Standard_LRS'
var storageAccountName = '${resourceName}sa'
var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
var storageBlobDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '17d1049b-9a84-46fb-8f53-869881c3d3ab')

var appServicePlanFuncName = '${resourceName}funcasp'
var appServicePlanFuncSKU = 'Y1'
var appServicePlanFuncTier = 'Dynamic'
var functionAppName = '${resourceName}func'

var swaSku = 'Standard'
var webAppName = '${resourceName}swa'

resource speechService 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: speechServiceName
  location: location
  sku: {
    name: speechServiceSKU
  }
  kind: 'SpeechServices'
  identity: {
    type: 'None'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSKU
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
        queue: {
          enabled: true
        }
        table: {
          enabled: true
        }
      }
    }
  }
}

resource storageFunctionAppPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storageAccount.id, functionAppName, storageBlobDataContributorRole)
  scope: storageAccount
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRole
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: logAnalyticsSKU
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }

  resource logAnalyticsDataExport 'dataexports@2020-08-01' = {
    name: 'DataExport'
    properties: {
      dataExportId: '8493dffe-42b2-438e-90b9-e351a4960e18'
      destination: {
        resourceId: storageAccount.id
        metaData: {}
      }
      tableNames: [
        'Alert'
        'AppCenterError'
        'ComputerGroup'
        'InsightsMetrics'
        'Operation'
        'Usage'
      ]
      enable: true
      createdDate: '2023-05-05T15:57:53.6038946Z'
      lastModifiedDate: '2023-05-05T15:57:53.6038946Z'
    }
  }

  resource logAnalyticsAlerts 'linkedstorageaccounts@2020-08-01' = {
    name: 'Alerts'
    properties: {
      storageAccountIds: [
        storageAccount.id
      ]
    }
  }

  resource logAnalyticsCustomLogs 'linkedstorageaccounts@2020-08-01' = {
    name: 'CustomLogs'
    properties: {
      storageAccountIds: [
        storageAccount.id
      ]
    }
  }

  resource logAnalyticsQuery 'linkedstorageaccounts@2020-08-01' = {
    name: 'Query'
    properties: {
      storageAccountIds: [
        storageAccount.id
      ]
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource appServicePlanFunc 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanFuncName
  location: location
  sku: {
    name: appServicePlanFuncSKU
    tier: appServicePlanFuncTier
  }
  kind: 'functionapp'
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    serverFarmId: appServicePlanFunc.id
    siteConfig: {
      numberOfWorkers: 1
      functionAppScaleLimit: 200
      minimumElasticInstanceCount: 0
    }
    use32BitWorkerProcess: false
    httpsOnly: true
  }

  resource functionAppConfiguration 'config' = {
    name: 'appsettings'
    properties: {
      AzureWebJobsStorage: storageAccountConnectionString
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
      netFrameworkVersion: 'v6.0'
      managedPipelineMode: 'Integrated'
      FUNCTIONS_EXTENSION_VERSION: '~4'
      AzureSignalRConnectionString: 'Endpoint=https://${signalRService.name}.service.signalr.net;AccessKey=${signalRService.listKeys().primaryKey}'
      SpeechKey: speechService.listKeys().key1
      SpeechRegion: location
    }
    dependsOn: [storageFunctionAppPermissions]
  }  
}

resource staticWebApp 'Microsoft.Web/staticSites@2020-12-01' = {
  name: webAppName
  location: location
  sku: {
    name: swaSku
    size: swaSku
  }
  properties: {}

  resource staticWebAppAppSettings 'config' = {
    name: 'appsettings'
    properties: {
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
    }
  }

  resource staticSiteBackend 'linkedBackends@2022-09-01' = {
    name: 'swaBackend'
    properties: {
      backendResourceId: functionApp.id
      region: 'centralus'
    }
  }
}

resource signalRService 'Microsoft.SignalRService/SignalR@2020-05-01' = {
  name: '${commonResourceName}signalr'
  location: location
  sku: {
    name: 'Free_F1'
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
        properties: {
        }
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
    serverless: {
      connectionTimeoutInSeconds: 30
    }
  }
}
