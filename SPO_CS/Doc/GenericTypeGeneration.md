# Generic Type Generation Notes

## Problem statement
- The regression fixture `07_12_GenericTypes.ILT` shows that the generated IL type table for generic helpers still contains standalone `[§FREE]` entries instead of binding the placeholder to the generic head that introduces it.【F:SPO_CS/Regression.Tests/07_12_GenericTypes.ILT†L2-L26】
- These free placeholders are emitted because the current lowering pipeline maps the universal quantifier head just like any other free type, so the resulting definitions never record how occurrences inside the body relate back to the binder.
- Downstream consumers like `mIL_GenerateOpcodes` therefore materialise VM types where the `Generic` wrapper keeps pointing at an unconstrained free slot, which is dropped when the procedure type is normalised.【F:SPO_CS/src/mIL_GenerateOpcodes.cs†L96-L136】

## Root cause in the pipeline
- `tModuleConstructor.MapType` simply recurses through the VM type graph. In the `Generic` branch it first calls itself on the head type, which is still a raw `Free`, so it creates a fresh `[§FREE]` definition and reuses that id for the entire body.【F:SPO_CS/src/mSPO2IL.cs†L202-L272】
- When the AST contains explicit generic type nodes we also emit `TypeFree` followed by `TypeGeneric`, but we never record the head symbol in the constructor's `TypeDict`. That means subsequent lookups of the parameter inside the body cannot resolve to a bound `TypeVar` instance either.【F:SPO_CS/src/mSPO2IL.cs†L1030-L1042】
- Finally, `CreateDefType` just wraps the annotated procedure type without inspecting whether the result is a `Generic`. No environment state is threaded that would allow us to map type ids from the lambda scope back to the enclosing module.【F:SPO_CS/src/mSPO2IL.cs†L309-L320】

## Implementation sketch
1. **Track generic binders during type lowering.**
   - Extend `MapType` with a lightweight “generic scope” (e.g. a `Dictionary<tNat64, tText>` keyed by the bound type's `DebugId`). Whenever we enter the `Generic` case we should:
     1. Allocate a dedicated type id for the binder (still emitted via `TypeFree` so the VM can allocate a placeholder),
     2. Register that mapping in the scope, and
     3. Recurse into the body while passing the updated scope.
   - When we later encounter a `Free` node whose representative is present in that scope, emit a `TypeVar` definition that aliases the previously created binder id instead of another `[§FREE]` entry. This keeps all uses of the parameter tied to the quantifier rather than floating independently.
2. **Expose binder information while compiling generic lambdas.**
   - When visiting `tGenericTypeNode` in `MapExpression`, add the freshly created binder id (and its `mVM_Type.Var` representation) to the def constructor's `TypeDict` so nested lookups can resolve it to the right VM type during environment construction.【F:SPO_CS/src/mSPO2IL.cs†L1030-L1042】
   - If the type body introduces additional type expressions, ensure those recursively respect the scope described in step 1, so the same binder id is threaded through nested `TypeGenericApply` nodes.
3. **Normalise definition signatures.**
   - Update `CreateDefType` (and the `FinishMapProc` call sites) to detect when the lambda's annotation is `Generic`. Split it into `(binder, innerProcType)` and return a `Proc` whose result is the generic wrapper plus explicit metadata about the binder id. That metadata can be reused when instantiating closures so that calls like `InitProc` have access to the quantifier during environment lowering.【F:SPO_CS/src/mSPO2IL.cs†L309-L320】
   - When the enclosing module writes the final type table (`EnsureTypeDefinition`), inject the `TypeVar` entry for each binder before emitting the `TypeGeneric` node so readers observe a stable sequence of `(binder placeholder → var alias → generic wrapper)`.
4. **Regression protection.**
   - Once the binding logic is in place, adjust the `07_12_GenericTypes.ILT` expectation so the def types reference `[§VAR …]` entries instead of loose `[§FREE]` placeholders. Add an additional regression that instantiates the same generic twice in one module to verify that the scope map prevents duplicate binder ids.

This staged approach keeps the existing recursion support untouched while introducing a clear flow for generic placeholders: the scope map guarantees structural sharing during type lowering, and the augmented def constructor state lets us thread those bindings all the way to the emitted IL.
