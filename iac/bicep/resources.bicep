param basename string
param location string = 'westus2'
param failoverLocation string = 'eastus2'
param principalId string

var secrets = [
  'get'
  'set'
  'list'
  'delete'
]

resource storage 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: '${basename}storage'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource form_recognizer 'Microsoft.CognitiveServices/accounts@2017-04-18' = {
  name: '${basename}fr'
  location: location
  kind: 'FormRecognizer'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: '${basename}fr'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

resource text_analytics 'Microsoft.CognitiveServices/accounts@2017-04-18' = {
  name: '${basename}ta'
  location: location
  kind: 'TextAnalytics'
  sku: {
    name: 'S'
  }
  properties: {
    customSubDomainName: '${basename}ta'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

resource key_vault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: '${basename}kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        objectId: principalId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
      {
        objectId: aks.properties.identityProfile.kubeletidentity.objectId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
      {
        objectId: aks.identity.principalId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
      {
        objectId: function.identity.principalId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
    ]
  }

  resource cosmos_key_secret 'secrets' = {
    name: 'CosmosKey'
    properties: {
      value: listKeys(cosmos_account.id, cosmos_account.apiVersion).primaryMasterKey
    }
  }

  resource signalr_connection_string_secret 'secrets' = {
    name: 'SignalRConnectionString'
    properties: {
      value: listKeys(signalr.id, signalr.apiVersion).primaryConnectionString
    }
  }
}

resource cosmos_account 'Microsoft.DocumentDB/databaseAccounts@2020-04-01' = {
  name: '${basename}cosmosaccount'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        failoverPriority: 1
        locationName: failoverLocation
      }
      {
        failoverPriority: 0
        locationName: location
      }
    ]
  }

  resource cosmos_sqldb 'sqlDatabases' = {
    name: 'memealyzer'
    properties: {
      options: {
        throughput: 400
      }
      resource: {
        id: 'memealyzer'
      }
    }

    resource cosmos_sqldb_container 'containers' = {
      name: 'images'
      properties: {
        options: {
          throughput: 400
        }
        resource: {
          partitionKey: {
            paths: [
              '/partitionKey'
            ]
          }
          id: 'images'
          uniqueKeyPolicy: {
            uniqueKeys: [
              {
                paths: [
                  '/uid'
                ]
              }
            ]
          }
        }
      }
    }
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {
  name: '${basename}aks'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: '1.20.7'
    nodeResourceGroup: '${basename}aksnodes'
    dnsPrefix: '${basename}aks'

    agentPoolProfiles: [
      {
        name: 'default'
        count: 1
        vmSize: 'Standard_A2_v2'
        mode: 'System'
      }
    ]
  }
}

resource logging 'Microsoft.Insights/components@2015-05-01' = {
  name: '${basename}ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource appconfig 'Microsoft.AppConfiguration/configurationStores@2020-06-01' = {
  name: '${basename}appconfig'
  location: location
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }

  resource appconfig_borderstyle 'keyValues@2020-07-01-preview' = {
    name: 'borderStyle'
    properties: {
      value: 'solid'
    }
  }
}

resource signalr 'Microsoft.SignalRService/signalR@2020-07-01-preview' = {
  name: '${basename}signalr'
  location: location
  sku: {
    name: 'Standard_S1'
    capacity: 1
  }
  properties: {
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
      }
    ]
  }
}

resource plan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: '${basename}plan'
  location: location
  sku: {
    tier: 'Standard'
    size: 'S1'
    name: 'S1'
  }
}

resource function 'Microsoft.Web/sites@2020-06-01' = {
  name: '${basename}function'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      alwaysOn: true
      cors: {
        allowedOrigins: [
          '*'
        ]
        supportCredentials: false
      }
      ftpsState: 'FtpsOnly'
    }
    httpsOnly: true
  }

  resource function_app_settings 'config@2018-11-01' = {
    name: 'appsettings'
    properties: {
      'AzureWebJobsStorage__accountName': storage.name
      'AzureSignalRConnectionString': '@Microsoft.KeyVault(VaultName=${key_vault.name};SecretName=${key_vault::signalr_connection_string_secret.name})'
      'ServiceBusConnection__fullyQualifiedNamespace': '${service_bus.name}.servicebus.windows.net'
      'StorageConnection__queueServiceUri': storage.properties.primaryEndpoints.queue
      'APPINSIGHTS_INSTRUMENTATIONKEY': logging.properties.InstrumentationKey
      'FUNCTIONS_WORKER_RUNTIME': 'dotnet'
      'FUNCTIONS_EXTENSION_VERSION': '~3'
      'WEBSITES_ENABLE_APP_SERVICE_STORAGE': 'false'
      'WEBSITE_RUN_FROM_PACKAGE': ''
      'BASENAME': basename
    }
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2019-12-01-preview' = {
  name: '${basename}acr'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource service_bus 'Microsoft.ServiceBus/namespaces@2017-04-01' = {
  name: '${basename}sb'
  location: location
  sku: {
    name: 'Basic'
  }

  resource messages 'queues' = {
    name: 'messages'
    properties: {
      defaultMessageTimeToLive: 'PT30S'
    }
  }

  resource sync 'queues' = {
    name: 'sync'
    properties: {
      defaultMessageTimeToLive: 'PT30S'
    }
  }
}

module cli_perms './rolesapp.bicep' = {
  name: 'cli_perms-${resourceGroup().name}'
  params: {
    principalId: principalId
    principalType: 'User'
    resourceGroupName: resourceGroup().name
  }
}

module function_perms './rolesapp.bicep' = {
  name: 'function_perms-${resourceGroup().name}'
  params: {
    principalId: function.identity.principalId
    resourceGroupName: resourceGroup().name
  }
}

module aks_kubelet_perms './rolesapp.bicep' = {
  name: 'aks_kubelet_perms-${resourceGroup().name}'
  params: {
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    resourceGroupName: resourceGroup().name
  }
}

module aks_cluster_perms './rolesacr.bicep' = {
  name: 'aks_cluster_perms-${resourceGroup().name}'
  params: {
    principalId: aks.identity.principalId
    resourceGroupName: resourceGroup().name
  }
}
