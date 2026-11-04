# Settings that every environment of the IMM Host needs.
# The deployment process merges this file with the file for the target environment, for example 'dev.ps1'.
# The tenant ID comes from an environment variable, so that this repository does not contain it. Set it before you
# deploy: $env:AZURE_TENANT_ID = '<tenant-id>'
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

    azureTenantId = $env:AZURE_TENANT_ID
    azureLocation = 'uksouth'

    # Set IMM_SKIP_RBAC_OPERATIONS to 'true' to deploy without the Owner role. The template then skips its role assignments.
    skipRbacOperations = $env:IMM_SKIP_RBAC_OPERATIONS -eq 'true'
}
