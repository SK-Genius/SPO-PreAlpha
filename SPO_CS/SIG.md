# SIG: AI implementation reference

Audience: agents changing SPO's parser, type checking, IL lowering, VM, or modules.
This is a code map and a record of design decisions, not a language specification.
Paths are relative to `SPO_CS/`. User-facing syntax and examples belong in
[the SPO cheat sheet](../doc/CheatSheet_SPO.wiki).
Verify implementation details in the linked source before changing them.

## Design context

- SIG is primarily a module interface: export an abstract representation type
  together with values and functions that use it.
- A consumer must be able to require that two imported modules use the same
  abstract type, so a value from one can be passed to functions from the other.
- Inspecting representations through SIG patterns in `MATCH` is existing behavior,
  not a requested use case. Representation leaks are neither a required feature
  nor categorically forbidden. Do not infer either requirement from the tests.
- Keep the implementation small. Prefer existing type operations and opcodes;
  a new abstraction or opcode needs a concrete SPO use case.
- Regression tests record existing behavior. A failure requires assessing the
  change; neither preserving every old result nor replacing it is automatic.

## Existing examples to read

Use these files instead of adding untested examples to documentation:

| Purpose | Files |
| --- | --- |
| Small real module with an abstract type constructor | [Span.SPO](Modules/Span.SPO), [Span.SIG](Modules/Span.SIG), [SpanConsumer.SPO](TestFiles/Modules/SpanConsumer.SPO), [expected result](TestFiles/Modules/SpanConsumer.result.SPO) |
| Larger generic APIs | [List.SIG](Modules/List.SIG), [List.SPO](Modules/List.SPO); [Result.SIG](Modules/Result.SIG), [Result.SPO](Modules/Result.SPO) |
| Shared abstract type across imports | [SigImportSameHead.SPO](TestFiles/Modules/SigImportSameHead.SPO), [expected result](TestFiles/Modules/SigImportSameHead.result.SPO) |
| Different imported type rejected | [SigImportDifferentHead.SPO](TestFiles/Modules/SigImportDifferentHead.SPO), [expected result](TestFiles/Modules/SigImportDifferentHead.result.SPO) |

The last two cases use providers in [TestFiles/Modules/_Imports](TestFiles/Modules/_Imports/).
Their exports and consumer imports load the same `.SIG` contracts.
`SigBoolReader.SIG` loads `SigReader.SIG` rather than duplicating it.

## Semantic model and invariants

### Contract, Head, and Body

A contract is `mVM_Type.Sig(Binder, BodySignature)`.
A runtime value is `mVM_Data.Sig(Contract, Head, Body)`; all three are retained.

Construction checks:

- `Head.KindType().SameType(Binder.KindType())`.
- The Body's static type is a subtype of
  `BodySignature.Substitute(Binder, Head)`.
- The complete expression has the contract as its static type.

Do not replace the Body subtype check with exact equality. Do not remove the
stored contract because two contracts happen to produce the same Body type for
one Head. Runtime `Matches` checks contract compatibility with `IsSubType`,
Head kind, and the Body after substituting the actual Head.

### FREE parameters and ABSTRACT witnesses

- `Free` declares a parameter, including the binder of a SIG contract.
  Substitution and inference must respect its kind.
- Opening a SIG introduces an `Abstract` witness: a fixed unknown type-level
  value. Inference must not solve it to a convenient concrete type.
- Separate openings create separate witnesses. Generated helper functions for
  one opening must retain the witness created by the SPO checker.
- A Head pattern naming an existing type or constructor uses `SameType` on the
  actual Head. Subtyping is insufficient. On success, substitute that Head into
  the Body type.
- Binding with `WITH §DEF ...` or ignoring with `WITH _` needs no exact Head test.
  Ignoring the name still leaves an abstract type if the Body depends on it.
- An abstract Head cannot escape its scope through a result type, function type,
  or closure. Repackage dependent values with SIG. Results whose types no longer
  mention that Head can leave directly.

