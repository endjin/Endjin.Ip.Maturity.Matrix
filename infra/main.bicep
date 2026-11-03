// Provisions the IMM Host: a .NET 10 isolated Function App on the Flex Consumption plan.
// The deployment process (deploy.ps1) runs this template, and then deploys the app's code.
// The settings follow the guidance in the README's 'Deploying to Azure Functions Flex Consumption' section.

@description('The Azure region for all the resources.')
param location string = resourceGroup().location

@description('The name of the Function App. The Flex Consumption plan has the same name.')
param functionAppName string

@description('The name of the storage account for the app\'s host storage and deployment packages.')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('The name of the Application Insights resource and its Log Analytics workspace.')
param appInsightsName string

@description('The memory size of each instance, in MB. Smaller instances get less CPU, so cold starts are slower.')
@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int = 2048

@description('The maximum number of instances that the app scales out to.')
param maximumInstanceCount int = 40

@description('Extra app settings, for example the workarounds in the README\'s Troubleshooting section. The deployment replaces all the app settings, so it removes any setting made by hand. If a key is also set by this template, the template\'s value is used.')
param additionalAppSettings object = {}

var deploymentContainerName = 'app-package'
var identityName = 'id-${functionAppName}'

// The roles that the app's managed identity needs on the storage account, for its host storage and deployment packages
var storageRoleDefinitionIds = [
  'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner
  '974c5e8b-45b9-4653-ba55-5f855dd0fb88' // Storage Queue Data Contributor
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor
]

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowSharedKeyAccess: false
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }

  resource blobService 'blobServices' = {
    name: 'default'

    resource deploymentContainer 'containers' = {
      name: deploymentContainerName
    }
  }
}

// The app uses a user-assigned identity, so that:
// - its storage role assignments exist before the app is created and its package is deployed.
// - recreating the app does not leave role assignments for an identity that no longer exists.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource storageRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleDefinitionId in storageRoleDefinitionIds: {
    name: guid(storage.id, identity.id, roleDefinitionId)
    scope: storage
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
      principalId: identity.properties.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: appInsightsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  // Create the app only when its identity can already use the storage account
  dependsOn: [
    storageRoleAssignments
  ]
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
    }
    // Flex Consumption sets the runtime here, not with the FUNCTIONS_WORKER_RUNTIME app setting
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identity.id
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
  }

  resource appSettings 'config' = {
    name: 'appsettings'
    properties: union(additionalAppSettings, {
      AzureWebJobsStorage__accountName: storage.name
      AzureWebJobsStorage__credential: 'managedidentity'
      AzureWebJobsStorage__clientId: identity.properties.clientId
      APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    })
  }
}

output functionAppName string = functionApp.name
output functionAppDefaultHostName string = functionApp.properties.defaultHostName
