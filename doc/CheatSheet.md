# SPO Cheat Sheet

> **Note:**
> All keywords are prefixed with `§` (e.g., `§IF`, `§VAR`, `§DEF`). Some tools may display the prefix differently.

---

## Legend

- `§` keyword prefix (may display differently in some tools)
- `€` type annotation
- `.` call operator and call-chain terminator
- `;` pair separator
- `,` tuple separator
- `#` tag constructor / tagged value
- `( )` parentheses for grouping, tuples, and empty literal `()`
- `[ ]` brackets for type forms
- `{ }` braces for blocks and records

---

## Types

```spo
§INT                              // Int type
§BOOL                             // Bool type
[#Tag T]                          // Prefix/tag type
[§VAR T]                          // Mutable slot / variable reference type (read with §TO_VAL)
[T; U] or [T, U, V]               // Pair/tuple type
[T | U | V]                       // Set/union type
[ArgType => ResultType]           // Function type ("->" does not exist)
[Obj : Args => Res]               // Method type with object
[§RECURSIVE T Body]               // Recursive type
[§GENERIC Param Body]             // Generic type (forall)
[§INTERFACE Param Body]           // Interface/existential type (not implemented yet)
```

---

## Literals & Basics

```spo
42                    // Number
"abc"                 // Text
#Tag 1                // Tagged value (constructor-like)
#Op 1 + 2             // Tagged prefix with an operator name
1 #+ 2                // Tagged value in mixfix position
§TRUE, §FALSE         // Bool
()                    // Empty literal
_                     // Ignored pattern
```

---

## Pairs, Tuples & Records

```spo
(a; b)             // Pair
(a, b, c)          // Tuple
{ a: 1, b: 2 }     // Record
```

---

## Variables & Definitions

```spo
§DEF foo = 1                          // Simple definition
§DEF bar € §INT = 2                   // Typed definition
§VAR x € §INT                         // Variable declaration
§VAR x := 42 .                        // Assignment (call chain)
§VAR x:
    = 42
.
x := x .+ 1 .                         // Reassignment (call chain)
§VAR y := 1, + 4, / 2 .               // Call-chain (inline)
§VAR y:
    = 1
    + 4
    / 2
.
```

---

## Blocks & Sequences

```spo
{
    ... // Commands/definitions
}
```

---

## Functions & Mixfix (Definition & Call)

```spo
§DEF ...<=... = (                     // "..." mark argument slots; "." stays the call op at call sites
    §DEF l € §INT
    §DEF r € §INT
) => l .<= r

§DEF ...<=...<=... = (                // Argument slots are the "..."
    §DEF min € §INT
    §DEF value € §INT
    §DEF max € §INT
) => (min .<= value) .And (value .<= max)

.a1 b2 a3 b4 a5                       // same as: .(a1...a3...a5)(b2, b4)
a1 .b2 a3 b4 a5                       // same as: .(...b2...b4...)(a1, a3, a5)
3 .<= 5 <= 7                          // same as: .(...<=...<=...)(3, 5, 7)
```

---

## Recursive Definitions

```spo
§RECURSIVE {
    §DEF ...CountDown = (
        §DEF n € §INT
    ) => §IF n §MATCH {
        0 : 0
        §DEF k : .CountDown (k .- 1)
    }
}
```

---

## Control Structures

```spo
§IF { cond : expr ... }                // If/else cascade
§IF value §MATCH { pat : expr ... }    // Pattern match
§IF value §MATCH {                     // Keywords keep the § prefix
    (§DEF n & n .< 2) : n
    _ : 2
}
§RETURN expr                           // Immediate return
§RETURN expr §IF cond                  // Conditional return
```

---

## Pattern Matching

```spo
(foo, bar)          // Tuple pattern
{ a: foo, b: bar }  // Record pattern
(foo & cond)        // Guard pattern
§DEF id             // Free pattern/binding
_                   // Wildcard
```

---

## Modules

```spo
§IMPORT {
    Std: { ...==...: §DEF ...==... € [[§INT, §INT] => §BOOL] }
    Char: { ...Ord: §DEF ...Ord € [§CHAR => §INT] }
}

... // Commands / definitions

§EXPORT {
    ...<=>...: ...Text<=>...
    ...<=...: ...Text<=...
}
// or: §EXPORT expression
```

---

**Tips:**
- The dot `.` is the call operator **and** the marker for the first name part of every mixfix function call (`.Fib 6`, `3 .<= 5 <= 7`).
- `#` constructs tagged values (`#Tag 1` builds a value with tag `Tag`).
- Mixfix operators interleave name segments with arguments.
- A trailing dot `.` terminates call chains (including `:=` assignments); lists/blocks use `{ ... }`, `[ ... ]`, or `( ... )`.
- Always prefix keywords with `§`; use `€` for type annotations.
