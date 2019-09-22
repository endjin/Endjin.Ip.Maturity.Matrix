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