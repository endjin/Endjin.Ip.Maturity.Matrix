$zerofailedExtensions = @(
    @{
        # Deploying to the Flex Consumption plan needs a version of this extension with zerofailed/ZeroFailed.Deploy.Azure#23.
        # TODO: Change 'GitRef' to a release tag when that version is released.
        Name = "ZeroFailed.Deploy.Azure"
        GitRepository = "https://github.com/zerofailed/ZeroFailed.Deploy.Azure"
        GitRef = "main"
    }
)
. ZeroFailed.tasks -ZfPath $here/.zf

#
# Deployment process configuration
#
# The environment configuration is read from the 'config' folder, which is ZeroFailed's default. To use another
# folder, set the ZF_DEPLOY_ENVIRONMENT_CONFIG_PATH environment variable.

# Provision the Function App, its Flex Consumption plan, its storage account and Application Insights.
# The other configuration settings are passed to the template as parameters with the same names.
$RequiredArmDeployments = @(
    @{
        templatePath = "$here/infra/main.bicep"
        resourceGroupName = { $DeploymentConfig.resourceGroupName }
        location = { $DeploymentConfig.azureLocation }
        configKeysToIgnore = @(
            'RequiredConfiguration'
            'azureLocation'
            'azureSubscriptionId'
            'azureTenantId'
            'resourceGroupName'
        )
    }
)

# Deploy the Function App. The task detects the Flex Consumption plan and waits for the deployment to finish.
$FunctionAppPublishPath = [IO.Path]::GetFullPath($FunctionAppPublishPath, $here)
$FunctionAppZipPath = [IO.Path]::GetFullPath((Join-Path $PackagesDir 'function-app.zip'), $here)
$AppServiceAppsToDeploy = @(
    @{
        appServiceName = { $DeploymentConfig.functionAppName }
        resourceGroupName = { $DeploymentConfig.resourceGroupName }
        zipPackagePath = $FunctionAppZipPath
        # The package is ready to run (linux-x64, ReadyToRun), so the platform must not build it
        remoteBuild = $false
    }
)

task . FullDeployment

#
# Deployment Process Extensibility Points - uncomment and implement as required
#

# task RunFirst {}
# task PreInit {}
# task PostInit {}
# task PreProvision {}
# task PostProvision {}

# Synopsis: Zip the published Function App, including the hidden '.azurefunctions' folder that the isolated worker needs
task PreDeploy {
    if (!(Test-Path $FunctionAppPublishPath)) {
        throw "The published Function App is not in '$FunctionAppPublishPath'. Run './build.ps1' first."
    }

    # Compress-Archive leaves out hidden files, so use the .NET API instead
    New-Item -ItemType Directory -Path (Split-Path -Parent $FunctionAppZipPath) -Force | Out-Null
    Remove-Item -Path $FunctionAppZipPath -Force -ErrorAction Ignore
    [IO.Compression.ZipFile]::CreateFromDirectory($FunctionAppPublishPath, $FunctionAppZipPath)
    Write-Build Green "Created the deployment package: $FunctionAppZipPath"
}

# task PostDeploy {}
# task PreTest {}

# Synopsis: Check that the deployed Function App renders an IMM badge
task PostTest {
    # The Bicep template outputs the host name
    $hostName = $ZF_ArmDeploymentOutputs.functionAppDefaultHostName
    if (!$hostName) {
        throw "The ARM deployment did not output 'functionAppDefaultHostName'. The smoke test needs the Provision stage to run."
    }
    # The README uses this repo for its example badges
    $badgeUri = "https://$hostName/api/imm/github/ais-dotnet/Ais.Net/total"
    Write-Build White "Requesting a badge: $badgeUri"

    # The first request after a deployment waits for a cold start, so retry for about 3 minutes.
    # Network errors (DNS, TLS, connection resets and timeouts) are retried too.
    $maxAttempts = 18
    $response = $null
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $badgeUri -SkipHttpErrorCheck -TimeoutSec 30
            $result = "HTTP $($response.StatusCode)"
        }
        catch {
            $response = $null
            $result = "request failed: $($_.Exception.Message)"
        }
        if ($response.StatusCode -eq 200) {
            break
        }
        if ($attempt -lt $maxAttempts) {
            Write-Build White "Attempt $($attempt): $result. Retrying in 10 seconds..."
            Start-Sleep -Seconds 10
        }
    }

    if ($response.StatusCode -ne 200) {
        throw "The badge request failed after $maxAttempts attempts ($result): $badgeUri"
    }
    if ($response.Headers['Content-Type'] -notlike 'image/svg+xml*') {
        throw "The badge request returned the content type '$($response.Headers['Content-Type'])' instead of 'image/svg+xml'."
    }
    if ($response.Headers['X-Imm-Error']) {
        Write-Warning "The Function App returned an error badge: $($response.Headers['X-Imm-Error'])"
    }
    Write-Build Green "The Function App returned an IMM badge"
}

# task RunLast {}
