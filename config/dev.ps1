# Settings for the 'dev' environment of the IMM Host.
# The subscription ID comes from an environment variable, so that this repository does not contain it. Each environment
# has its own variable, so that a shell set up for one environment cannot deploy another one to the wrong subscription.
# Set it before you run './deploy.ps1 -EnvironmentName dev':
#   $env:IMM_DEV_SUBSCRIPTION_ID = '<subscription-id>'
# In GitHub Actions, AZURE_SUBSCRIPTION_ID is used instead. It comes from the 'dev' GitHub environment.
@{
    azureSubscriptionId = $env:IMM_DEV_SUBSCRIPTION_ID ?? ($env:GITHUB_ACTIONS -eq 'true' ? $env:AZURE_SUBSCRIPTION_ID : $null)
    resourceGroupName = 'rg-imm-dev'

    # The suffix makes the names of the Function App and the storage account unique in Azure
    functionAppName = 'func-imm-dev-mqssz'
    storageAccountName = 'stimmdevmqssz'
    appInsightsName = 'appi-imm-dev-mqssz'

    # Each deployment sets these values, and replaces any value set by hand. To work around the worker crash in the
    # README's Troubleshooting section, set 'instanceMemoryMB' to 4096, or add WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED = '0'
    # to 'additionalAppSettings'.
    instanceMemoryMB = 2048
    additionalAppSettings = @{}
}
