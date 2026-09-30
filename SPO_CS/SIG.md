# SIG and Pipes

## SIG in one sentence

A SIG value packages two things together:

- a **Head**
- a **Body**

The important part is that the Head determines which type the Body must have.

A SIG contract describes that relationship.

```spo
§DEF sContract = [§SIG_WITH t € §TYPE IN t]
§DEF mPackage = §SIG sContract WITH §INT IN 7
```

The contract says:

- the Head must be a type,
- call that type `t`,
- the Body must have exactly that type `t`.

In `mPackage`, the Head is `§INT`. Therefore the Body must be an integer.
The Body is `7`, so the package is valid.

The static type of the complete package is `sContract`.

You can think of a SIG as a package whose **label determines what is allowed inside**.

## Type values and type functions

Types are values in SPO.

For example:

```text
§INT : §TYPE
```

A type function is a value that receives a type and produces a type.

For example, a value with the type

```spo
[§TYPE => §TYPE]
```

receives one type and returns another type.

If `tF...` is such a type function, then:

```spo
[.tF §INT]
```

applies it to `§INT`. The result is a type.

This distinction matters for SIG contracts.

```spo
[§SIG_WITH tF... € [§TYPE => §TYPE] IN [.tF §INT]]  // valid
[§SIG_WITH tF... € [§TYPE => §TYPE] IN tF...]        // invalid
```

In the first contract, `tF...` is applied and the result is a type.

In the second contract, `tF...` itself is still a type function. A type function
cannot directly be used where a concrete type is required, for example as the
Body type, a record field type, an argument type, or a result type.

A curried type constructor may require several applications. A partial
application may still be a type function. Only an application whose result is a
type can be used as a type.

## Generic type abstractions

In SPO:

```spo
[§GENERIC t Body...]
```

and in IL:

```text
[§ALL t => Body...]
```

represent the same kind of type abstraction.

For example:

```spo
[§GENERIC t [t => t]]
```

receives a type `t` and produces the function type:

```spo
[t => t]
```

Applied to `§INT`, it produces:

```spo
[§INT => §INT]
```

The abstraction itself therefore has the type:

```spo
[§TYPE => §TYPE]
```

If the Body is itself another type abstraction, the result is a curried type
function.

## Generic function signatures

A declared type abstraction can also describe a family of function signatures.

```spo
§DEF sSignature... = [§GENERIC t [t => t]]

§DEF Identity... € sSignature... = (
	§DEF t € §TYPE
) <=> (
	§DEF Value € t
) => Value
```

`sSignature...` describes the family:

```text
INT  => INT
BOOL => BOOL
t    => t
```

Applying the signature itself:

```spo
[.sSignature §INT]
```

produces:

```spo
[§INT => §INT]
```

Calling the function is different:

```spo
.Identity 7
```

Here the function argument determines that `t` is `§INT`, and the result is
`7`.

The signature annotation does not change the value or the type of the type
abstraction. It only states that the abstraction is used as a family of function
signatures.

When generic function signatures are compared, the existing inference from
function arguments still applies. The names and order of bound generic
parameters do not have to be identical if the resulting signatures describe the
same relationship.

A monomorphic function is not accepted merely because it works for one possible
type. For example, a function that works only for integers does not satisfy:

```spo
[§GENERIC t [t => t]]
```

because that signature requires the function to work for every allowed `t`.

A type abstraction is still a type function. It does not become a value of type
`§TYPE` merely because it is used as a generic function signature.

## Unpacking and matching a SIG

A SIG can be unpacked with pattern matching.

```spo
§EXPORT §IF mPackage MATCH {
	§SIG sContract WITH §INT IN §DEF Value : Value
	§SIG sContract WITH _ IN _ : 0
}
```

The first case means:

> If the package uses `sContract` and its Head is exactly `§INT`, bind the
> Body to `Value`.

Inside that case, `Value` is known to be an integer.

The second case ignores both Head and Body and acts as the fallback.

### Concrete Heads use exact type equality

A concrete SIG Head pattern requires **exact type equality**.

Subtyping is not enough.

A package with:

```text
Head = §INT
```

does not match:

```spo
WITH [§INT | §BOOL]
```

even though `§INT` is part of that union.

SIG Head matching asks:

```text
Are these the same type?
```

not:

```text
Is one assignable to the other?
```

or:

```text
Is one a subtype of the other?
```

Record fields participate in this equality check. The order of union
alternatives does not matter.

The names of bound type variables do not affect equality. Free type variables,
however, keep their identity.

### Binding an unknown Head

A pattern can bind the Head instead of matching one concrete type.

```spo
WITH §DEF tHead € §TYPE
```

The Body is then checked using exactly that bound Head.

The important rule is:

**A bound SIG Head is fixed but unknown.**

It is not an inference variable.

The compiler may not later decide that `tHead` should become `§INT` merely
because that would make another expression type-check.

Only a successful concrete Head match can establish that the Head really is
`§INT`.

```spo
WITH _
```

simply ignores the Head.

A concrete Head pattern covers only that concrete Head, so another case is
required when other Heads are possible.

The Body may also contain an additional pattern.

## Type constructors as SIG Heads

A SIG Head can itself be a type constructor.

```spo
§DEF tIdentity... = [§GENERIC t t]

§DEF sContract = [
	§SIG_WITH tF... € [§TYPE => §TYPE] IN [.tF §INT]
]

§DEF mPackage =
	§SIG sContract WITH tIdentity... IN 7
```

The contract says:

- `tF...` is a type function,
- the Body must have the type produced by applying `tF...` to `§INT`.

For this package, `tF...` is `tIdentity...`.

Therefore:

```spo
[.tIdentity §INT]
```

evaluates to:

```spo
§INT
```

so the Body must be an integer. The Body `7` is valid.

As long as `tF...` is unknown, the compiler keeps:

```spo
[.tF §INT]
```

as a symbolic type application.

When the concrete Head becomes known, that application can be evaluated.

The same rule applies to nested or curried applications such as:

```text
.(.tF tError) tValue
```

## Why this is useful for modules

A SIG can keep a representation type together with the functions that operate
on that representation.

A module can therefore export:

- a hidden or abstract representation as the SIG Head,
- values and functions that depend on that representation in the SIG Body.

The consumer binds the Head and uses the Body through the contract.

The important property is that the relationship between the Head and the Body
is preserved. Unpacking a SIG and packing it again must not break that
relationship.

## Implementation

SIG processing stays inside the existing compiler phases:

1. The parser creates SIG expressions, contracts, and patterns.
2. Desugaring processes their parts and the pipe syntax.
3. SPO type checking verifies the Head and the dependent Body type.
4. IL generation emits construction, matching, and projection operations.
5. IL checking and the VM verify the corresponding operations again.

The existing structures from `src/Common` remain the basis.

### Type values and their types stay separate

For a type expression such as:

```spo
§INT
```

the compiler needs to keep two different pieces of information:

- the value is the type `§INT`,
- the value itself has the type `§TYPE`.

The same distinction is required for symbolic type values and type functions.

The SPO checker keeps known or symbolic type-level values available for IL
generation. The IL checker evaluates type constructions locally and keeps those
values separate from register types. Aliases and projections preserve known
type-level values.

No additional language type and no separate `§VALUE` instruction are required.

### Free variables and fixed SIG Heads are different

```text
[§FREE]
```

creates a free type variable. It may participate in type inference.

A SIG Head binding is different: it represents a **fixed unknown value** and must
not be changed by inference.

Internally this distinction is represented explicitly. Conceptually:

```text
tKind... := [TYPE => TYPE]
tF... := [§SIG_HEAD tKind]
Body... := [.tF INT]
sContract := [§SIG_WITH tF IN Body]
```

`Body...` refers to exactly that `tF...` binding.

If a concrete constructor is known, the application can be evaluated.
Otherwise it remains symbolic.

The binding is replaced only by the actual SIG Head, never by subtype inference.

When a SIG is unpacked, the compiler creates a fixed reference to the Head stored
inside that SIG.

The `§SIG_HEAD` form is an internal IL representation for this fixed unknown
binding. It is not a new user-facing kind of type.

### Generic type abstractions in IL

IL uses one representation for generic type abstractions:

