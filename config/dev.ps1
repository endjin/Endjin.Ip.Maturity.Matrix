# Settings for the 'dev' environment of the IMM Host.
# The subscription and tenant IDs come from environment variables, so that this repository does not contain them.
# Set these variables before you run './deploy.ps1 -EnvironmentName dev':
#   $env:AZURE_SUBSCRIPTION_ID = '<subscription-id>'
#   $env:AZURE_TENANT_ID = '<tenant-id>'
@{
    azureSubscriptionId = $env:AZURE_SUBSCRIPTION_ID
    azureTenantId = $env:AZURE_TENANT_ID
    azureLocation = 'uksouth'
    resourceGroupName = 'rg-imm-dev'

    # The suffix makes the names of the Function App and the storage account unique in Azure
    functionAppName = 'func-imm-dev-mqssz'
    storageAccountName = 'stimmdevmqssz'
    appInsightsName = 'appi-imm-dev-mqssz'
}
