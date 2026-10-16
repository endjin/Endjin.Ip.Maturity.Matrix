$zerofailedExtensions = @(
    @{
        # References the extension from its GitHub repository. If not already installed, use latest version from 'main' will be downloaded.
        Name = "ZeroFailed.Build.DotNet"
        GitRepository = "https://github.com/zerofailed/ZeroFailed.Build.DotNet"
        GitRef = "main"
    }
)
. ZeroFailed.tasks -ZfPath $here/.zf

#
# Build process control options
#
$SkipInit = $false
$SkipVersion = $false
$SkipBuild = $false
$CleanBuild = $Clean
$SkipTest = $false
$SkipTestReport = $false
$SkipAnalysis = $false
$SkipPackage = $false

#
# Build process configuration
#
$SolutionToBuild = (Resolve-Path (Join-Path $here ".\Solutions\Endjin.Ip.Maturity.Model.slnx")).Path

# The Azure Functions app is published as a deployable artifact; nothing in this solution is shipped as a NuGet package.
# It is published for Azure Functions Flex Consumption, which runs on Linux x64 only, to
# _packages/Endjin.Ip.Maturity.Model.Host/linux-x64/. These settings belong here rather than in the Host's .csproj,
# because the Host's test and benchmark projects reference it and must keep building for the machine they run on.
$ProjectsToPublish = @(
    @{
        Project            = "Solutions/Endjin.Ip.Maturity.Model.Host/Endjin.Ip.Maturity.Model.Host.csproj"
        RuntimeIdentifiers = @("linux-x64")   # Flex Consumption is Linux x64 only
        SelfContained      = $false           # the Flex worker image supplies the .NET 10 runtime
        ReadyToRun         = $true            # precompile to native code to cut JIT time on cold start
        Trimmed            = $false
        SingleFile         = $false
    }
)
$SkipNuGetPackages = $true
$NuSpecFilesToPackage = @(
    # "Solutions/MySolution/MyProject/MyProject.nuspec"
)

$IncludeAssembliesInCodeCoverage = "Endjin*"
$ExcludeAssembliesInCodeCoverage = "Endjin*.Tests", "Endjin*.Benchmarks"

# Leave out code no test can reach, so the Host's coverage reflects the code we wrote: the Functions worker's
# source-generated types (under obj/.../Microsoft.Azure.Functions.Worker.Sdk.Generators/) and the Host's top-level
# startup in Program.cs, which only runs inside the Functions host.
$ExcludeFilesInCodeCoverage = "*Microsoft.Azure.Functions.Worker.Sdk.Generators*", "*Endjin.Ip.Maturity.Model.Host*Program.cs"

task . FullBuild

#
# Build Process Extensibility Points - uncomment and implement as required
#

# task RunFirst {}
# task PreInit {}
# task PostInit {}
# task PreVersion {}
# task PostVersion {}
# task PreBuild {}
# task PostBuild {}
# task PreTest {}
# task PostTest {}
# task PreTestReport {}
# task PostTestReport {}
# task PreAnalysis {}
# task PostAnalysis {}
# task PrePackage {}
# task PostPackage {}
# task PrePublish {}
# task PostPublish {}
# task RunLast {}
