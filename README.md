# IP Maturity Model

The aim of this framework is to measure maturity across the disciplines required to build, test, deploy, reuse, support & sell Intellectual Property. This flexible framework is based on a number of conventions to measure "quality", many of these measures are subjective, rather than qualitative, and need to reviewed by people rather than automated by tools.

Our IP exists at different levels of fidelity:

| Level | Type                          |
|-------|-------------------------------|
| 0     | Script / Template / Code file |
| 1     | Component                     |
| 2     | Tool                          |
| 3     | HTTP APIs                     |
| 4     | Solution                      |
| 5     | Product                       |

Each of these has different nuances when it comes to the categories in the IP Maturity Model outlined below. 

## Categories

We have, collectively, defined the following categories as quality measures for our IP. In many ways these measures can be thought of as a more traditional "definition of done". These quality measures cover different aspects of the lifecycle of IP, from maintainance to adoption, to operational support. It also includes commercial and legal concerns.

- [IP Maturity Model](#ip-maturity-model)
  - [Categories](#categories)
    - [Shared Engineering Standards](#shared-engineering-standards)
    - [Coding Standards](#coding-standards)
    - [Executable Specifications](#executable-specifications)
    - [Coverage](#coverage)
    - [Benchmarks](#benchmarks)
    - [Reference Documentation](#reference-documentation)
    - [Design \& Implementation Documentation](#design--implementation-documentation)
    - [How-to Documentation](#how-to-documentation)
    - [Demos](#demos)
    - [Date of Last IP Review](#date-of-last-ip-review)
    - [Framework Version](#framework-version)
    - [Associated Work Items](#associated-work-items)
    - [Source Code Availability](#source-code-availability)
    - [License](#license)
    - [Production Use](#production-use)
    - [Insights](#insights)
    - [Packaging](#packaging)
    - [Deployment](#deployment)
    - [Ops](#ops)
  - [The IMM Schema](#the-imm-schema)
  - [How to use the IP Maturity Model](#how-to-use-the-ip-maturity-model)
    - [Total Score](#total-score)
    - [IMM Measures](#imm-measures)
  - [Building](#building)
  - [Licenses](#licenses)
  - [Project Sponsor](#project-sponsor)
  - [Code of conduct](#code-of-conduct)

### Shared Engineering Standards

We have developed a standardised set of configuration files for projects and IDEs. Using these creates a pit of quality for internal and external developers to follow.

| Score | Measure    |
|-------|------------|
| 0     | None       |
| +1    | Configured |

### Coding Standards

We have agreed upon coding standards for our primary languages, and in most languages these are enforced by linters or other code analysis tools.

| Score | Measure              |
|-------|----------------------|
| 0     | None                 |
| +1    | Enforced via tooling |

### Executable Specifications

Executable Specifications are our fundamental design tool; whether via Gherkin or OpenAPI, it creates a shared understanding of behaviour, and a common domain language. 

| Score | Measure                                                           |
|-------|-------------------------------------------------------------------|
| 0     | None                                                              |
| +1    | Specs which cover golden path / APIs have full OpenAPI Definition |
| +1    | Specs which cover common failure cases                            |
| +1    | Specs which explore edge cases                                    |

### Coverage

Code coverage should be used as a ancillary measure of how well we have written our Executable Specifications. We could expect a similar score across both categories. A discrepancy between scored requires further inspection & analysis.

| Score | Measure |
|-------|---------|
| 0     | 0-25    |
| +1    | +26-50  |
| +1    | 51-75   |
| +1    | 75+     |

### Benchmarks

We are commonly asked to write high performance / low latency code; understanding the performance characteristics from a memory and CPU perspective is vital as production performance issues have big reputational impact.

| Score | Measure                                         |
|-------|-------------------------------------------------|
| 0     | None                                            |
| +1    | Benchmarks which cover baseline performance     |
| +1    | Benchmarks which demonstrate failure conditions |

### Reference Documentation

Code should be self-documenting, but long term support of maintaince of code requires context and narrative as well as purpose.

| Score | Measure                  |
|-------|--------------------------|
| 0     | None                     |
| +1    | Good quality             |
| +1    | Technical Fellow quality |

### Design & Implementation Documentation

One of our constant failings is under valuing the thought and effort that goes into creating solutions, and capturing all that knowledge in a format we can use to take customers along on the journey. 

| Score | Measure                                                         |
|-------|-----------------------------------------------------------------|
| 0     | None                                                            |
| +1    | Up-to-date architecture & high level conceptual docs & diagrams |
| +1    | Logical, infrastructure, security & ops views                   |
| +1    | Constraints & extensibility (obtained from benchmarks & specs)  |

### How-to Documentation

The use of IP can be nuanced. Effective documentation allows users to be self-starters. Questions should be captured as FAQs.

| Score | Measure                 |
|-------|-------------------------|
| 0     | None                    |
| +1    | Common scenarios        |
| +1    | Extensibility Scenarios |
| +1    | FAQs / Troubleshooting  |

### Demos

Understanding how to get started with our IP, or how different elements of IP can be used together is fundamental for increased producivity.

| Score | Measure                                |
|-------|----------------------------------------|
| 0     | None                                   |
| +1    | Common scenarios                       |
| +1    | Extensibility Scenarios                |
| +1    | End to end integration (with other IP) |

### Date of Last IP Review

How recently IP was reviewed is a powerful code smell. We exist at the bleeding edge, which means that change is a constant. We need to be vigilant to change, especially when we depend on cloud PaaS services that can change beneath our feet.

| Score | Measure    |
|-------|------------|
| 0     | Unknown    |
| 1     | > 3 months |
| 2     | > 1 month  |
| 3     | < 1 month  |

### Framework Version

The majority of our IP is based on the .NET Framework. This is now a rapidly evolving ecosystem. Staying up to date is no small feat.

| Score | Measure                                     |
|-------|---------------------------------------------|
| 0     | Using an unsupported flavour of .NET        |
| +1    | Using a LTS version of a flavour of .NET    |
| +1    | Using the most current LTS flavour of .NET  |

### Associated Work Items

The number of associated work items is another code smell; it can signal either chaos or order. We need to distinguish between these two states.

| Score | Measure                 |
|-------|-------------------------|
| 0     | None                    |
| 1     | Bugs & Features         |
| 2     | Curated Bugs & Features |
| 3     | Active Roadmap          |

### Source Code Availability

One of the most overlooked aspects of our use of IP is supporting our contractual clauses, allowing customers to access the source code for the binaries we use. Historically, we have been approached 5 years after the actual engagement as part of acquisition / due dilligence processes.

| Score | Measure                     |
|-------|-----------------------------|
| 0     | None                        |
| 1     | Snapshot archive for escrow |
| 2     | Private OSS / Mirrored Repo |
| 3     | Public OSS                  |

### License

A foundation of our commercial success is establishing the licensing of our IP.

| Score | Measure                               |
|-------|---------------------------------------|
| 0     | None                                  |
| +1    | Copyright headers in each source file |
| +1    | License in Source & Packages          |
| +1    | Contributor license in Repo           |

### Production Use

A good measure of the quality of our IP is how many times it is being used in a production environment by our customers.

| Score | Measure                                 |
|-------|-----------------------------------------|
| 0     | None                                    |
| +1    | Accepted by a customer                  |
| +1    | In production use by a customer         |
| +1    | In production use by multiple customers |

### Insights

When things go wrong, we need the infrastucture in place to help us quickly resolve the situation.

| Score | Measure                                                           |
|-------|-------------------------------------------------------------------|
| 0     | None                                                              |
| +1    | Telemetry, Diagnostics & Debugging                                |
| +1    | Perf Counters                                                     |
| +1    | Operational Insights (Custom Queries defining abnormal behaviour) |

### Packaging

It's one thing to create reusable IP, it's entirely another for it to be in a form that easy to find and use.

| Score | Measure      |
|-------|--------------|
| 0     | None         |
| +1    | Packaged     |
| +1    | Versioned    |
| +1    | Discoverable |

### Deployment

The final hurdle is getting the IP into an environment where it can be used.

| Score | Measure                       |
|-------|-------------------------------|
| 0     | None                          |
| +1    | Scripted & Documented         |
| +1    | Templated                     |
| +1    | Multi-tenanted - as a Service |

### Ops

We need to consider other personas, whose funciton is to support the IP we create. How do we make their experience "delightful"?

| Score | Measure |
|-------|---------|
| 0     | TBD     |

## The IMM Schema

The IMM schema is designed to be very flexible as you may decide that you want to add or alter categories. The [current RuleSet](https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/Solutions/Endjin.Imm.App/RuleSet.yaml) is defined in the `Endjin.Imm.App` project in the solution.

Each rule needs a unique, guid based Id, a name, and a data type which can either be `Discrete` (meaning it can only be scored by a single item listed in the schema), or `Continuous` (meaning that the score can be cumulative). These two data types are used to select and run the correct rules engine to calculate the values to be used to render the badges.

## How to use the IP Maturity Model

Each project that adopts the IMM simply adds a `imm.yaml` file into the root of the repo. `Endjin.Ip.Maturity.Model.Host` is an Azure Function, which when given the path to the GitHub repo, will render the IMM Measure as a badge, which can be displayed in the repo and provide an "at a glance" view of the quality of the project. 

The Azure Function exposes a HTTP Trigger, which has two actions.

### Total Score

Call the the Function `api/imm/github/<Org_Name>/<Repo>/total?cache=false`

And it will render a badge - the example below is for the [AIS.NET Project](https://github.com/ais-dotnet/Ais.Net) (you may need to refresh the page to wake the Function up!)

[![IMM](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/total?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/total?cache=false)

### IMM Measures

To display a badge for each measure you need to call `api/imm/github/<Org_Name>/<Repo>/rule/<RuleId>?nocache=true`

Here are examples for the [AIS.NET Project](https://github.com/ais-dotnet/Ais.Net);

[![Shared Engineering Standards](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?nocache=true)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?cache=false)

[![Coding Standards](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)

[![Executable Specifications](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)

[![Code Coverage](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)

[![Benchmarks](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)

[![Reference Documentation](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)

[![Design & Implementation Documentation](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)

[![How-to Documentation](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)

[![Date of Last IP Review](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)

[![Framework Version](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)

[![Associated Work Items](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)

[![Source Code Availability](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)

[![License](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)

[![Production Use](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)

[![Packaging](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)

## Building

You will need the .NET 10 SDK; the version is pinned by `global.json` in the root of the repo. The solution, `Solutions/Endjin.Ip.Maturity.Model.slnx`, uses Central Package Management (`Solutions/Directory.Packages.props`) and shared build settings (`Solutions/Directory.Build.props`).

```console
dotnet build Solutions/Endjin.Ip.Maturity.Model.slnx
dotnet test --solution Solutions/Endjin.Ip.Maturity.Model.slnx
```

The full build process, which is the same one CI runs, uses [ZeroFailed](https://github.com/zerofailed/ZeroFailed). It needs PowerShell 7, and it downloads the ZeroFailed and InvokeBuild modules on first run:

```console
./build.ps1
```

It versions the build with GitVersion, then builds, tests, writes a code coverage report to `_codeCoverage/`, and publishes the Functions app to `_packages/`. Its configuration is in `.zf/build-config.ps1`.

There are three test projects. They are written with MSTest and [Shouldly](https://docs.shouldly.org/), and run on Microsoft.Testing.Platform. None of them uses the network: GitHub is replaced by a stub HTTP handler or by [NSubstitute](https://nsubstitute.github.io/) substitutes.

- `Solutions/Endjin.Imm.Tests` covers rule evaluation, YAML loading, and fetching and caching the rule set and IMMs from GitHub.
- `Solutions/Endjin.Badger.Tests` covers SVG badge rendering, including how far the widths of the fonts a browser falls back to without Verdana (DejaVu Sans, and Liberation Sans for Arial and Helvetica) are from the Verdana widths badges are sized with.
- `Solutions/Endjin.Ip.Maturity.Model.Host.Tests` covers the badge and `showbadges` functions, the startup warm-up, the spans they are traced with, and when telemetry is exported.

To collect code coverage and a TRX report as well:

```console
dotnet test --solution Solutions/Endjin.Ip.Maturity.Model.slnx --coverage --report-trx
```

`Endjin.Imm.App` is a console app that renders badges for the `imm-default.yaml` file in its working directory, which is handy for checking changes to the badge rendering.

Badge text is sized from a table of Verdana's advance widths and kerning (`Solutions/Endjin.Badger/VerdanaMetrics.g.cs`), so no fonts are needed on the server. The viewer's browser draws the text in the first font it has from `Verdana, Geneva, DejaVu Sans, sans-serif`, and each text has a `textLength` of its measured Verdana width, so whatever font is used fills exactly the width the badge was sized for. The tables are generated by `tools/GenerateFontMetrics.cs`, which also generates the DejaVu Sans and Liberation Sans tables the tests compare against (the font files themselves are never committed).

`Endjin.Ip.Maturity.Model.Host` is an Azure Function using the isolated worker model with ASP.NET Core integration. To run it locally, copy `local.settings.json.template` to `local.settings.json` and start it with the [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (`func start`, or `dotnet run`, which calls `func` for you). The template sets `FUNCTIONS_WORKER_RUNTIME` to `dotnet-isolated`, which the Core Tools need. In Azure, the Function App runs on Flex Consumption, which sets the runtime in a different way: see the next section.

### Deploying to Azure Functions Flex Consumption

The Host is designed to run on the [Flex Consumption plan](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan), which runs on Linux only.

#### Create the Function App

Create the app with the .NET 10 isolated runtime and 2048 MB instances. Put its storage account in the same region as the app.

```console
az functionapp create --resource-group <rg> --name <app> --storage-account <storage> \
  --flexconsumption-location <region> --runtime dotnet-isolated --runtime-version 10.0 \
  --instance-memory 2048
```

- Use 2048 MB instances. Do not use 512 MB: the CPU allocation scales with the memory size, and a 512 MB instance has a quarter of a core (a 2048 MB instance has one core), so cold starts and badge rendering are slower.
- Flex Consumption sets the runtime and its version in the app's `functionAppConfig`, not in app settings. Do not set these app settings, which Flex Consumption does not support: `FUNCTIONS_WORKER_RUNTIME`, `FUNCTIONS_EXTENSION_VERSION`, `WEBSITE_RUN_FROM_PACKAGE` and `WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT`. The site settings `alwaysOn`, `use32BitWorkerProcess` and `netFrameworkVersion` also do not apply.
- Do not set `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED`. By default, Flex Consumption starts .NET isolated apps from a pre-started placeholder, which makes cold starts faster.
- The Warmup trigger is not supported on Flex Consumption, so the Host does not use it. The Host warms itself up instead (see below).

#### Remove cold starts with always-ready instances

An app with no always-ready instances scales to zero, and the first request after that waits for a cold start. To keep one instance ready for HTTP requests at all times:

```console
az functionapp scale config always-ready set --resource-group <rg> --name <app> --settings http=1
```

You pay for always-ready instances while they are idle, so this is a trade-off between cost and latency. It is the only way to make sure that no request waits for a cold start.

#### Deploy the package

The build publishes the Host for `linux-x64` with [ReadyToRun](https://learn.microsoft.com/dotnet/core/deploying/ready-to-run) compilation, which precompiles the assemblies to native code so that less code is JIT compiled during a cold start. The publish settings are in `.zf/build-config.ps1`, not in the project file, so local builds and tests are not affected. The output is in `_packages/Endjin.Ip.Maturity.Model.Host/linux-x64/`, and CI uploads it as the `function-app` artifact. CI also runs `tools/Test-ReadyToRun.ps1`, which fails the build if the app's own assemblies are not ReadyToRun.

The package is ready to run, so deploy it without a remote build. Zip the contents of the publish folder, including the hidden `.azurefunctions` folder, which the isolated worker needs. (PowerShell's `Compress-Archive` leaves out hidden files, so do not use it.)

```console
# Linux or macOS
cd _packages/Endjin.Ip.Maturity.Model.Host/linux-x64 && zip -r ../app.zip . && cd -

# PowerShell
[IO.Compression.ZipFile]::CreateFromDirectory("$PWD/_packages/Endjin.Ip.Maturity.Model.Host/linux-x64", "$PWD/_packages/app.zip")

az functionapp deployment source config-zip --resource-group <rg> --name <app> --src _packages/app.zip
```

From GitHub Actions, you can instead deploy the `function-app` artifact with [`Azure/functions-action`](https://github.com/Azure/functions-action), with `remote-build: false` and OpenID Connect sign-in to Azure.

#### Startup warm-up

When an instance starts, the Host's `StartupWarmup` service does, in the background, the one-off work that would otherwise slow down the first request: it renders badges, loads and evaluates a small IMM (which compiles that code and builds the YAML deserializer), and fetches the canonical rule set from GitHub, which the Host then caches. It does not delay startup, which matters because Flex Consumption allows an app only 30 seconds to initialize when an instance is assigned to it. Requests that arrive before it finishes are served as usual.

When it finishes, it writes one log line that contains `Warm-up:`. When the Host exports telemetry (see [Tracing and logging with OpenTelemetry](#tracing-and-logging-with-opentelemetry)), the line goes to the exporter with the rest of the Host's logs. When it does not, the line would never be seen, because the worker relays logs to the Functions host only when they are written during a function invocation, so the Host writes this line (and only this line) to the console, which the Functions host captures. Look for it in the Application Insights `traces` table, for example with `traces | where message contains "Warm-up:"`, or in the console output of `func start`. The line gives the time taken by each step and whether the step succeeded:

```text
info: Endjin.Ip.Maturity.Model.Host.StartupWarmup[1] Warm-up: badges 104 ms (ok), yaml/evaluate 35 ms (ok), rule set 80 ms (ok)
```

A failed step shows `failed:` and the exception instead of `ok`, and the line is logged as a warning. A failure does not stop the Host: the step's work is done when the first request needs it, as it would be without the warm-up. If the rule set step fails, check that the app can reach `raw.githubusercontent.com`.

To measure the effect of the warm-up, set the app setting `IMM_DISABLE_WARMUP` to `true`, which turns it off. `tools/Measure-ColdStart.ps1` measures cold starts locally under the Core Tools, with and without ReadyToRun and the warm-up. Its results show the relative effect of each, but they are not the times you will see on Flex Consumption.

The warm-up is also traced, as a trace of its own: an `imm.warmup` span with a child span for each step, `imm.warmup.badges`, `imm.warmup.yaml` and `imm.warmup.ruleset`. The last one contains the spans of the rule set fetch. A step that fails, and the `imm.warmup` span, have the status Error, and `imm.warmup` has the tag `imm.warmup.failed`.

#### Tracing and logging with OpenTelemetry

The Host can export traces and logs with [OpenTelemetry](https://opentelemetry.io/), so that you can see where the time of any badge request went: waiting for the Functions host, fetching the rule set and the IMM from GitHub (or getting them from the cache), parsing the YAML, evaluating the IMM, and rendering the badge or page. Each log line carries the trace id and span id of the request that wrote it.

The Host exports only when one of these settings is set. Without either, nothing listens to the spans, so they are never created and cost nothing.

- `APPLICATIONINSIGHTS_CONNECTION_STRING` exports traces, logs and metrics to Application Insights.
- `OTEL_EXPORTER_OTLP_ENDPOINT` exports traces and logs over OTLP, for example to the Aspire dashboard. The other standard `OTEL_EXPORTER_OTLP_*` settings also apply.

You can set both. These are the spans of one badge request:

```text
<Functions host request span>                      (host process)
└─ GitHubImmTotalScore                             Microsoft.Azure.Functions.Worker (the invocation)
   └─ imm.badge.total                              Endjin.Ip.Maturity.Model.Host
      ├─ imm.content.get  [rule set URL]           Endjin.Imm (cache lookup)
      │  └─ imm.content.fetch                      Endjin.Imm (only on a miss or revalidation)
      │     ├─ GET                                 System.Net.Http (raw.githubusercontent.com)
      │     └─ imm.content.deserialize             Endjin.Imm (only when a body was received)
      ├─ imm.content.get  [IMM URL]                Endjin.Imm (in parallel with the one above)
      │  └─ ...
      ├─ imm.evaluate                              Endjin.Imm
      └─ imm.render                                Endjin.Ip.Maturity.Model.Host
```

The rule endpoint's span is `imm.badge.rule`, and the `showbadges` endpoint's span is `imm.badge.showbadges`. The spans have these tags:

- `imm.badge.*`: `imm.github.owner`, `imm.github.repo`, `imm.github.ref` (the `projectBranch`) and `imm.ruleset.ref` (the `definitionsBranch`); `imm.rule.id` on the rule endpoint; and `imm.error`, the `X-Imm-Error` code, when an error badge or page is returned. The status is Error only when GitHub is unavailable, or when an exception propagates (then `error.type` and an `exception` event are also recorded). A request for an IMM that does not exist is answered correctly, so its span has an `imm.error` tag but is not an error.
- `imm.render`: `imm.render.kind`, which is `total`, `rule`, `showbadges`, `error-badge` or `error-page`.
- `imm.content.get`: `url.full`, and `imm.cache.result`, which is `hit` (the cached content was fresh), `miss` (nothing was cached, so it was fetched), `revalidate` (the cached content was stale, so it was fetched again with its ETag) or `coalesced` (the request joined a fetch that another request had started; the span links to that fetch).
- `imm.content.fetch`: `url.full`, `http.response.status_code`, `imm.cache.conditional` (an ETag was sent), `imm.cache.not_modified` (GitHub returned 304) and `imm.cache.stale_if_error` (GitHub was unavailable, so the content cached before was used).
- `imm.content.deserialize`: `imm.content.length`, the length of the YAML.
- `imm.evaluate`: `imm.rule_count`, `imm.score` and `imm.max_score`.

`host.json` sets `telemetryMode` to `OpenTelemetry`, so that the Functions host's request span and the worker's spans are in one trace.

##### In Azure

Set the app setting `APPLICATIONINSIGHTS_CONNECTION_STRING` to the connection string of an Application Insights resource. Nothing else is needed. To see the spans of one request as a tree, open a request in Application Insights and choose **View all** under the end-to-end transaction details. Span tags appear in `customDimensions`.

In this mode, log streaming in the Azure portal is not supported. Use Application Insights (Live Metrics, or the `traces` table) instead.

These queries are a place to start. The full tree for one request, given its operation id:

```kusto
union requests, dependencies
| where operation_Id == "<id>"
| project timestamp, itemType, name, duration, customDimensions
| order by timestamp asc
```

Where the time goes, on average and at the 95th percentile, for each span:

```kusto
dependencies
| where name startswith "imm."
| summarize avg(duration), percentile(duration, 95), count() by name
```

How well the cache works:

```kusto
dependencies
| where name == "imm.content.get"
| summarize count() by tostring(customDimensions["imm.cache.result"])
```

##### Locally, with the Aspire dashboard

The [Aspire dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone) shows traces and logs from any OTLP exporter. Start it with Docker:

```console
docker run --rm -it -p 18888:18888 -p 4317:18889 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then add the endpoint to the `Values` in `local.settings.json`, start the Host with `func start`, request a badge, and open `http://localhost:18888`:

```json
"OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
```

##### Tracing connections

Set `IMM_TRACE_CONNECTIONS` to `1` or `true` to also trace DNS lookups, socket connections, TLS handshakes and waits for a connection from the pool. These spans are noisy, so leave the setting off unless you need it. They are useful for diagnosing cold starts, where the first fetch from GitHub pays for setting up a connection, and the Host's 5-minute connection recycling, which makes a fetch set up a new one.

#### Troubleshooting

If the worker process fails at startup with exit code 139 (a segmentation fault), you might have hit [azure-functions-dotnet-worker#3335](https://github.com/Azure/azure-functions-dotnet-worker/issues/3335), which affects .NET 10 isolated apps on Flex Consumption. The workarounds are to set the app setting `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED` to `0` (cold starts are then slower), or to use 4096 MB instances.

### Benchmarks

`Solutions/Endjin.Imm.Benchmarks` is a [BenchmarkDotNet](https://benchmarkdotnet.org/) project that measures execution time and memory allocations for the work the Host does on each request:

- loading rule sets and IMMs from YAML, including the cost of building the YAML deserializer
- evaluating an IMM, and each rule calculator on its own
- rendering SVG badges, and the text measurement and HTML decoding steps inside it
- the complete pipeline for the total badge, rule badge and `showbadges` endpoints, with and without a cache hit, using the Host's own code (the benchmarks reference the Host project, and the `HostEndpoint_*` benchmarks call its function methods)
- a hit in the HTTP deserializing cache

The benchmarks make no network calls. They use a pinned snapshot of the canonical rule set (see `Solutions/Endjin.Imm.Benchmarks/Data/README.md`) and an in-process stub in place of GitHub.

Always run the benchmarks in Release. A Debug build fails validation.

```console
# List the benchmarks
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --list flat

# Run all the benchmarks with the default job (took 43 minutes for the first baseline)
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*"

# Smoke test: run each benchmark once, to make sure that it executes
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*" --job dry

# Quick, less accurate results
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*" --job short

# Run some categories only (Yaml, Evaluate, Badge, Pipeline, Cache)
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --anyCategories Yaml Evaluate

# Run one class, or one method
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*BadgePainterBenchmarks*"
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*.Evaluate"

# On a hybrid CPU (performance and efficiency cores), keep the benchmark processes on the performance cores.
# The mask is specific to the machine: 4095 (0xFFF) selects logical processors 0-11, the performance cores of an i7-13800H.
dotnet run -c Release --project Solutions/Endjin.Imm.Benchmarks -- --filter "*" --affinity 4095
```

The results go to `Solutions/Endjin.Imm.Benchmarks/BenchmarkDotNet.Artifacts/results` (GitHub Markdown, compressed JSON and CSV). This folder is in `.gitignore`. To keep a set of results for comparison, copy them into `Solutions/Endjin.Imm.Benchmarks/Baselines/`, one folder for each baseline, with a README that records the commit, command and environment. That folder is also in `.gitignore`: results depend on the machine they were measured on, so baselines are kept locally and not in source control.

`dotnet test --solution` does not run the benchmarks.

## Licenses

[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/LICENSE)

The IP Maturity Model is available under the Apache 2.0 open source license.

For any licensing questions, please email [&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;](&#109;&#97;&#105;&#108;&#116;&#111;&#58;&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;)

## Project Sponsor

This project is sponsored by [endjin](https://endjin.com), a UK based Technology Consultancy which specializes in Data & Analytics, AI & Cloud Native App Dev.

> We help small teams achieve big things.

We produce three free weekly newsletters: 

 - [Azure Weekly](https://azureweekly.info) for all things about the Microsoft Azure Platform
 - [Power BI Weekly](https://powerbiweekly.info) for all things Power BI
 - [Fabric Weekly](https://fabricweekly.info) for all things Microsoft Fabric

Keep up with everything that's going on at endjin via our [blog](https://endjin.com/blog), follow us on [Twitter](https://twitter.com/endjin), [YouTube](https://www.youtube.com/c/endjin) or [LinkedIn](https://www.linkedin.com/company/endjin).

We have become the maintainers of a number of popular .NET Open Source Projects:

- [Reactive Extensions for .NET](https://github.com/dotnet/reactive)
- [Reaqtor](https://github.com/reaqtive)
- [Argotic Syndication Framework](https://github.com/argotic-syndication-framework/)

And we have over 50 Open Source projects of our own, spread across the following GitHub Orgs:

- [endjin](https://github.com/endjin/)
- [Corvus](https://github.com/corvus-dotnet)
- [Menes](https://github.com/menes-dotnet)
- [Marain](https://github.com/marain-dotnet)
- [AIS.NET](https://github.com/ais-dotnet)

And the DevOps tooling we have created for managing all these projects is available on the [PowerShell Gallery](https://www.powershellgallery.com/profiles/endjin).

For more information about our products and services, or for commercial support of this project, please [contact us](https://endjin.com/contact-us). 

## Code of conduct

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;](&#109;&#097;&#105;&#108;&#116;&#111;:&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;) with any additional questions or comments.
