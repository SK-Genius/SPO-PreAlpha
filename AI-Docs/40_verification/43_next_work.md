# Next Work

This list is intentionally not a general wishlist. It prioritizes only tasks that can be derived directly from the current code, the existing tests, and reproducible runtime observations.

## Priority 1

1. Stabilize generic type application across module boundaries.
	- Observation: `mModule_Tests.Maybe` fails reproducibly with `expected generic type but '_tMaybe...'`.
	- Verified failure boundary: the failure is in [SPO_CS/Modules/Maybe.SPO](../../SPO_CS/Modules/Maybe.SPO) at `.tMaybe tIn`, and it is triggered while [SPO_CS/src/mModule.cs](../../SPO_CS/src/mModule.cs) runs the `.SPO -> .ILT` check through `mSPO_Parser.Module.ParseText(...).ToILT()`, where `ToILT()` is currently defined in [SPO_CS/src/mSPO_Interpreter.cs](../../SPO_CS/src/mSPO_Interpreter.cs). That path reaches the generic-application branch in [SPO_CS/src/mSPO_AST_Types.cs](../../SPO_CS/src/mSPO_AST_Types.cs) before any VM execution happens.
	- Relevant locations:
		[SPO_CS/src/mModule.Tests.cs](../../SPO_CS/src/mModule.Tests.cs),
		[SPO_CS/src/mSPO_AST_Types.cs](../../SPO_CS/src/mSPO_AST_Types.cs),
		[SPO_CS/src/mSPO_Interpreter.cs](../../SPO_CS/src/mSPO_Interpreter.cs),
		[SPO_CS/src/mModule.cs](../../SPO_CS/src/mModule.cs),
		[SPO_CS/Modules/Maybe.SPO](../../SPO_CS/Modules/Maybe.SPO).
	- Why first: This is currently the most painful real system boundary, because generics work in regressions, but are still not reliable in library modules.
	- First debugging order:
    	1. Re-run `dotnet run --project .\\SPO_CS.csproj -- --plainText --stopOnFirstFail --matchAny mModule_Tests` from `SPO_CS/`.
    	2. Inspect `.tMaybe tIn` in `Modules/Maybe.SPO`.
    	3. Inspect `mSPO_AST_Types.AsVM_Type(GenericApplyType)` and identifier/type lookup around it.
    	4. Only after that, inspect IL/runtime layers if the failure moves downstream.
	- Inference to verify: the current error text suggests the head of the generic application is arriving as a named type reference (`_tMaybe...`) instead of an already materialized `mVM_Type.Generic`.

2. Complete `mVM_Type.IsSubType(...)` for `Any`, `Var`, `Ref`, and `Cond`.
	- Observation: In [SPO_CS/src/mVM_Type.cs](../../SPO_CS/src/mVM_Type.cs), these cases still throw `NotImplementedException`.
	- Why next: These gaps make type rules visible more broadly than they are actually decidable today.

3. Bring runtime type construction into sync across enum, IL generator, and VM.
	- Observation: [SPO_CS/src/mVM_Data.cs](../../SPO_CS/src/mVM_Data.cs) defines a broader `Type*` opcode family than [SPO_CS/src/mVM.cs](../../SPO_CS/src/mVM.cs) executes, and [SPO_CS/src/mIL_GenerateOpcodes.cs](../../SPO_CS/src/mIL_GenerateOpcodes.cs) already emits additional runtime type-building commands such as `TypeFunc`, `TypeMethod`, `TypePrefix`, `TypeVar`, and `TypeInterface`.
	- Why now: The architecture uses `mVM_Type` across all phases; once type values are moved around at runtime, partial support across these three layers becomes a real maintenance and correctness risk.

## Priority 2

