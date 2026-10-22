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
.PARAMETER FunctionAppPublishPath
    The folder that contains the published Function App. './build.ps1' publishes it to this folder by default.
.PARAMETER PackagesDir
    The output path for the deployment package.
.PARAMETER ZfModulePath
    The path to import the ZeroFailed module from. This is useful when testing pre-release
    versions of ZeroFailed that are not yet available in the PowerShell Gallery.
.PARAMETER ZfModuleVersion
    The version of the ZeroFailed module to import. This is useful when testing pre-release
    versions of ZeroFailed that are not yet available in the PowerShell Gallery.
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
    [string] $EnvironmentConfigName = $EnvironmentName,

    [Parameter()]
    [string] $FunctionAppPublishPath = "_packages/Endjin.Ip.Maturity.Model.Host/linux-x64",

    [Parameter()]
    [string] $PackagesDir = "_packages",

    [Parameter()]
    [string] $ZfModulePath,

    [Parameter()]
    [string] $ZfModuleVersion = "1.0.7",

    [Parameter()]
    [version] $InvokeBuildModuleVersion = "5.14.23"
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $PSCommandPath

#region InvokeBuild setup
# This handles calling the build engine when this file is run like a normal PowerShell script
# (i.e. avoids the need to have another script to setup the InvokeBuild environment and issue the 'Invoke-Build' command )
if ($MyInvocation.ScriptName -notlike '*Invoke-Build.ps1') {
    Install-PSResource InvokeBuild -Version $InvokeBuildModuleVersion -Scope CurrentUser -TrustRepository -Verbose:$false | Out-Null
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
Import-Module Microsoft.PowerShell.PSResourceGet
if (!($ZfModulePath)) {
    Install-PSResource ZeroFailed -Version $ZfModuleVersion -Scope CurrentUser -TrustRepository | Out-Null
    $ZfModulePath = "ZeroFailed"
    $splat.Add("RequiredVersion", ($ZfModuleVersion -split '-')[0])
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

# Load the deployment configuration
. $here/.zf/deploy-config.ps1
