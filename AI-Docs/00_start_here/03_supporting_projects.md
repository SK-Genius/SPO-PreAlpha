# Supporting Projects

`SPO_CS/` is the architectural center of this repository, but it is not the whole repo. These sibling folders matter when you work on host integration, editor tooling, or test discovery.

## Repo Satellites

| Path | Verified role |
| --- | --- |
| `SPO/` | example host executable that references `SPO_CS` and runs `.spo` programs |
| `LSP/` | VS Code client package intended to start a `server/SPO-LSP-Server.exe` process |
| `spo-grammar-0.1.0/` | TextMate grammar package for the `spo` language id |
| `TestRunner/FakeTestAdapter/` | custom VSTest adapter that exposes `mTest` tests to IDE tooling |
| `doc/` | older free-form notes, not wired into execution |

When you only need parser, lowering, VM, or module behavior, stay in `SPO_CS/`. Move into these folders only for the corresponding integration.

## `SPO/`: Example Host Embedding

`SPO/SPO.csproj` is a small `net10.0` executable that references `../SPO_CS/SPO_CS.csproj`.

`SPO/src/Program.cs` shows the host contract concretely:

- it loads `Module_Std` first
- it reads a source file from `aArgs[0]`, defaulting to `src/_.spo`
- it injects two host-visible imports: `_StdLib` and `_LoadModule...`
- `_LoadModule...` is implemented as an `ExternProc` that reads another `.spo` file relative to the current project file
- after `mSPO_Interpreter.Run(...)`, the returned value is invoked through `mVM.Run(...)` with object `#IO ()`, empty argument, and a mutable result slot
- the process expects the final result to be `§TRUE`

`SPO/src/_.spo` is the clearest example of how that host contract is consumed.
It loads `Stack.spo` and `StackTest.spo` through `LoadModule...`.

## Editor Tooling Split

The repo currently has two separate VS Code-facing folders:

- `spo-grammar-0.1.0/package.json`
  registers language id `spo` for `.spo` and `.ilt`
- `LSP/package.json`
  activates on language `spo`, but currently advertises only `.SPO`

That means file-extension handling is not uniform across the repo:

- core compiler artifacts mostly use uppercase `.SPO` and `.ILT`
- the sample app under `SPO/src/` uses lowercase `.spo`
- the grammar package recognizes lowercase `.spo` and `.ilt`
- the LSP package metadata currently mentions uppercase `.SPO` only

`LSP/src/extension.js` also shows that the LSP bootstrap is unfinished in the current worktree: the `run` / `debug` command block is not assigned to a `serverOptions` object before `LanguageClient` construction.

## `TestRunner/FakeTestAdapter/`

This folder is the bridge from the repo's custom `mTest` tree to the standard Visual Studio / VSTest world.

From the code:

- `FakeTestAdapter.csproj` references `Microsoft.TestPlatform.ObjectModel`
- it also references `../../SPO_CS/.proj/Common/mTest/mTest.csproj`
- `FakeTestDiscoverer` loads assemblies, scans public static fields of type `mTest.tTest`, and flattens nested `mTest` collections into `TestCase` instances
- `FakeTestExecutor` executes the stored test delegate from `Test.LocalExtensionData` and reports the result back to the test platform

If IDE test discovery looks odd, this folder is the first place to inspect.

## Secondary Text Guidance

`SPO_CS/src/Common/llms.txt` exists, but it should be treated as secondary guidance only.

It is already stale relative to the current source tree in multiple places:

- it says "C# 13" while `SPO_CS.csproj` uses `LangVersion = 14.0`
- it says "Tabs for indentation (4 spaces)", which conflicts with the actual project guidance to use tabs

For AI-facing instructions, prefer `SPO_CS/AGENTS.md`, then the code, then `AI-Doc/`. Treat `llms.txt` as a hint list, not as an authority.
