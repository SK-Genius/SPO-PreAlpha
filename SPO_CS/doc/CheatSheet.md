# SPO Cheat Sheet

> **Hinweis:**  
> Alle Schlüsselwörter haben den Präfix `§` (z.B. `§IF`, `§VAR`, `§DEF`, ...).

---

## Literale & Basis

```spo
42           // Zahl
"abc"        // Text
§TRUE, §FALSE  // Bool
()           // Leeres Literal
_            // Ignoriertes Pattern
```

---

## Variablen & Definitionen

```spo
§DEF foo      // Pattern-Bindung
§VAR x : = 42 .
x : foo y, bar z .
```

---

## Funktionen & Methoden (Mixfix)

```spo
foo x bar y        // Mixfix-Aufruf: abwechselnd Id und Argument
#foo x bar y       // Präfix-Mixfix
foo x bar y => ... // Methoden mit Pattern-Match
```

---

## Blöcke & Sequenzen

```spo
{ 
    ... // Befehle/Definitionen
}
```

---

## Pattern Matching

```spo
(foo, bar)         // Tupel-Pattern
{ a: foo, b: bar } // Record-Pattern
(foo & cond)       // Guard-Pattern
§DEF id            // Freies Pattern
```

---

## Typen

```spo
[§INT]              // Int-Typ
[§BOOL]             // Bool-Typ
[#foo bar]          // Mixfix-Typ (Präfix)
[VAR T]             // Typvariable
[T, U]              // Tupel-Typ
[| T | U |]         // Set-Typ
[T : U => V]        // Lambda-Typ
[§RECURSIVE T ...]  // Rekursiver Typ
```

---

## Kontrollstrukturen

```spo
§IF { cond => expr ... }
§IF expr §MATCH { pat => expr ... }
§RETURN expr
§RETURN expr §IF cond
```

---

## Module

```spo
§IMPORT pat
... // Befehle
§EXPORT expr
```

---

## Methodenaufruf & Mixfix

```spo
.foo x bar y baz    // Id: foo...bar...baz, Children: [x, y, baz]
#foo x bar y       // Präfix-Mixfix
x: foo x bar z => pat // Methodenaufruf mit Pattern
```

---

## Tupel & Records

```spo
(a, b, c)          // Tupel
{ a: 1, b: 2 }     // Record
```

---

**Tipp:**  
- Mixfix-Operatoren: Immer abwechselnd Id und Argument, optional Präfix mit `#`.
- Blöcke und Listen: Immer mit `{ ... }`, `[ ... ]` oder `( ... )`.
- Schlüsselwörter immer mit `§`-Präfix schreiben!