`SameType` compares bound types independently of binder names, includes record
fields, and ignores union order. Free variables and abstract witnesses retain
reference identity. Do not replace this operation with textual names,
`ToText()`, or the general `tType.operator==`.

### Type-level values, kinds, and signatures

Keep a type-level value separate from its kind and from the type of the runtime
register holding it.

- `KindType` returns the kind of a type-level value.
- `IsSignature` accepts ordinary types and declared generic signature families.
  Accepting a generic abstraction as a signature does not change its function kind.
- `[§GENERIC t Body]` in SPO and `[§ALL t => Body]` in IL use the same
  `tKind.Generic` representation. There is no second constructor representation.
- `ApplyType` substitutes a known Generic binder. An unknown constructor produces
  a symbolic `TypeApply`. Partial applications may still have a function kind.
- An abstract constructor cannot itself serve as a Body or field type when its
  kind is still a function. Apply it until the result is a type.
- `AsVM_Value` reads a type-level value; `AsVM_Type` additionally requires
  `IsSignature`. VM `TypeExpressionValue()`, `SignatureValue()`, and
  `TypeValue()` enforce the corresponding distinctions.
- Generic function signatures still require generic behavior; a monomorphic
  function that works for one instantiation is insufficient. Existing inference
  is local and driven by arguments, including higher-order argument feedback.

## Implementation map

| Area | Source and entry points |
| --- | --- |
| SPO syntax and signature loading | [mSPO_Parser.cs](src/mSPO_Parser.cs): `SigType`, `Sig`, `SigPattern`, `SigHeadPattern`, `Signature`, `LoadSig` |
| AST | [mSPO_AST.cs](src/mSPO_AST.cs): `tSigTypeNode`, `tSigNode`, `tSigPatternNode.HeadValue`, `tIdNode.TypeValue` |
| SPO checking | [mSPO_AST_Types.cs](src/mSPO_AST_Types.cs): `AsVM_Value`, `AsVM_Type`, `UpdateTypes`, `UpdatePatternTypes`, `HasChildWith` |
| Shared type operations | [mVM_Type.cs](src/mVM_Type.cs): `KindType`, `IsSignature`, `ApplyType`, `Substitute`, `SameType`, `IsSubType`, `ApplyMappings` |
| Lowering | [mSPO2IL.cs](src/mSPO2IL.cs): `MapType`, `MapTypeValue`, `MapSigPattern`, `MapPattern`, `TryBindMatchedPattern`, `TryMapPatternGuard` |
| IL syntax | [mIL_AST.cs](src/mIL_AST.cs), [mIL_Parser.cs](src/mIL_Parser.cs): command definitions, text rendering, parsing |
| IL checking and opcode generation | [mIL_GenerateOpcodes.cs](src/mIL_GenerateOpcodes.cs): `CompileModule`, `CreateTypeExpression`, local `GetReg`, `KnownValues` |
| Runtime | [mVM_Data.cs](src/mVM_Data.cs): SIG data and opcode definitions; [mVM.cs](src/mVM.cs): `Matches` and opcode execution |
| Module initialization and file tests | [mModule.cs](src/mModule.cs): `ModuleSetup`, `Init`; [mE2E.Tests.cs](src/mE2E.Tests.cs): function and module consumers |

The full SPO path starts in [mSPO_Interpreter.Run](src/mSPO_Interpreter.cs):
parse, desugar, check import/commands, lower, compile IL, execute.
SPO, IL, and VM checks intentionally serve different purposes; they are not
redundant checks to remove merely because the compiler emitted the IL.

## IL operations and operand contracts

Forms below are schematic instruction syntax, not standalone example programs.

