# Settings that every environment of the IMM Host needs.
# The deployment process merges this file with the file for the target environment, for example 'dev.ps1'.
@{
    RequiredConfiguration = @(
        'azureSubscriptionId'
        'azureTenantId'
        'azureLocation'
        'resourceGroupName'
        'functionAppName'
        'storageAccountName'
        'appInsightsName'
    )
}
