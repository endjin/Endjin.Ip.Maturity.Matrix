# Settings for the 'prod' environment of the IMM Host.
# The subscription and tenant IDs come from environment variables, so that this repository does not contain them.
# Set these variables before you run './deploy.ps1 -EnvironmentName prod':
#   $env:AZURE_SUBSCRIPTION_ID = '<subscription-id>'
#   $env:AZURE_TENANT_ID = '<tenant-id>'
@{
    azureSubscriptionId = $env:AZURE_SUBSCRIPTION_ID
    azureTenantId = $env:AZURE_TENANT_ID
    azureLocation = 'uksouth'
    # The deployment identity has rights on this resource group only (endjin/endjin-identity-ops#57)
    resourceGroupName = 'rg-imm-prod'

    # The suffix makes the names of the Function App and the storage account unique in Azure
    functionAppName = 'func-imm-prod-s1k8s'
    storageAccountName = 'stimmprods1k8s'
    appInsightsName = 'appi-imm-prod-s1k8s'
}