| Operation / IL form | Current meaning |
| --- | --- |
| `Sig`: `p := §SIG Contract WITH Payload` | Payload is the pair `(Head, Body)`. Contract is a declared type reference or a type-valued register. Checks kind and dependent Body type. VM opcode: `NewSig`. |
| `TryAsSig`: `m := §TRY p AS_SIG Contract` | Contract must name a SIG declaration in `§TYPES`, with a FREE binder. Checks the package against it; returns the entire package and gives it an abstract witness in IL typing. VM receives a type-definition index. |
| `TryHasHeadType`: `m := §TRY p HAS_HEAD_TYPE Expected` | Input is already a SIG. Expected can be a declared type or a type-valued register, including a captured Head. Exact Head check; returns the entire package and refines its Body type. |
| `SigHead`: `h := §SIG_HEAD p` | Projects the actual Head. IL opens a still-FREE binder with a fresh abstract witness and updates the input register's SIG type. |
| `SigHeadAs`: `h := §SIG_HEAD p AS Witness` | Witness must name an ABSTRACT declaration of the matching kind. Opens the input using that specific witness. This preserves the identity already used by generated function signatures. No runtime Head comparison. |
| `SigBody`: `b := §SIG_BODY p` | Projects the Body with the dependent type established for the input SIG; opens a still-FREE binder if necessary. |
| `Alias`: `b := a` | Reuses the same register; emits no VM opcode and performs no type assertion. |

`SigHead` and `SigHeadAs` both emit the VM opcode `SigHead`.
They are distinct IL operations with fixed operands.
`AliasAs` has been removed; do not reintroduce it as a second witness mechanism.

Both `TryAsSig` and `TryHasHeadType` abort the current procedure on mismatch;
they do not return a boolean. The different-Head import regression records the
resulting empty module result.

`MapSigPattern` emits an exact Head check only for a type/existing-Head pattern,
then projects Head and Body. It uses `SigHeadAs` for an abstract Head recorded by
the SPO checker. MATCH branch dispatch already checks the contract through
`§TRY_RETURN` and the branch argument type; it does not need an additional
`TryAsSig` solely to repeat that check.

Closed type-level values can be declared once in `§TYPES` and used as value
operands. `GetReg` resolves these references and emits the VM opcode `LoadType`;
it does not add a textual IL load instruction. Types depending on runtime
parameters or opened Heads must be built with their actual values inside the
function. A raw unbound FREE parameter cannot be used as an ordinary value.

The former VM opcode name `TypeValue` is now `LoadType`. The accessor
`mVM_Data.TypeValue()` and AST/scope `TypeValue` fields still exist and have
different roles.

## State and identity when editing

- `tTypeState.ValidatedTypes` is an immutable TreeMap snapshot of inferred types,
  keyed by AST identity with hash-collision buckets. Carry the returned state
  through child checks and branches. Failed checks must preserve the input state.
  Declared AST annotations are separate; `HeadValue` and `TypeValue` also remain
  AST metadata used during lowering.
- `mVM_Type.IsSubType` returns a TreeMap of substitutions keyed by free-variable
  identity. Rebinding replaces the entry and preserves previous snapshots.
- `mSPO2IL.tTypeDeclarations` contains `TypeDef`, `Types`, and `TypeIds`.
  Helpers receive this small struct by `ref` because they append declarations and
  replace TreeMap roots. Passing only a read-only mapping would lose updates.
  Avoid passing the whole module constructor to helpers that only need this state.
- IL register `Types` and `KnownValues` are different: one stores register types,
  the other known/symbolic values. Head projection records the witness as a known
  type-level value while the register's type is the witness's kind.
- `HasChildWith` is a private static extension that traverses the type graph,
  including record fields, and stops cycles by reference identity. The caller
  supplies the condition; abstractness is not hard-coded into the traversal.

## Signature files and tooling

A `.SIG` file contains one complete SPO type definition with optional surrounding
whitespace, without a `§DEF`, `§IMPORT`, or `§EXPORT` wrapper. It need not be a
SIG contract; ordinary type definitions are valid too.

