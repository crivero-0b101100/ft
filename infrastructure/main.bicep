param location string = resourceGroup().location
param environment string = 'dev'
param aksClusterName string = 'foodtracker-aks'
param cosmosDbName string = 'foodtracker-db'

// Cosmos DB Account
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbName
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
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    enableFreeTier: true
    enableAutomaticFailover: false
    minimalTlsVersion: 'Tls12'
  }
}

// AKS Cluster
resource aks 'Microsoft.ContainerService/managedClusters@2023-11-01' = {
  name: aksClusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: '1.28.5'
    dnsPrefix: aksClusterName
    agentPoolProfiles: [
      {
        name: 'agentpool'
        count: 1
        vmSize: 'Standard_B2s'  // Cheapest VM size with 2 vCPUs
        osType: 'Linux'
        mode: 'System'
        enableAutoScaling: false
        osDiskSizeGB: 30
        type: 'VirtualMachineScaleSets'
      }
    ]
    networkProfile: {
      networkPlugin: 'kubenet'
      serviceCidr: '10.0.0.0/16'
      dnsServiceIP: '10.0.0.10'
    }
    addonProfiles: {
      httpApplicationRouting: {
        enabled: false
      }
    }
  }
}

// Outputs
output cosmosDbConnectionString string = listKeys('${cosmosDb.id}/listKeys', cosmosDb.apiVersion).primaryMasterKey
output aksClusterName string = aks.name 