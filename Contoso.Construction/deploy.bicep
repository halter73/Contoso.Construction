param sqlUsername string
param sqlPassword string
param resourceBaseName string
param currentUserObjectId string
param apimPublisherEmail string
param apimPublisherName string

resource sqlServer 'Microsoft.Sql/servers@2014-04-01' ={
  name: '${resourceBaseName}srv'
  location: resourceGroup().location
  properties: {
    administratorLogin: sqlUsername
    administratorLoginPassword: sqlPassword
  }
}

resource sqlFirewallRules 'Microsoft.Sql/servers/firewallRules@2014-04-01' = {
  parent: sqlServer
  name: 'dbfirewallrules'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlServerDatabase 'Microsoft.Sql/servers/databases@2014-04-01' = {
  parent: sqlServer
  name: '${resourceBaseName}db'
  location: resourceGroup().location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    edition: 'Basic'
    maxSizeBytes: '2147483648'
    requestedServiceObjectiveName: 'Basic'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: '${resourceBaseName}kv'
  location: resourceGroup().location
  properties: {
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: true
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: currentUserObjectId
        permissions: {
          keys: [
            'all'
          ]
          secrets: [
            'all'
          ]
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
  }
}

resource sqlSecret 'Microsoft.KeyVault/vaults/secrets@2021-06-01-preview' = {
  parent: keyVault
  name: 'ConnectionStrings--AzureSqlConnectionString'
  properties: {
    value: 'Data Source=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433; Initial Catalog=${resourceBaseName}db;User Id=${sqlUsername};Password=${sqlPassword};'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: '${resourceBaseName}strg'
  location: resourceGroup().location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource storageBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  name: format('{0}/default/uploads', storage.name)
  dependsOn: [
    storage
  ]
  properties: {
    publicAccess: 'Blob'
  }
}

resource storageSecret 'Microsoft.KeyVault/vaults/secrets@2021-06-01-preview' = {
  parent: keyVault
  name: 'AzureStorageConnectionString'
  properties: {
    value: format('DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${listKeys(storage.name, storage.apiVersion).keys[0].value};EndpointSuffix=core.windows.net')
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: '${resourceBaseName}hostingplan'
  location: resourceGroup().location
  sku: {
    name: 'F1'
    capacity: 1
  }
}

resource webApp 'Microsoft.Web/sites@2018-11-01' = {
  name: '${resourceBaseName}web'
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'VaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development'
        }
      ]
    }
  }
}

resource webAppAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2019-09-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

resource apiManagement 'Microsoft.ApiManagement/service@2020-12-01' = {
  name: '${resourceBaseName}apis'
  location: resourceGroup().location
  sku: {
    capacity: 0
    name: 'Consumption'
  }
  properties: {
    virtualNetworkType: 'None'
    publisherEmail: apimPublisherEmail
    publisherName: apimPublisherName
  }
}

resource api 'Microsoft.ApiManagement/service/apis@2020-12-01' = {
  parent: apiManagement
  name: 'job-site-survey-app-api'
  properties: {
    format: 'swagger-link-json'
    value: 'https://sitephotoapi.azurewebsites.net/Contoso.JobSiteAppApi.json'
    subscriptionRequired: false
    displayName: 'Job Site Survey App API'
    path: '/jobsitesapp'
    serviceUrl: 'https://${webApp.properties.defaultHostName}'
    protocols: [
      'http'
      'https'
    ]
  }
}
