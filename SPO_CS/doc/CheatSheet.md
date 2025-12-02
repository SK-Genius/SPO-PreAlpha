# SPO Cheat Sheet

> **Note:**
> All keywords are prefixed with `§` (e.g., `§IF`, `§VAR`, `§DEF`, ...).

---

## Literals & Basics

```spo
42           // Number
"abc"        // Text
§TRUE, §FALSE  // Bool
()           // Empty literal
_            // Ignored pattern
```

---

## Variables & Definitions

> Bindings in SPO are immutable by default. Use `§VAR` to explicitly mark a binding as mutable.

```spo
§DEF foo      // Pattern binding
§VAR x : = 42 .
x : foo y, bar z .
```

---

## Functions & Methods (Mixfix)

```spo
foo x bar y        // Mixfix call: alternating id and argument
#foo x bar y       // Prefix mixfix
foo x bar y => ... // Methods with pattern match
```

---

## Blocks & Sequences

```spo
{ 
    ... // Commands/definitions
}
```

---

## Pattern Matching

```spo
(foo, bar)         // Tuple pattern
{ a: foo, b: bar } // Record pattern
(foo & cond)       // Guard pattern
§DEF id            // Free pattern
```

---

## Types

```spo
[§INT]              // Int type
[§BOOL]             // Bool type
[#foo bar]          // Mixfix type (prefix)
[VAR T]             // Type variable
[T, U]              // Tuple type
[| T | U |]         // Set type
[T : U => V]        // Lambda type
[§RECURSIVE T ...]  // Recursive type
```

---

## Control Structures

```spo
§IF { cond : expr ... }
§IF expr §MATCH { pat : expr ... }
§RETURN expr
§RETURN expr §IF cond
```

---

## Modules

```spo
§IMPORT pat
... // Commands
§EXPORT expr
```

---

## Method Call & Mixfix

```spo
.foo x bar y baz    // Id: foo...bar...baz, Children: [x, y, baz]
#foo x bar y       // Prefix mixfix
x: foo x bar z => pat // Method call with pattern
```

---

## Tuples & Records

```spo
(a, b, c)          // Tuple
{ a: 1, b: 2 }     // Record
```

---

**Tip:**
- Mixfix operators: Always alternate id and argument; optional prefix with `#`.
- Blocks and lists: Always use `{ ... }`, `[ ... ]`, or `( ... )`.
- Always write keywords with the `§` prefix!
