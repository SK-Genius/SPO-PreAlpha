# SPO, Modules, and Runtime

## Syntax for Reading Code

| Form | Meaning |
| --- | --- |
| `§DEF Name = Value`, `€ Type` | Binding and type annotation |
| `§VAR Cell := Value .`, `§TO_VAL Cell`, `Cell := Value .` | Create, read, write a mutable cell; access checks apply |
| `(§DEF a € T) => Expression` | Function; `{ ... §RETURN Value }` is a block expression |
| `(§VAR aSelf € T) : (...) => Expression` | Method; a block body without `=>` is also supported |
| `Value .Operation Argument`, `Object : Operation Argument .` | Function and method calls; bind method results with `=> Pattern` |
| `...Name...` | Argument slots are part of the name; infix/multiword calls |
| `§DEF t <=> (...) => ...` | Generic implementation; functions and methods supported |
| `§RECURSIVE { ... }` | Recursive functions/methods; mutual references allowed |
| `{Field: Value}`, `[< Field: Type >]` | Structural record value and open record contract |
| `(a; b)`, `(a, b, c)` | Pair and tuple; tuples are pair chains built from `EMPTY` |
| `#Tag Value`, `[T \| U]` | Prefix value and union type |
| `§IF Value MATCH { Pattern: Result ... }` | Pattern selection with statically checked coverage |
| `§>`, `§<` | Pipes; insert only the piped value, no implicit `()` |

- **Distinguish `F` from `F...`:** `.F` versus `.F ()`; likewise for methods, `#X` versus `#X ()`, and their patterns/types.
- `()` is the empty value, `[]` its type. Empty parentheses may span lines.
- Block results require `§RETURN`; no implicit last-expression result. `§RETURN Value IF Condition` returns early.
- Function/method type: `[Arg => Res]` / `[Obj : Arg => Res]`; IL writes the latter as `[Obj : [Arg => Res]]`.
- SPO `§RECURSIVE`/`§GENERIC`/interface types become IL `§REC`/`§ALL`/`§ANY`; distinguish binder-form IL `§ANY` from the top type `ANY`.
- Full syntax: [SPO cheat sheet](../doc/CheatSheet_SPO.wiki), [IL cheat sheet](../doc/CheatSheet_IL.wiki); executable examples in `SPO_CS/TestFiles/SPO_Functions/`.

## Modules and SIG

- SPO module: optional `§IMPORT Pattern`, commands, `§EXPORT Expression`. Imports are pattern matches, not just file lists.
- The loader supplies the import value; filesystem resolution is not a general SPO operation.
- **Module values are transitively immutable.** Create mutable state per call through factories or supply capabilities later.
- A `.SIG` file contains exactly **one SPO type expression**, without `§IMPORT`/`§DEF`/`§EXPORT`; it need not itself be a SIG contract.
- `[§LOAD "File.SIG"]`: bracketed type syntax; paths are relative to the containing file, nested loads allowed. Missing/invalid files, extra type expressions, and cycles produce diagnostics.
- Loading happens during SPO parsing; ILT contains resolved types and does not support `§LOAD`.
- Compiler file access uses `mTokenizer.tFileContext` (`ReadText`, `LoadPath`); restricting it is the host's responsibility, outside runtime capability checking.

| SIG concept | Meaning |
| --- | --- |
| Contract | `[§SIG_WITH t € Kind IN BodyType]`; binds a representation type/constructor |
| Package | `§SIG Contract WITH Head IN Body`; retains contract, actual head, and body |
| Construction | Head kind must match; body is a subtype of the body contract after substituting the head |
| Open with `WITH §DEF t` | New local abstract witness |
| Another import with `WITH t` | Requires exactly the same actual head, not just subtyping |
| Return dependent values | Keep the witness in scope or repackage as SIG |

- SIG transports an abstract type and its API. An interface binds a concrete receiver with limited method authority. Keep these roles distinct.
- IL: `SigHeadAs` retains the witness already chosen by the SPO checker; `TryHasHeadType` checks actual head equality. `Alias` creates no new witness.
- Examples: [Span.SIG](../SPO_CS/TestFiles/SPO_Modules/_Modules/Span.SIG), [Span.SPO](../SPO_CS/TestFiles/SPO_Modules/_Modules/Span.SPO), [shared head across two imports](../SPO_CS/TestFiles/SPO_Modules/00_09_SigImportSameHead.SPO).
- Details: [SIG.md](../SPO_CS/SIG.md); verify older path/phase descriptions against current code.

## Module Checking and Execution

Source: [mModule.cs](../SPO_CS/src/mModule.cs).

1. `Check`: check dependencies; SPO → checked IL, or ILT → parser + independent checker. Save/update SPO output as `.ILT`.
2. `Link`: compare the graph's import/export types **before** initializing modules.
3. `Init`: initialize dependencies, assemble an immutable import record, generate opcodes, execute the entry point; cache result/error.

- IL module: `§TYPES`, then definitions; the last definition is the entry point. A definition's contract first maps `ENV` to the actual callable contract.
- VM register roles: `ENV` environment, `OBJ` receiver, `ARG` ordinary argument, `RES` result.
- `TryRun` checks IL; `RunValidModule` assumes that check and validates import types/mutability. `GenerateOpcodes` does not perform independent validation itself.
- VM runtime checks protect actual closure environments and module imports/exports, among other invariants; they do not replace static access checking.

## Apps and Native Capabilities

| Component | Responsibility |
| --- | --- |
| [Program.cs](../SPO/src/Program.cs) | Launcher; starts the configured runtime `.cs`/`.dll`/`.exe` in a separate process, forwards its exit code |
| `startup.jni` | `runtime`, `entry`, `modules`; each module has `path` and optional named `imports` |
| [mStartup.cs](../SPO/src/mStartup.cs) | Configured graph, shared dependencies, missing names/cycles; `.*` prefers `.spo` over `.ILT` |
| `Runtime/Terminal/`, `Runtime/GUI/` | Native resources, input, rendering, event loop; shared VM helpers in `mRuntime.cs` |
| `Libs/App.SIG`, `Libs/GUI/App.SIG` | App exports SIG with state type, `Initial`, `Frame...`; terminal adds `TickMilliseconds` |
| `Frame...` | Receiver = terminal/surface interface; argument = `(State, Event)`; result = `(NextState, ContinueRunning)` |

- Configuration paths are relative to `startup.jni`; SIG paths to their containing source file.
- Supply native capabilities at the frame call, not through module imports or closure captures.
- The host creates an interface with a private handle and expires it when `WithCapability` ends; see [typechecker/capabilities](02_Typechecker_Capabilities.md) for access rules.
- `--check` checks the module graph and app contract without windows/native resources; it does not test gameplay/rendering.
- Snake separates `Input`, `Logic`, `Render`, and shared `State`; logic needs no terminal. GUI widgets/layout/interaction live in SPO, SDL access in the host.
- The GUI host sends an additional `Redraw` after an input frame. Compiler hosts use a 32 MiB thread stack for recursive compiler paths.
- Demos are interpreted; GUI is particularly slow. Large interaction tests may take minutes.
- Details: [SPO/README.md](../SPO/README.md), [runtime contract](../SPO/Runtime/README.md), [Snake](../SPO/Apps/Snake/README.md).