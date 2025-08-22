# AI_Info.txt – Project Guidance for AI Assistants

## Project Overview
- **Project Name:** SPO_CS
- **Language:** C# (modern, .NET 9, C# 13 features)
- **Paradigm:** Functional-inspired, modular, custom AST/VM/compiler
- **Domain:** Parser, Compiler, and Virtual Machine for a custom language

## Project Structure
- **src/**: Main source code folder
  - **Common/**: Core utilities (mStd, mMaybe, mSpan, mTextStream, mParserGen, etc.)
  - **mIL_AST.cs, mIL_Parser.cs, mTokenizer.cs, mVM_Data.cs, mVM.cs**: Core compiler/VM modules
  - **mStdLib.cs**: Standard library loader and runner
  - **mSPO2IL.cs**: Main compiler logic (SPO AST → IL)
  - **Test files**: Named `*.Tests.cs`, included via custom `<TestSrc>` in `.csproj`
- **Std.ILT**: Standard library in custom IL format, loaded at runtime

## Core source modules (src/):

- **Language pipeline**
  - Tokenizer, parser, abstract syntax tree (AST), intermediate language (IL) generator, and virtual machine reside directly under src (e.g., mTokenizer.cs, mSPO_Parser.cs, mSPO_AST.cs, mSPO2IL.cs, mVM.cs).
- **Standard library**
  - Implemented in mStdLib.cs, providing basic language functions and utilities.
- **Testing infrastructure**
  - mRunTests.cs plus numerous *.Tests.cs files for unit testing each module, alongside .SPO test programs in src/Tests/ such as Echo.SPO and IfThenElse.SPO.

## Common utilities (src/Common/):

Reusable functional-style helpers (e.g., option types, result handling, parser combinators, collections) are grouped under Common (mMaybe.cs, mResult.cs, mParserGen.cs, etc.). These components underpin the language implementation with generic data structures, error handling, and stream parsing facilities.

## Usage flow

Typical execution follows this chain:

1. Tokenization (mTokenizer.cs)
2. Parsing and AST construction (mSPO_Parser.cs, mSPO_AST.cs)
3. IL generation (mIL_AST.cs, mSPO2IL.cs)
4. Virtual machine execution (mVM.cs, mSPO_Interpreter.cs)

The tests and .SPO samples demonstrate how the language components interact and provide examples for experimentation.

## Coding Style
- **Identifier Placement:**  
  - Type and identifier are split across lines for readability:
    ```csharp
    public static class
    mAny_Tests {
      public static readonly mTest.tTest
      Tests = ...
    }
    ```
- **Collection Initializer:**  
  - Uses C# 12+ collection expressions: `[ ... ]` instead of `new[] { ... }`
- **Tabs:**  
  - Use tabs for indentation, including on empty lines.
- **Line Length:**  
  - Lines are kept reasonably short for side-by-side diff readability.
- **Grouping:**  
  - Blank lines are used for logical grouping, and are indented to match code blocks.
- **Pattern Matching:**  
  - Prefer C# switch expressions with pattern matching to reduce boilerplate and improve readability.
- **Functional Style:**  
  - Many methods are static, use of tuples, and functional constructs (Map, Where, etc.).
- **Error Handling:**  
  - Custom error types and assertions (e.g., `mAssert`, `mError.Error`).

## Common Module Guidelines

- Declare dependencies with `// IMPORT <module>` comments instead of `using`.
- Module layout:
  public static class
  ModuleName {
    ...
  }
- Break method signatures so the return type and method name are on separate lines; each parameter occupies its own line.
- Prefix modules with `m` (e.g., mStd) and types with `t` (e.g., tBool, tNat32).
- For pure or frequently‑called functions, include attributes:
  [Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
- Prefer expression-bodied members (`=>`) where practical.
- Avoid trailing whitespace and keep comments concise.

Types in this directory intentionally avoid the standard `object` methods such as `Equals`, `GetHashCode`, and `ToString`.

- Use `ToText` for string representations.
- Provide explicit comparers and hash functions when working with collections.
- Add dedicated equality or hashing helpers rather than relying on default `object` implementations.

Apply these rules when creating new types to keep behavior consistent with existing structures like `tAny`.

## Test Framework
- **Custom Test Framework:**  
  - Tests are defined as static fields using `mTest.tTest` and registered via `mTest.Tests(...)`.
  - Test files are named `*.Tests.cs` and linked in the `.csproj` via `<TestSrc>`.
  - Assertions via `mAssert`.

## Editor/Tooling
- **.editorconfig:**  
  - Tabs for indentation, trailing whitespace allowed (even on empty lines).
  - `dotnet_diagnostic.IDE0055.severity = none` to suppress formatting warnings.
- **VS Code:**  
  - Split editor is used for parallel editing of similar files or code sections.
  - Modern C# features enabled (`LangVersion=13.0`).

## Build & Run
- **AOT Support:**  
  - `<PublishAot>true</PublishAot>` in `.csproj` for native builds.
  - Use `dotnet publish -c Release -r win-x64` for AOT; run the resulting `.exe` directly.
- **JIT for Development:**  
  - Use `dotnet run` for fast edit/run cycles.

## Design Notes
- **Pattern Matching:**  
  - There are often two versions of pattern-matching functions: one for guaranteed matches (e.g., assignments), one for runtime-checked matches (e.g., if/switch).
  - Code duplication is accepted for clarity; abstraction via flags or visitors is possible but not always preferred.
- **File Organization:**  
  - Related pattern-matching cases may be grouped in the same file or split for clarity.

## Miscellaneous
- **Imports:**  
  - Custom `// IMPORT ...` comments at the top of files for dependency clarity.
- **Standard Library:**  
  - Loaded from `src/Std.ILT` at runtime.

---

**This file is intended to help AI assistants understand the project's conventions, structure, and philosophy for more effective code suggestions and reviews.**