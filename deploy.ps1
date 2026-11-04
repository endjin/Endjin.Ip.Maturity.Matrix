#Requires -Version 7
<#
.SYNOPSIS
    Runs the deployment process for the IMM Host.
.DESCRIPTION
    This script was scaffolded using a template from the ZeroFailed project.
    It uses the InvokeBuild module to orchestrate the deployment process: it provisions the Azure resources with
    Bicep, and then deploys the Function App that './build.ps1' publishes.
.EXAMPLE
    PS C:\> ./deploy.ps1 -EnvironmentName dev
    Downloads any missing module dependencies (ZeroFailed & InvokeBuild) and deploys to the 'dev' environment.
.PARAMETER Tasks
    Optionally override the default task executed as the entry-point of the deployment.
.PARAMETER EnvironmentName
    The name of the target environment. It matches a configuration file in the 'config' folder.
.PARAMETER EnvironmentConfigName
    The name of the configuration file for the target environment, when it differs from the environment name.
    When it is not set, the ZF_DEPLOY_ENVIRONMENT_CONFIG_NAME environment variable is used if it is set.
.PARAMETER PackagesDir
    The output path for the deployment package. It is also the folder that contains the published Function App,
    unless FunctionAppPublishPath is set. Use the same value as for './build.ps1'.
.PARAMETER FunctionAppPublishPath
    The folder that contains the published Function App. By default, it is the folder in PackagesDir that
    './build.ps1' publishes the app to.
.PARAMETER ZfModulePath
    The path to import the ZeroFailed module from, instead of installing it. This is useful when testing a local
    copy of ZeroFailed.
.PARAMETER ZfModuleVersion
    The version of the ZeroFailed module to install and import.
.PARAMETER ZfModuleRepository
    The PowerShell repository to install the ZeroFailed module from: a registered repository name, or a URL. For a
    URL, a repository that is already registered with that URL (and any credentials it has) is used. Otherwise, the
    URL is registered as a new repository. The default is the ZeroFailed GitHub Packages feed. GitHub Packages needs
    a token, even for public packages: when the GITHUB_TOKEN environment variable is set (as it is in GitHub Actions),
    it is used as the credential. If the module cannot be installed from this repository, it is installed from PSGallery.
.PARAMETER ZfExtensionsLockMode
    How the ZeroFailed extensions lock file is used. 'Write' resolves the extensions and updates the lock file.
    'Frozen' installs the versions in the lock file, and fails if the extensions differ from it. 'Off' does not
    use the lock file. Defaults to 'Frozen' in GitHub Actions, and to 'Write' elsewhere. The
    ZF_EXTENSIONS_LOCK_MODE environment variable takes precedence over this parameter.
.PARAMETER InvokeBuildModuleVersion
    The version of the InvokeBuild module to be used.
