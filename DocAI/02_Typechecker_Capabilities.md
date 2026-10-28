# Typechecker, Capabilities, and Local Scopes

## Security Model

- Untrusted SPO/ILT code receives no implicit global resource authority.
- **Types** describe valid values/operations; **access facts** describe how mutable components may be used here.
- A compatible type annotation grants no additional access rights. A record may contain extra fields hidden by its contract.
- Pure functions may create and return local cells. Access to existing mutable state requires an authorized method receiver.
- External effects come through explicit capabilities. C# callbacks, runtime, and native backends are trusted host code and must uphold the contract.

## Access by Origin

| Origin | Rule inside the body |
| --- | --- |
| `ENV` = closure environment | Transitively immutable; no cells, hidden mutable fields, or mutable interface receivers |
| `ARG` = ordinary argument | Mutable components are **opaque**: transport/package/forward allowed; read, write, or use as a method receiver forbidden |
| `OBJ` = method receiver | Explicit authority over the supplied mutable components |
| Fresh local `§VAR` | Locally accessible; ordinary function results may contain fresh cells |
| Guaranteed immutable projection | Still readable, even from an aggregate with opaque components |

- `§DEF` binds a name; it does not make a contained `§VAR` reference immutable.
- `§VAR aCell` in an **ordinary** parameter pattern grants no read authority either. `ARG` versus `OBJ` is what matters.
- A mutable interface passed as an ordinary argument may be forwarded, but cannot become a method receiver inside that body.
- `tType.IsImmutable` guarantees immutability of **all** values of that type. `ANY`, open records, and unresolved free types provide no such guarantee.
- A type-level value is immutable even if it describes `[§VAR T]`. A function value is immutable under the checked ENV invariant.

## Scoping and Zero Trust

| Boundary | Consequence |
| --- | --- |
| Lexical block/function body | Local bindings do not escape their scope; returns are explicit. Visibility alone does not permit capturing mutable values. |
| Callback | Pass required state through arguments/receivers. No hidden access through captured mutable variables. |
| Method callback with context | Only cells explicitly bundled in `OBJ` receive authority; additional ordinary arguments stay opaque. |
| SIG opening | A fresh abstract witness is local. Dependent result types cannot escape unpackaged; repackage with `§SIG`. |
| Native capability | Also checks receiver identity and lifetime at runtime; lexical scoping does not replace this check. |

- Compiler-generated IF/MATCH helpers obey the closure rule: `InitLocalCall` puts provably immutable values in `ENV`, other lexical intermediate values in `ARG`; the method receiver travels explicitly through `OBJ`.
- IF evaluates only the selected result branch. Lowering must preserve early RETURNs and the possible empty result when no branch matches.
- Concrete callback example: [GUI.Scopes.spo](../SPO/Tests/GUI.Scopes.spo), nested `DoWithContext` calls with explicit `(Panel, Count, Other)` receivers.

## How the IL Checker Tracks Rights

Source: [mIL_TypeChecker.cs](../SPO_CS/src/mIL_TypeChecker.cs); details: [MutableAccess.md](../SPO_CS/MutableAccess.md).

- `TypeDef`: entire body → typed commands with source positions, value IDs, operand types, and completed type refinements.
- `PrimitiveTypes`, `StructureTypes`, `SigTypes`, `CallTypes`, `ControlTypes` return type facts; only `TypeDef` applies refinements.
- `AreDefAccessesValid`: separate pass with `tAccessValue(Mutable, Opaque, Cell, BoundReceiver)`.
- Name aliases share a value ID. Direct VAR references retain local cell identity; cell contents are tracked separately.
- **One aggregate has one access summary.** No finer rights per tuple component, record field, prefix, or SIG body.
- An opaque component makes the aggregate's mutable projections opaque. A narrower type annotation removes neither hidden fields nor this restriction.
- Storing opaque values or passing them to methods may change cell contents. Previously known mutable cell contents/aggregates become conservatively opaque; old aliases must not restore stale rights.
- This invalidation may affect unrelated aggregates too. Keep values separate when they need different rights.
- **Call results:** immutable parts stay accessible; mutable parts are accessible if the ordinary argument contains no opaque mutables. Forbidden captures are rejected earlier.
- Module exports with possibly mutable values are rejected statically; actual import/export values are also checked by the VM. Immutable factories may be exported.

## Type Inference and Identity

Sources: [mSPO_AST_Types.cs](../SPO_CS/src/mSPO_AST_Types.cs), [mVM_Type.cs](../SPO_CS/src/mVM_Type.cs).

- Local and primarily argument-driven: no global solver or general backward inference from the desired result.
- Higher-order calls supply expected parameter types to argument functions; their results can refine further arguments.
- Recursive callables receive signatures first; result equations are resolved and structural self-references become recursive types.
- `SameType`: structural equality accounting for binder renaming/union order; free variables and abstract witnesses retain identity.
- `IsSubType`: returns a substitution map. Function arguments are contravariant, results covariant; method objects and cell contents invariant.
- `EMPTY`/`[]` is a real value type. Missing inference result = `None`, not `EMPTY`; preserve this distinction in unions.
- `Free` = inferable/bindable parameter; `Abstract` = fixed unknown type symbol, not a variable to solve to a convenient concrete type.
- Distinguish a type-level value, its `KindType()`, and the register's type. `ApplyType` substitutes known generics, otherwise produces symbolic `TypeApply`.

## Interfaces Restrict Authority

- `§INTERFACE Contract WITH Object IN Methods` binds a hidden receiver to named methods.
- The hidden type may occur in the contract only as the first method receiver; methods cannot be extracted as ordinary record fields.
- Narrowing an interface actually removes methods from the runtime value. `ANY`, MATCH, records, or forwarding must not recover removed rights.
- [mIL_GenerateOpcodes.cs](../SPO_CS/src/mIL_GenerateOpcodes.cs), `Adapt`, generates conversions and ordinary procedure adapters, including at function arguments/results and inside composite values.
- [mVM.cs](../SPO_CS/src/mVM.cs), `SubInterface`, builds the restricted method table; this is more than a static view of retained admin rights.
- Native terminal/GUI capabilities bind a private handle. Every callback checks `Active` and `ReferenceEquals(Receiver, Handle)`; `finally` expires the capability when `WithCapability` ends.
- `Proc`/`ExternProc` check the actual SPO environment's `_IsMutable`. A C# delegate can still capture host state, outside SPO analysis.

## Focused Reading / Tests

Fixtures below are relative to `SPO_CS/TestFiles/`:

| Topic | Fixture |
| --- | --- |
| Opaque transport / packaging | `SPO_Functions/09_01_MutableTransport.SPO`, `09_02_MutablePackaging.SPO` |
| Authorized receiver + immutable projection | `SPO_Functions/09_10_MutableReceiverAndImmutableProjection.SPO` |
| Fresh cell results | `SPO_Functions/11_12_RegressionFreshFactoryResults.SPO` |
| Forbidden reads / captures / hidden fields | `SPO_Function_Errors/04_01_MutableArgumentRead.SPO`, `04_02_MutableCapturedReceiverRead.SPO`, `04_25_MutableNarrowedRecordResultAsReceiver.SPO` |
| Alias after a method call | `ILT_Function_Errors/03_32_MutableReceiverAliasAfterCall.ILT` |
| Rights after interface narrowing | `SPO_Functions/10_05_InterfaceAuthority.SPO`, `10_07_InterfaceAdapters.SPO` |