# Workflow and Tests

## Before Changing Code

- Read [AGENTS.md](../AGENTS.md). Answer questions by explaining the problem/solutions first; implement explicit requests. Resolve material uncertainty through code/fixtures.
- Prototype: no backward-compatibility layers. Remove unused internal helpers in the area being changed.
- Functional + imperative; separate data and operations. Prefer extension methods over ordinary instance operations/OOP hierarchies.
- Small local scopes, local values over fields/globals, minimal redundant state. Inline single-use internal helpers where useful; retain local functions for recursion/substantial steps.
- C# local functions should be `static`, with explicit context; use actual callback closures deliberately, with small captures.
- Check working directory/Git status; preserve existing changes. Test runs may write fixtures.

## C# Conventions

Details: [CodeStyle.txt](../SPO_CS/CodeStyle.txt), actual [.editorconfig](../.editorconfig), [_GlobalUsings.cs](../SPO_CS/src/_GlobalUsings.cs).

| Area | Convention |
| --- | --- |
| Modules/types/arguments | `mModule`, `tType`, `aArgument`; PascalCase locals, constants `c`, globals `g` |
| Structure | Static modules, related nested data types; global namespace |
| Primitives/delegates | Aliases `tText`, `tInt32`, `tNat32`, `tBool`; `mStd.tFunc`/`tAction` |
| Collections | Project `mStream`, `mArrayList`, `mMap`, `mTreeMap`; BCL where interop requires it; no additional NuGet dependencies in the language core |
| Errors | `tMaybe` for absence; `tResult` for expected failures; `Then` maps, `ThenTry` chains results |
| Invariants | `mAssert`, `mError`; `ElseThrow`/assertions at deliberate boundaries only |
| Files/format | File-based C# with `#:include`/`#:ref`; UTF-8, LF, tabs (4), no final newline; opening `{` on the introducing line |

- Persistent maps return new roots; `readonly`/`record` does not imply deep immutability.
- Mutable VM stacks, builders, caches, and arenas are intentionally imperative; do not mechanically convert them to functional code.

## SPO Style

- [API-Design.md](../SPO/API-Design.md): design the call first; readable infix names, visible receiver, explicit roles/units.
- Arguments `a`, types `t`, constant data `c`; argument-dependent locals have no `c`. Account for indirect dependencies/captures.
- Public contracts in SIG; annotate only necessary type boundaries, infer local types.
- Structural records instead of getters/wrappers; generics and abstract types need a concrete purpose.
- Mark imported vector operators: `..._+_...`, `..._==_...`; scalar operators stay unchanged.
- Handwritten apps/libs/runtime: 120 columns at tab width 4; `SPO/Tests/Readability.ps1` checks mechanical rules.

## Test Commands

SDK: file-based .NET 11; respect `global.json` and `#:property`. The language core needs no `.sln` build.

**From `SPO_CS/`:**

```powershell
dotnet src/mRunTests.cs -- --help
dotnet src/mRunTests.cs -- --plainText --outputLevel 2
dotnet src/mRunTests.cs -- --plainText -t -g -m Mutable Interface
dotnet src/mRunTests.cs -- --plainText -t -g -M mSPO_Parser LOAD
dotnet src/mRunTests.cs -- -l -m SIG Sig
```

- `-m` = match any filter, `-M` = match all; **case-sensitive substrings, filters last**. `-l` lists without execution.
- `-t -g` gives immediate detail output for long/hanging cases; exit code = failure count.
- For repeated runs, build once: `dotnet build src/mRunTests.cs --output ../.tmp/docai-build`; then `dotnet ../.tmp/docai-build/mRunTests.dll --plainText --outputLevel 2` plus filters.

**From the repository root, as relevant to the change:**

```powershell
dotnet VSCode_Extension/server-src/mSPO_Navigation.Tests.cs
./SPO/Tests/Readability.ps1
dotnet SPO/src/Program.cs -- SPO/Apps/Snake --check
dotnet SPO/src/Program.cs -- SPO/Apps/GUI_Demo --check
dotnet SPO/Tests/Launcher.Tests.cs
dotnet SPO/Tests/Runtime.Tests.cs
dotnet SPO/Tests/GUI.Tests.cs
dotnet SPO/Tests/SDL3.Tests.cs
dotnet SPO/Tests/WindowsTerminal.Tests.cs
```

- GUI/SDL: account for native dependencies and long interpreter runtimes; WindowsTerminal needs a Windows console.
- Match validation to the change; inspect generated `.ILT` and diagnostic artifacts in the diff afterward.

## E2E File Conventions

Authority: [mE2E.Tests.cs](../SPO_CS/src/mE2E.Tests.cs), rather than historical directory names in older descriptions.

| Path / file | Role |
| --- | --- |
| `TestFiles/SPO_Functions`, `ILT_Functions`, `SPO_Modules`, `ILT_Modules` | Positive programs |
| `SPO_Function_Errors`, `ILT_Function_Errors`, `SPO_Module_Errors`, `ILT_Module_Errors` | Negative programs |
| `<Group>/GG_TT_Name.SPO` + `.result.SPO` | SPO entry point and complete SPO module exporting the expected result |
| Same-stem `.ILT` beside `.SPO` | Generated output, not another independent entry point |
| Standalone `.ILT` + `.result.SPO` | Handwritten ILT case |
| Same-stem `.error.txt` | Expected complete normalized compiler diagnostic; program entry point is not executed |
| `.error.new.txt` | Actual mismatch/exception; do not automatically accept it or count it as a current failure |
| `<Group>/_Modules/` | Providers for this group; `.SIG` is a contract, not an executable module |

- Group names are arbitrary. Source extension and companion files determine behavior, not “Functions”/“Errors” in the name.
- Positive SPO cases check **three paths**: SPO result, SPO→ILT generation, ILT execution against the same expected result.
- Changed `.ILT` is updated directly, including SPO dependencies; changed IL text alone does not fail the test.
- Result comparison checks value representation and type compatibility, not exact equality of all inferred types.
- Negative cases: unexpected compilation success remains a failure; exceptions are bugs, not valid diagnostic baselines. Review positions/diagnostics before changing expectations.
- Provider name = full filename stem. `GG_TT_` restricts a provider to its matching case; groups have separate namespaces.
- Imports from the SPO import pattern or ILT entry contract determine dependencies. Direct standard operations may come from the group's `Std.ILT`.
- Load modules on demand; normalized absolute paths share setups. Report missing providers/cycles; helper directories and disabled source extensions are not tests.

## Where to Add a Test

- Observable language rule → positive/negative E2E fixture; consider direct ILT input too.
- Internal type identity, substitution, AST shape/positions, or VM API contract → adjacent `mModule.Tests.cs`; aggregate in `mSPO.Tests.cs`/Common.
- Capability identity/lifetime, app contract, native integration → `SPO/Tests/`.
- Editor navigation/references/scopes → `VSCode_Extension/server-src/mSPO_Navigation.Tests.cs`.
- Bug: keep a small reproducible counterexample and an allowed control case; avoid masking the target diagnostic with an earlier unrelated error.