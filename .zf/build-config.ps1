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
$SolutionToBuild = (Resolve-Path (Join-Path $here ".\Solutions\Endjin.Ip.Maturity.Matrix.slnx")).Path

# The Azure Functions app is published as a deployable artifact; nothing in this solution is shipped as a NuGet package.
$ProjectsToPublish = @(
    "Solutions/Endjin.Ip.Maturity.Matrix.Host/Endjin.Ip.Maturity.Matrix.Host.csproj"
)
$SkipNuGetPackages = $true
$NuSpecFilesToPackage = @(
    # "Solutions/MySolution/MyProject/MyProject.nuspec"
)

$IncludeAssembliesInCodeCoverage = "Endjin*"
$ExcludeAssembliesInCodeCoverage = "Endjin*.Tests", "Endjin*.Benchmarks"

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