#>
[CmdletBinding()]
param (
    [Parameter(Position=0)]
    [string[]] $Tasks = @("."),

    [Parameter(Mandatory)]
    [string] $EnvironmentName,

    [Parameter()]
    [string] $EnvironmentConfigName,

    [Parameter()]
    [string] $PackagesDir = "_packages",

    [Parameter()]
    [string] $FunctionAppPublishPath = (Join-Path $PackagesDir "Endjin.Ip.Maturity.Model.Host/linux-x64"),

    [Parameter()]
    [string] $ZfModulePath,

    [Parameter()]
    [string] $ZfModuleVersion = "2.0.0",

    [Parameter()]
    [string] $ZfModuleRepository = "https://nuget.pkg.github.com/zerofailed/index.json",

    [Parameter()]
    [ValidateSet("Write","Frozen","Off")]
    [string] $ZfExtensionsLockMode = ($env:GITHUB_ACTIONS -eq "true" ? "Frozen" : "Write"),

    [Parameter()]
    [version] $InvokeBuildModuleVersion = "5.14.23"
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $PSCommandPath

#region InvokeBuild setup
# This handles calling the build engine when this file is run like a normal PowerShell script
# (i.e. avoids the need to have another script to setup the InvokeBuild environment and issue the 'Invoke-Build' command )
if ($MyInvocation.ScriptName -notlike '*Invoke-Build.ps1') {
    if (!(Get-InstalledPSResource InvokeBuild -Version $InvokeBuildModuleVersion -ErrorAction Ignore)) {
        # InvokeBuild is only published to PSGallery, which sometimes fails, so retry the install
        for ($attempt = 1; ; $attempt++) {
            try {
                Install-PSResource InvokeBuild -Version $InvokeBuildModuleVersion -Scope CurrentUser -TrustRepository -Repository PSGallery
                break
            }
            catch {
                if ($attempt -ge 3) { throw }
                Write-Warning "Could not install InvokeBuild $InvokeBuildModuleVersion (attempt $attempt of 3), so retrying: $($_.Exception.Message)"
                Start-Sleep -Seconds (5 * $attempt)
            }
        }
    }
    try {
        Invoke-Build $Tasks $MyInvocation.MyCommand.Path @PSBoundParameters
    }
    catch {
        Write-Host -f Yellow "`n`n***`n*** Deployment Failure Summary - check previous logs for more details`n***"
        Write-Host -f Yellow $_.Exception.Message
        Write-Host -f Yellow $_.ScriptStackTrace
        exit 1
    }
    return
}
#endregion

#region Initialise build framework
$splat = @{ Force = $true; Verbose = $false}
if (!($ZfModulePath)) {
    if (!(Get-InstalledPSResource ZeroFailed -Version $ZfModuleVersion -ErrorAction Ignore)) {
        # Support a repository URL, re-using any existing registration for it (and its credentials)
        if ($ZfModuleRepository -match '://') {
            $uri = $ZfModuleRepository
            $ZfModuleRepository = (Get-PSResourceRepository | ? Uri -eq $uri | Select-Object -First 1).Name ?? ($uri -replace '\W','_')
            $null = Get-PSResourceRepository $ZfModuleRepository -ErrorAction Ignore || Register-PSResourceRepository $ZfModuleRepository -Uri $uri
        }
        $installSplat = @{
            Name = 'ZeroFailed'
            Version = $ZfModuleVersion
            Repository = $ZfModuleRepository
            Scope = 'CurrentUser'
            TrustRepository = $true
        }
        # GitHub Packages needs a token to install any package, even a public one. In GitHub Actions, the
        # workflow's token is in GITHUB_TOKEN.
        if ($env:GITHUB_TOKEN -and (Get-PSResourceRepository $ZfModuleRepository).Uri.Host -eq 'nuget.pkg.github.com') {
            $installSplat.Credential = [pscredential]::new('token', (ConvertTo-SecureString $env:GITHUB_TOKEN -AsPlainText -Force))
        }
        try {
            Install-PSResource @installSplat
        }
        catch {
            if ($ZfModuleRepository -eq 'PSGallery') { throw }
            Write-Warning "Could not install ZeroFailed $ZfModuleVersion from '$ZfModuleRepository', so trying PSGallery: $($_.Exception.Message)"
            $installSplat.Remove('Credential')
            $installSplat.Repository = 'PSGallery'
            Install-PSResource @installSplat
        }
    }
    # Import the module's manifest by its path. A package from GitHub Packages is installed in a lower-case
    # 'zerofailed' folder, which 'Import-Module ZeroFailed' does not find on a case-sensitive file system (Linux).
    $installed = Get-InstalledPSResource ZeroFailed -Version $ZfModuleVersion | Select-Object -First 1
    $ZfModulePath = Join-Path $installed.InstalledLocation $installed.Name ($ZfModuleVersion -split '-')[0] 'ZeroFailed.psd1'
    if (!(Test-Path $ZfModulePath)) {
        throw "The ZeroFailed module manifest is not in the expected location: $ZfModulePath"
    }
}
else {
    Write-Host "ZfModulePath: $ZfModulePath"
}
$splat.Add("Name", $ZfModulePath)
# Ensure only 1 version of the module is loaded
Get-Module ZeroFailed | Remove-Module
Import-Module @splat
$ver = "{0} {1}" -f (Get-Module ZeroFailed).Version, (Get-Module ZeroFailed).PrivateData.PsData.PreRelease
Write-Host "Using ZeroFailed module version: $ver"
#endregion

$PSModuleAutoloadingPreference = 'none'

# ZeroFailed reads the extensions lock mode from this variable, unless ZF_EXTENSIONS_LOCK_MODE is set
$zerofailedExtensionsLockMode = $ZfExtensionsLockMode

# Load the deployment configuration
. $here/.zf/deploy/config.ps1
