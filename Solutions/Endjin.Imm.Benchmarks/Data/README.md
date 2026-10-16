# Benchmark data

## RuleSet.yaml

`RuleSet.yaml` is a byte-for-byte snapshot of the canonical IMM rule set. The benchmarks embed it
(logical name `RuleSet.yaml`) so that they make no network calls and give reproducible results.

| | |
|---|---|
| Source repository | [endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions](https://github.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions) |
| Source URL | <https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/f4d05d030f4579581730c5374e1b3bd8b5f8dad8/RuleSet.yaml> |
| Commit SHA | `f4d05d030f4579581730c5374e1b3bd8b5f8dad8` (head of `main`, committed 2023-02-22) |
| Snapshot taken | 2026-10-15 |
| SHA-256 | `1a86f444b5966ffd3ad1490ad8048071d4f77b1f70d369f86f0d41dbda97a744` |

The snapshot contains 18 rules: 9 Continuous, 7 Discrete, 1 Age (`da4ed776-0365-4d8a-a297-c4e91a14d646`,
Date of Last IP Review) and 1 Framework (`6c0402b3-f0e3-4bd7-83fe-04bb6dca7924`, Framework Version).

The repository's default branch is `main`, which is the branch the Host requests by default.

## imm-default.yaml

`imm-default.yaml` is not copied here. The project file links it from `../../Endjin.Imm.App/imm-default.yaml`
and embeds it with the logical name `imm-default.yaml`.

## Refreshing the snapshot

A refresh changes the benchmark inputs. Do it in a separate commit, and record a new baseline after it.

```powershell
$sha = gh api repos/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/commits/main --jq .sha
Invoke-WebRequest "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/$sha/RuleSet.yaml" `
    -OutFile Solutions/Endjin.Imm.Benchmarks/Data/RuleSet.yaml
(Get-FileHash Solutions/Endjin.Imm.Benchmarks/Data/RuleSet.yaml -Algorithm SHA256).Hash
```

Then update the SHA, dates and hash in this file. If the rule ids in `SampleData.cs` no longer exist
or have a different `DataType`, the benchmark setup throws.