`[§LOAD "File.SIG"]` is a type parser form; brackets are required. Loading:

- resolves paths relative to the containing source/signature file; a missing
  source ID falls back to CWD; absolute paths are accepted;
- accepts `.SIG` case-insensitively, allows nested and repeated independent loads,
  and rejects missing files, incomplete/extra definitions, and load cycles;
- propagates [mTokenizer.tFileContext](src/mTokenizer.cs) with `ReadText` and
  `LoadPath`, so nested loads preserve the supplied reader and cycle detection;
- reports `tLoadSigError` with the load position and nested error/load chain;
- happens during SPO parsing; generated IL contains the type definitions, not
  `§LOAD`, and the IL parser does not support that form.

The existing reusable contracts are in `Modules/*.SIG`; test-provider contracts
are in `TestFiles/Modules/_Imports/*.SIG`. Do not assume a `Types/` folder exists.

VSCode signature diagnostics and navigation use the shared parser:
[mSPO_Diagnostics.cs](../VSCode_Extension/server-src/mSPO_Diagnostics.cs) and
[mSPO_Navigation.cs](../VSCode_Extension/server-src/mSPO_Navigation.cs).
The wiki maps `.SIG` to the SPO viewer through
[viewer.ini](../js/viewer.ini) and [viewer.spo.js](../js/viewer.spo.js).

## Verification and test placement

Run commands from `SPO_CS/`; filters must be the final arguments:

```powershell
# SIG regressions plus real module consumers
dotnet src/mRunTests.cs -- -t -g -m SIG Sig HasHeadType SpanConsumer ListConsumer ResultConsumer 07_20_GenericSignatureAlias

# Signature loading
dotnet src/mRunTests.cs -- -t -g -M mSPO_Parser LOAD

# Full suite
dotnet src/mRunTests.cs -- -t -g
```

Use `-l` to list matching tests without executing them. Filters are
case-sensitive substring matches; `-m` means any, `-M` means all.

- SPO behavior belongs in `TestFiles/Functions/` (end to end) or
  `TestFiles/Modules/` (module dependencies and shared abstract types).
  Function cases receive Std exports directly; module cases receive the module
  record and the providers from `_Imports/`.
- Use focused C# tests for isolated properties not already covered by those
  cases: [mVM_Type.Tests.cs](src/mVM_Type.Tests.cs),
  [mSPO_AST_Types.Tests.cs](src/mSPO_AST_Types.Tests.cs),
  [mIL_GenerateOpcodes.Tests.cs](src/mIL_GenerateOpcodes.Tests.cs),
  [mIL_Parser.Tests.cs](src/mIL_Parser.Tests.cs), and
  [mSPO_Parser.Tests.cs](src/mSPO_Parser.Tests.cs).
  Keep [mModule.Tests.cs](src/mModule.Tests.cs) about the actual `Modules/` files.
- Existing `03_18`–`03_22` SIG MATCH cases and `07_19_SigHigherKind` /
  `07_20_GenericSignatureAlias` are regressions, not additional design use cases.
- Check SPO output, generated IL, and IL execution against the paired
  `.result.SPO`. Test execution and module initialization can rewrite `.ILT`
  files; module-consumer generation checks report changed IL as a failure.
  Inspect the generated diff before deciding whether to accept it.

Keep documentation examples tied to tested source/result files. Update this
reference when invariants or phase responsibilities change; do not record a
transient test count or pass/fail snapshot.

## Related pipe behavior

[04_10_PipeWithoutArguments.SPO](TestFiles/Functions/04_10_PipeWithoutArguments.SPO)
and its [expected result](TestFiles/Functions/04_10_PipeWithoutArguments.result.SPO)
cover both directions: a pipe adds only the piped value, never an implicit
`()` argument. An explicitly supplied `()` remains a real argument; the called
name determines the insertion position. Lowering starts in
[mSPO_Desugar.cs](src/mSPO_Desugar.cs).