```text
tElement := [§FREE]
Body... := [tElement => tElement]
tIdentity... := [§ALL tElement => Body]
tApplied := [.tIdentity INT]
```

Here:

```text
tIdentity... : [TYPE => TYPE]
Body...      : TYPE
tApplied     : TYPE
```

There is no second `TypeConstructor` representation and no additional
`[§GENERIC ...]` IL form.

The parser, IL generation, type checker, and VM share the same generic type
representation.

Applying:

```text
[.tIdentity INT]
```

substitutes `INT` for the bound parameter inside the Body.

The same evaluation rule is used when such a type function is called normally.

### Generic signatures in the VM

The VM still has to distinguish:

- type values,
- type functions,
- fixed SIG Head bindings.

A generic signature is transported as the same type abstraction that created
it. It does not receive a second representation merely because it is used as a
signature.

`IsSignature` checks whether a type description can be used as a value
signature or as a declared family of such signatures.

`KindType` independently determines the type of the described type-level
value.

Type-constructor application and exact type equality remain part of the common
type operations.

### The contract remains part of a SIG value

A SIG value keeps its contract in addition to its Head and Body.

This matters because two different contracts can produce the same Body type for
one particular Head while still being different contracts.

Matching must therefore distinguish those SIG values by contract.

The existing mutable AST annotation mechanism remains unchanged.

## Pipes without additional arguments

```spo
7 §> .Right
.Left §< 8
```

Only the piped value is passed.

Conceptually:

```text
.Right 7
.Left 8
```

No additional empty argument is inserted.

An explicitly written `()` is still a real argument:

```spo
9 §> .RightWith ()
.Left () Then §< 10
```

The argument positions of the called name determine where the piped value is
inserted.

## Signature files

A `.SIG` file contains exactly one SPO type definition, with optional surrounding
whitespace. It has no `§DEF`, `§IMPORT`, or `§EXPORT` wrapper. For example,
`Types/TypeConstructor.SIG` contains:

```SPO
[§TYPE => §TYPE]
```

Use `[§LOAD "path/to/File.SIG"]` wherever a type is allowed in SPO, including
inside another `.SIG` file. The square brackets mark a type load and are required:

```SPO
[§SIG_WITH F € [§LOAD "../Types/TypeConstructor.SIG"] IN [<
    Some...: [§GENERIC t [t => [.F t]]]
>]]
```

Paths are resolved relative to the file containing `§LOAD`. Absolute paths
are also accepted. Each file is parsed as one complete type; empty files,
additional definitions, missing files, and loading other file extensions are
errors. Direct and indirect load cycles are rejected with their load chain.
Repeated independent loads of the same file are allowed.

Loading happens during SPO parsing. Generated `.ILT` contains the resulting type
definitions and does not support `[§LOAD "..."]`.

The SIG modules and their consumer tests share the files in `Modules/*.SIG`.
Common type definitions such as `TypeConstructor.SIG` live in `Types/`.
VSCode recognizes `.SIG` and `.sig`, checks their syntax and types, and supports
symbols, references, renaming, and navigation to files named by `§LOAD`.

## Tests

Run all tests with:

```text
dotnet src/mRunTests.cs --no-restore
```

The main existing SIG examples are:

```text
03_18_MatchSigConcrete
03_19_MatchSigHead
03_20_MatchSigUnion
07_19_SigHigherKind
07_20_GenericSignatureAlias
```

The module consumer tests contain additional SIG examples.

`04_10_PipeWithoutArguments` tests both pipe directions and explicitly supplied
empty values.

The test run updates `.ILT` files. These are generated IL examples.
The `.result.SPO` files define the expected behavior.

The full test run currently contains 582 tests.

Additional regression tests cover:

- the separation of type values and type functions,
- fixed SIG Head bindings,
- normal type-constructor calls,
- generic signatures,
- curried type applications,
- preventing SIG binding declarations from escaping,
- exact Head equality.

When a type application still contains an abstract SIG Head, the VM does not yet
know the final representation of the result. SPO and IL still verify the
binding and arguments statically.

For a concrete SIG package, the actual Head is substituted before the VM checks
the dependent Body.
