# IP Maturity Matrix

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

Each of these has different nuances when it comes to the categories in the IP Maturity Matrix outlined below. 

## Categories

We have, collectively, defined the following categories as quality measures for our IP. In many ways these measures can be thought of as a more traditional "definition of done". These quality measures cover different aspects of the lifecycle of IP, from maintenance to adoption, to operational support. It also includes commercial and legal concerns.

- [IP Maturity Matrix](#ip-maturity-matrix)
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
    - [Platform \& Runtime Version](#platform--runtime-version)
    - [Associated Work Items](#associated-work-items)
    - [Source Code Availability](#source-code-availability)
    - [License](#license)
    - [Production Use](#production-use)
    - [Insights](#insights)
    - [Packaging](#packaging)
    - [Deployment](#deployment)
    - [Ops](#ops)
  - [The IMM Schema](#the-imm-schema)
  - [How to use the IP Maturity Matrix](#how-to-use-the-ip-maturity-matrix)
    - [Total Score](#total-score)
    - [IMM Measures](#imm-measures)
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

Code should be self-documenting, but long term support of maintenance of code requires context and narrative as well as purpose.

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

Understanding how to get started with our IP, or how different elements of IP can be used together is fundamental for increased productivity.

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

### Platform & Runtime Version

All software suffers from bit-rot. The platforms & runtime we depend on are no different. How up to date are we with the currently supported versions?

| Score | Measure                            |
|-------|------------------------------------|
| 0     | Using an unsupported version       |
| +1    | Using a LTS version                |
| +1    | Using the most current LTS version |

### Associated Work Items

The number of associated work items is another code smell; it can signal either chaos or order. We need to distinguish between these two states.

| Score | Measure                 |
|-------|-------------------------|
| 0     | None                    |
| 1     | Bugs & Features         |
| 2     | Curated Bugs & Features |
| 3     | Active Roadmap          |

### Source Code Availability

One of the most overlooked aspects of our use of IP is supporting our contractual clauses, allowing customers to access the source code for the binaries we use. Historically, we have been approached 5 years after the actual engagement as part of acquisition / due diligence processes.

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

When things go wrong, we need the infrastructure in place to help us quickly resolve the situation.

| Score | Measure                                                           |
|-------|-------------------------------------------------------------------|
| 0     | None                                                              |
| +1    | Telemetry, Diagnostics & Debugging                                |
| +1    | Performance Characteristics                                       |
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

We need to consider other personas, whose function is to support the IP we create. How do we make their experience "delightful"?

| Score | Measure |
|-------|---------|
| 0     | TBD     |

## The IMM Schema

The IMM schema is designed to be very flexible as you may decide that you want to add or alter categories. The [current RuleSet](https://github.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/blob/main/RuleSet.yaml) is defined in the [Endjin.Ip.Maturity.Matrix.RuleDefinitions](https://github.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions) repo.

Each rule needs a unique, guid based Id, a name, and a data type which can either be `Discrete` (meaning it can only be scored by a single item listed in the schema), or `Continuous` (meaning that the score can be cumulative). These two data types are used to select and run the correct rules engine to calculate the values to be used to render the badges.

## How to use the IP Maturity Matrix

Each project that adopts the IMM simply adds a `imm.yaml` file into the root of the repo. `Endjin.Ip.Maturity.Matrix.Host` is an Azure Function, which when given the path to the GitHub repo, will render the IMM Measure as a badge, which can be displayed in the repo and provide an "at a glance" view of the quality of the project. 

The Azure Function exposes a HTTP Trigger, which has two actions.

### Total Score

Call the the Function `api/imm/github/<Org_Name>/<Repo>/total?cache=false`

And it will render a badge - the example below is for the [AIS.NET Project](https://github.com/ais-dotnet/Ais.Net) (you may need to refresh the page to wake the Function up!)

[![IMM](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/total?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/total?cache=false)

### IMM Measures

To display a badge for each measure you need to call `api/imm/github/<Org_Name>/<Repo>/rule/<RuleId>?nocache=true`

Here are examples for the [AIS.NET Project](https://github.com/ais-dotnet/Ais.Net);

[![Shared Engineering Standards](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?nocache=true)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?cache=false)

[![Coding Standards](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)

[![Executable Specifications](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)

[![Code Coverage](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)

[![Benchmarks](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)

[![Reference Documentation](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)

[![Design & Implementation Documentation](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)

[![How-to Documentation](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)

[![Date of Last IP Review](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)

[![Runtime Version](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)

[![Associated Work Items](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)

[![Source Code Availability](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)

[![License](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)

[![Production Use](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)

[![Insights](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)

[![Packaging](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)

[![Deployment](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)

## Licenses

[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix/master/LICENSE)

The IP Maturity Matrix is available under the Apache 2.0 open source license.

For any licensing questions, please email [&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;](&#109;&#97;&#105;&#108;&#116;&#111;&#58;&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;)

## Project Sponsor

This project is sponsored by [endjin](https://endjin.com), a UK based Technology Consultancy which specializes in Data, AI, DevOps & Cloud, and is a [.NET Foundation Corporate Sponsor](https://dotnetfoundation.org/membership/corporate-sponsorship).

> We help small teams achieve big things.

We produce two free weekly newsletters: 

 - [Azure Weekly](https://azureweekly.info) for all things about the Microsoft Azure Platform
 - [Power BI Weekly](https://powerbiweekly.info) for all things Power BI, Microsoft Fabric, and Azure Synapse Analytics

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

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behaviour in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;](&#109;&#097;&#105;&#108;&#116;&#111;:&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;) with any additional questions or comments.