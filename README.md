# IP Maturity Matrix

The aim of this framework is to measure maturity across the disciplines required to build, test, deploy, reuse, support & sell Intellectual Property. This flexible framework is based on a number of conventions to meansure "quality", many of these measures are subjective, rather than qualitative, and need to reviewed by people rather than automated by tools.

Our IP exists at different levels of fidelity:

| Level | Type                          |
|-------|-------------------------------|
| 0     | Script / Template / Code file |
| 1     | Component                     |
| 2     | Tool                          |
| 3     | HTTP APIs                     |
| 4     | Solution                      |
| 5     | Product                       |

Each of these has different nuances when it comes to the categories in the IP Maturity Matrix outlined below. 

## Categories

We have, collectively, defined the following categories as quality measures for our IP. In many ways these measures can be thought of as a more traditional "definition of done". These quality measures cover different aspects of the lifecycle of IP, from maintainance to adoption, to operational support. It also includes commercial and legal concerns.

  - [Shared Engineering Standards](#Shared-Engineering-Standards)
  - [Coding Standards](#Coding-Standards)
  - [Executable Specifications](#Executable-Specifications)
  - [Coverage](#Coverage)
  - [Benchmarks](#Benchmarks)
  - [Reference Documentation](#Reference-Documentation)
  - [Design & Implementation Documentation](#Design--Implementation-Documentation)
  - [How-to Documentation](#How-to-Documentation)
  - [Demos](#Demos)
  - [Date of Last IP Review](#Date-of-Last-IP-Review)
  - [Framework Version](#Framework-Version)
  - [Associated Work Items](#Associated-Work-Items)
  - [Source Code Availability](#Source-Code-Availability)
  - [License](#License)
  - [Production Use](#Production-Use)
  - [Insights](#Insights)
  - [Packaging](#Packaging)
  - [Deployment](#Deployment)
  - [Ops](#Ops)

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

The IMM schema is designed to be very flexible as you may decide that you want to add or alter categories. The [current RuleSet](https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix/master/Solutions/Endjin.Imm.App/RuleSet.yaml) is defined in the `Endjin.Imm.App` project in the solution.

Each rule needs a unique, guid based Id, a name, and a data type which can either be `Discrete` (meaning it can only be scored by a single item listed in the schema), or `Continuous` (meaning that the score can be cumulative). These two data types are used to select and run the correct rules engine to calculate the values to be used to render the badges.

## How to use the IP Maturity Matrix

Each project that adopts the IMM simply adds a `imm.yaml` file into the root of the repo. `Endjin.Ip.Maturity.Matrix.Host` is an Azure Function, which when given the path to the GitHub repo, will render the IMM Measure as a badge, which can be displayed in the repo and provide an "at a glance" view of the quality of the project. 

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

[![Insights](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)

[![Packaging](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)

[![Deployment](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/ais-dotnet/Ais.Net/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)

## Licenses

[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix/master/LICENSE)

The IP Maturity Matrix is available under the Apache 2.0 open source license.

For any licensing questions, please email [&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;](&#109;&#97;&#105;&#108;&#116;&#111;&#58;&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;)

## Project Sponsor

This project is sponsored by [endjin](https://endjin.com), a UK based Microsoft Gold Partner for Cloud Platform, Data Platform, Data Analytics, DevOps, and a Power BI Partner.

For more information about our products and services, or for commercial support of this project, please [contact us](https://endjin.com/contact-us). 

We produce two free weekly newsletters; [Azure Weekly](https://azureweekly.info) for all things about the Microsoft Azure Platform, and [Power BI Weekly](https://powerbiweekly.info).

Keep up with everything that's going on at endjin via our [blog](https://blogs.endjin.com/), follow us on [Twitter](https://twitter.com/endjin), or [LinkedIn](https://www.linkedin.com/company/1671851/).

Our other Open Source projects can be found at https://endjin.com/open-source

## Code of conduct

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;](&#109;&#097;&#105;&#108;&#116;&#111;:&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;) with any additional questions or comments.