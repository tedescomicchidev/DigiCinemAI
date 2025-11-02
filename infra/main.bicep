param location string = resourceGroup().location
param environment string = 'dev'
param useAks bool = false
param prefix string = 'digicin'

var nameSuffix = toLower(environment)

resource analytics 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-${nameSuffix}-appi'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${prefix}-${nameSuffix}-law'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${prefix}-${nameSuffix}-sb'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

var topics = [
  'pitches'
  'assignments'
  'drafts'
  'factcheck'
  'copyedit'
  'package'
  'publish'
  'distribute'
  'analytics'
  'moderation'
  'errors'
]

resource busTopics 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = [for topic in topics: {
  parent: serviceBus
  name: topic
  properties: {
    enableBatchedOperations: true
    enablePartitioning: true
  }
}]

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${prefix}${nameSuffix}sa'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: '${prefix}-${nameSuffix}-cosmos'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    enableFreeTier: true
    enableAnalyticalStorage: true
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${prefix}-${nameSuffix}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

resource containerEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${prefix}-${nameSuffix}-aca'
  location: location
  properties: {
    daprAIConnectionString: analytics.properties.InstrumentationKey
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: listKeys(logAnalytics.id, logAnalytics.apiVersion).primarySharedKey
      }
    }
  }
}

resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: '${prefix}-${nameSuffix}-aoai'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

output serviceBusConnection string = listKeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', serviceBus.name, 'RootManageSharedAccessKey'), '2017-04-01').primaryConnectionString
output storageAccountName string = storage.name
output appInsightsConnection string = analytics.properties.InstrumentationKey
