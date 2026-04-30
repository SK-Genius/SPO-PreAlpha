[![codecov](https://codecov.io/github/SK-Genius/SPO-PreAlpha/graph/badge.svg?token=4YUKMtzu8y)](https://codecov.io/github/SK-Genius/SPO-PreAlpha)
[![CodeFactor](https://www.codefactor.io/repository/github/sk-genius/spo-prealpha/badge)](https://www.codefactor.io/repository/github/sk-genius/spo-prealpha)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=SK-Genius_SPO-PreAlpha&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SK-Genius_SPO-PreAlpha)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/SK-Genius/SPO-PreAlpha)

# SPO (PreAlpha)

SPO is a pre-alpha programming language project focused on security, safety, robustness, and performance.
This repository currently contains the C# implementation of the language, a sample host, a VS Code extension, and a local documentation viewer.

## Why SPO?

Modern software runs critical infrastructure, yet many languages still make it easy to ship null bugs, unchecked bounds, and hidden side effects.
SPO aims to push those failure modes out of the default path by making dataflow and effects explicit.

Current design goals:

- Memory safety and type safety by construction.
- Avoid common runtime crashes such as null dereferences and out-of-range access.
- Keep mutable state explicit and capability-based instead of allowing hidden global effects.
- Make static dataflow analysis straightforward.
- Allow functional, imperative, and parallel styles to coexist.

Current safety direction:

- Explicit `tMaybe...` and `tResult...` types instead of implicit nulls or exceptions.
- Refinement-style typing to move validity checks to the caller when possible.
- Capability-oriented effects so code must be given access to files, networks, or other resources.

## Repository Layout

| Path | Purpose |
| --- | --- |
| `SPO_CS/` | main implementation: tokenizer, parser, AST, typing, lowering, VM, diagnostics, navigation, modules, and tests |
| `SPO/` | small sample host that loads and runs `src/_.spo` through the interpreter |
| `VSCode_Extension/` | VS Code client, language server source, packaged server payload, and local deploy helper |
| `doc/` | contributor documentation in the repository's custom `.wiki` format |
| `index.html` and `js/` | local viewer for `.wiki`, `.spo`, `.ilt`, diagrams, images, and related assets |

If you want the file-level map first, open [doc/Repository_Map.wiki](doc/Repository_Map.wiki).
If you want the architecture-level explanation, open [doc/Repository_Overview.wiki](doc/Repository_Overview.wiki).

## Contributor Quick Start

Prerequisites:

- .NET 10 SDK pinned in [`global.json`](global.json)
- Node only if you need to package or reinstall the VS Code extension
- Windows if you need the packaged extension server path exactly as checked in today

Start with the compiler-side runner, not the sample host:

```text
cd SPO_CS
dotnet run -- --list
dotnet run -- --matchAny mTokenizer
dotnet run -- --matchAny 07_12_GenericTypes
```

That is the most reliable first feedback loop in the current checkout.
For command details and path-sensitive caveats, read [doc/Build_and_Test.wiki](doc/Build_and_Test.wiki).

## Documentation Map

The detailed contributor docs live under [`doc/`](doc/) and are easiest to browse locally through the bundled viewer:

```text
index.html?file=doc/index.wiki
```

Recommended entry points:

- [doc/index.wiki](doc/index.wiki): master index by goal
- [doc/Getting_Started.wiki](doc/Getting_Started.wiki): fastest contributor onboarding path
- [doc/Build_and_Test.wiki](doc/Build_and_Test.wiki): reliable commands and current caveats
- [doc/Compilation_Pipeline.wiki](doc/Compilation_Pipeline.wiki): SPO source to IL to VM
- [doc/VM_and_Modules.wiki](doc/VM_and_Modules.wiki): runtime data model and module bootstrap
- [doc/Tooling.wiki](doc/Tooling.wiki): local viewer and VS Code extension
- [doc/Current_Status.wiki](doc/Current_Status.wiki): dated command results for this checkout

## Current Verified Status (2026-05-01)

These command results were rechecked in this worktree on 2026-05-01:

- `cd SPO_CS` then `dotnet run -- --list` succeeds and lists the custom test tree.
- `cd SPO_CS` then `dotnet run -- --matchAny mModule_Tests --plainText` currently fails in `Maybe`.
- `cd SPO` then plain `dotnet run` is not the safest smoke test in this checkout; if you need the host path, use `dotnet run /p:EnableSourceControlManagerQueries=false`, which currently still fails with a parser error at `SPO/src/_.spo:13`.
- `cd VSCode_Extension` then `dotnet run .\Deploy-LocalExtension.cs -- --help` succeeds.

Treat `SPO_CS/` as the implementation center and the best default validation path.
Use the sample host only when you are specifically debugging host embedding behavior.

## Planned Killer App

A plugin-driven, secure, privacy-first social media infrastructure without mandatory central servers.
Untrusted extensions would run under explicit capabilities, and user data would stay user-hosted unless deliberately delegated elsewhere.

## Roadmap

- Pre-Alpha to Alpha: make the compiler self-hosting by rewriting it in SPO.
- Alpha to Beta: complete memory management and missing core features such as refinement types, parallel execution, and debugger support.
- Beta to Release: add real CPU backends such as LLVM and expand tooling plus standard libraries.