4. Either implement `TryAsType` / `TryAsRef` end-to-end or stop presenting them as executable VM-facing IL.
	- Observation: [SPO_CS/src/mIL_AST.cs](../../SPO_CS/src/mIL_AST.cs), [SPO_CS/src/mIL_Parser.cs](../../SPO_CS/src/mIL_Parser.cs), and [SPO_CS/src/mIL_GenerateOpcodes.cs](../../SPO_CS/src/mIL_GenerateOpcodes.cs) all know these opcodes, but [SPO_CS/src/mVM.cs](../../SPO_CS/src/mVM.cs) has no execution case for them, and [SPO_CS/src/mVM_Data.cs](../../SPO_CS/src/mVM_Data.cs) does not define a `Ref` runtime value kind.
	- Why: Current IL text can suggest runtime matching features that the VM still cannot execute.

5. Either finish `TypeCond` end-to-end or mark it clearly as internal / unfinished.
	- Observation: The IL parser and IL AST know `TypeCond`, but [SPO_CS/src/mIL_GenerateOpcodes.cs](../../SPO_CS/src/mIL_GenerateOpcodes.cs) still blocks the case with `NotImplementedException`.
	- Why: Otherwise this remains a visible feature without a robust backend path.

6. Materialize multi-recursion in `DefRecProcs` for real.
	- Observation: [SPO_CS/src/mVM.cs](../../SPO_CS/src/mVM.cs) currently uses `Count = 1` in the `DefRecProcs` path.
	- Why: Single recursive definitions exist, but mutual recursion remains a structural risk area.

7. Harden hash-based prefix and record-field identity.
	- Observation: [SPO_CS/src/mIL_GenerateOpcodes.cs](../../SPO_CS/src/mIL_GenerateOpcodes.cs) and [SPO_CS/src/mVM_Data.cs](../../SPO_CS/src/mVM_Data.cs) already mark collisions around `PrefixHash()` as TODOs.
	- Why: The more modules and field names exist, the less acceptable it is for identity to depend only on a hash.

## Priority 3

8. Factor the parsed-module `ToILT()` helper out of `mSPO_Interpreter`.
   - Observation: [SPO_CS/src/mSPO_Interpreter.cs](../../SPO_CS/src/mSPO_Interpreter.cs) currently owns `ToILT()`, and both [SPO_CS/src/mModule.cs](../../SPO_CS/src/mModule.cs) and [SPO_CS/src/mRegression.Tests.cs](../../SPO_CS/src/mRegression.Tests.cs) reach it through `mSPO_Parser.Module.ParseText(...).ToILT()`.
   - Why: This is not a runtime bug, but it still keeps parsing, desugaring, type analysis, and lowering bundled into one convenience path instead of one explicit compilation service.

9. Extend type desugaring beyond tuple types.
	- Observation: [SPO_CS/src/mSPO_Desugar.cs](../../SPO_CS/src/mSPO_Desugar.cs) actively normalizes only tuple types; many other type nodes are currently passed through.
	- Why: More normalization before lowering would reduce later special cases in type analysis and backend code.

10. Decide whether `SPO_CS/Modules/Result.SPO` should be wired into `mModule` as an official module.
	- Observation: The artifact exists, but [SPO_CS/src/mModule.cs](../../SPO_CS/src/mModule.cs) currently exposes only `Std`, `Char`, `Text`, and `Maybe`.
	- Why: Not an immediate bug, but it is a visible mismatch between the filesystem and the officially supported module graph.

11. Resolve regression fixtures relative to the project instead of the current working directory.
	- Observation: [SPO_CS/src/mRegression.Tests.cs](../../SPO_CS/src/mRegression.Tests.cs) uses `mFS.CWD() / "Regression.Tests"` and prints `Folder not found: mFS+tFolder` when launched from the repo root.
	- Why: This makes verification brittle, silently removes the regression suite from discovery, and has already forced the AI docs to document a workdir-specific workaround.

## Recommended Order

1. Fix the `Maybe` failure.
2. Close the subtyping gaps.
3. Finish runtime type opcodes.
4. Resolve the `TryAsType` / `TryAsRef` execution gap.
5. Finish `TypeCond`.
6. Harden recursion and hash-related issues.
7. Make regression discovery location-stable.
8. Clean up parser / desugaring coupling afterward.
