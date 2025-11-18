# Project Guidance for AI Assistants

## Project Overview
- **Project Name:** SPO_CS
- **Language:** C# (modern, .NET 10, C# 14 features)
- **Paradigm:** Functional-inspired, modular, custom AST/VM/compiler
- **Domain:** Parser, Compiler, and Virtual Machine for a experimental secure and robust programming language

## Project Structure
- **src/:** Main source code folder
  - **Common/:** Core utilities (mStd, mMaybe, mSpan, mTextStream, mParserGen, etc.)
  - **mIL_AST.cs, mIL_Parser.cs, mTokenizer.cs, mVM_Data.cs, mVM.cs:** Core compiler/VM modules
  - **mStdLib.cs:** Standard library loader and runner
  - **mSPO2IL.cs:** Main compiler logic (SPO AST -> IL)
  - **Test files:** Named `*.Tests.cs`, included via custom `<TestSrc>` in `.csproj`
- **Modules/:** Canonical compiled modules (`*.SPO` + generated `*.ILT`) used by `mModule_Tests` for standard-library validation.
- **Regression.Tests/:** Scenario fixtures; each trio of `.SPO`, `.result.SPO`, and `.ILT` feeds `mRegression_Tests` regression harness.

## Core source modules (src/)
- **Language pipeline**
  - Tokenizer
  - Parser
  - AST - abstract syntax tree
  - IL - intermediate language (generator)
  - VM - virtual machine
- **Testing infrastructure**
  - `Common/mTest.cs` implements the custom runner with test/group combinators and rich CLI filtering/logging support.
  - `mRunTests.cs` is the console entry point; run via `dotnet run --project SPO_CS.csproj --` with flags like `--list`, `--matchAll`, `--plainText`, or `--stopOnFirstFail`.
  - Unit suites live in `*.Tests.cs` modules and expose a static `mTest.tTest Tests` field registered through `mTest.Tests(...)`.
  - `mRegression_Tests` discovers fixtures in `Regression.Tests/`, executes each `.SPO`, compares against `.result.SPO`, and diffs generated `.ILT` output (writes `.ILT.new` on mismatch).
  - `mModule_Tests` loads canonical modules from `Modules/` (`Std`, `Char`, `Text`) to verify interpreter + VM integration against known tuples.

## Common utilities (src/Common/)
Reusable functional-style helpers (e.g., option types, result handling, parser combinators, collections) are grouped under Common (mMaybe.cs, mResult.cs, mParserGen.cs, etc.). These components underpin the language implementation with generic data structures, error handling, and stream parsing facilities.

## Coding Style
- **Identifier Placement**
  - Type and identifier are split across lines for readability:
    ```csharp
    public static class
    mAny_Tests {
      public static readonly mTest.tTest
      Tests = ...
    }
    ```
- **Collection Initializer**  
  - Uses C# 12+ collection expressions: `[ ... ]` instead of `new[] { ... }`
- **Tabs**
  - Use tabs for indentation, including on empty lines.
- **Line Length**
  - Lines are kept reasonably short for side-by-side diff readability.
- **Grouping**
  - Blank lines are used for logical grouping, and are indented to match code blocks.
- **Pattern Matching**
  - Prefer C# switch expressions with pattern matching to reduce boilerplate and improve readability.
- **Functional Style**
  - Many methods are static, use of tuples, and functional constructs (`Map`, `Where`, etc.).
- **Error Handling**
  - Custom error types and assertions (e.g., `mAssert`, `mError.Error`).

## Common Module Guidelines
- Declare dependencies with `// IMPORT <module>` comments instead of `using`.
- Module layout:
  ```cs
  public static class
  ModuleName {
    ...
  }
  ```
- Break method signatures so the return type and method name are on separate lines; each parameter occupies its own line.
- Prefix modules with `m` (e.g., `mStd`) and types with `t` (e.g., `tBool`, `tNat32`).
- For pure or frequently-called functions, include attributes:
  [Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
- Prefer expression-bodied members (`=>`) where practical.
- Avoid trailing whitespace and keep comments concise.

Types in this directory intentionally avoid the standard `object` methods such as `Equals`, `GetHashCode`, and `ToString`.
- Use `ToText` for string representations.
- Provide explicit comparers and hash functions when working with collections.
- Add dedicated equality or hashing helpers rather than relying on default `object` implementations.

Apply these rules when creating new types to keep behavior consistent with existing structures like `tAny`.

## Test Framework
- **Custom Test Framework**
  - Tests are defined as static fields using `mTest.tTest` and registered via `mTest.Tests(...)`.
  - Test files are named `*.Tests.cs` and linked in the `.csproj` via `<TestSrc>`.
  - Assertions via `mAssert`.

## Build & Run
- **AOT Support**  
  - `<PublishAot>true</PublishAot>` in `.csproj` for native builds.
  - Use `dotnet publish -c Release -r win-x64` for AOT; run the resulting `.exe` directly.
- **JIT for Development**  
  - Use `dotnet run --no-restore` for fast edit/run cycles.

## Miscellaneous
- **Imports**
  - Custom `// IMPORT ...` comments at the top of files for dependency clarity.
- **Standard Library**
  - Loaded from `Modules/Std.ILT` at runtime.

## Language Semantics for Assistants
- Bindings are **immutable by default**. Only constructs with `§VAR` explicitly mark mutable bindings or types.
- When mutations are necessary, apply the `§VAR` prefix intentionally and treat all other bindings as read-only.

---

**This file is intended to help AI assistants understand the project's conventions, structure, and philosophy for more effective code suggestions and reviews.**
