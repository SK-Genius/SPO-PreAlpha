# SIG und Pipes

## Sprachregeln

Ein SIG-Wert enthält einen Head und einen Body. Der Contract beschreibt den Kind
des Heads und den davon abhängigen Typ des Bodys:

```spo
§DEF Contract = [§SIG_WITH t € §TYPE IN t]
§DEF Package = §SIG Contract WITH §INT IN 7
```

Hier ist der Head `§INT`; der Body muss deshalb ein Integer sein. Der statische
Typ des Pakets ist der Contract. Beim Verpacken wird der Body-Typ aus dem
Contract durch Einsetzen des angegebenen Heads bestimmt und geprüft.

### Typwerte, Typfunktionen und Bindungen

`§INT` und `[§TYPE => §TYPE]` sind Werte vom Typ `§TYPE`.
Eine Typabstraktion `F` mit Typ `[§TYPE => §TYPE]` ist eine Funktion.
Ihre Anwendung `[.F §INT]` liefert einen Typ. Ein unbekannter SIG-Head mit
Funktionstyp darf nicht unmittelbar als Body-Typ, Record-Feldtyp oder Argument-
bzw. Ergebnistyp eingesetzt werden.
Bei einem currierten Konstruktor kann eine Teilanwendung zunächst wieder eine
Funktion liefern; erst das vollständig angewendete Ergebnis ist als Typ zulässig.

```spo
[§SIG_WITH F € [§TYPE => §TYPE] IN [.F §INT]]  // gültig
[§SIG_WITH F € [§TYPE => §TYPE] IN F]          // ungültig
```

`[§GENERIC t Body]` in SPO und `[§ALL t => Body]` im IL bezeichnen dieselbe
Typabstraktion. Sie hat unabhängig von ihrer Verwendung einen Funktionstyp.
Für einen Body vom Typ `§TYPE` ist das `[§TYPE => §TYPE]`; ist der Body selbst
eine Typabstraktion, entsteht ein currierter Funktionstyp.

Eine deklarierte Typabstraktion kann als generische Signatur verwendet werden:

```spo
§DEF Signature... = [§GENERIC t [t => t]]
§DEF Identity... € Signature... = (§DEF t € §TYPE) <=> (§DEF Value € t) => Value
```

`Signature...` hat auch hier den Typ `[§TYPE => §TYPE]`. Die Annotation beschreibt
eine Familie von Funktionssignaturen; sie ändert den Wert oder Typ der Abstraktion
nicht. `[.Signature §INT]` ergibt `[§INT => §INT]`. Der Aufruf `.Identity 7`
bestimmt dagegen den Parameter der Signatur aus seinem Argument und liefert `7`.
Beim Vergleich generischer Funktionssignaturen bleibt die bestehende Inferenz
aus den Argumenttypen erhalten; die Reihenfolge der generischen Parameter kann
sich dabei unterscheiden. Das ist unabhängig von der exakten Gleichheit zweier
Typabstraktionen als SIG-Heads. Eine monomorphe Funktion wird gegen einen festen,
unbekannten Parameter geprüft: Eine nur für Integer gültige Funktion erfüllt
nicht die generische Signatur `[§GENERIC t [t => t]]`.

Typabstraktionen dürfen weiterhin nicht als Werte vom Typ `§TYPE` verwendet
oder einer freien Typvariablen zugewiesen werden. Die Verwendung als deklarierte
generische Signatur wird von dieser Kind-Prüfung getrennt behandelt; eine
unbekannte Typfunktion besitzt keine solche auswertbare Signaturdeklaration.

### Auspacken und Matching

```spo
§EXPORT §IF Package MATCH {
	§SIG Contract WITH §INT IN §DEF Value : Value
	§SIG Contract WITH _ IN _ : 0
}
```

Ein konkretes Head-Pattern verlangt **exakte Typgleichheit**. Weder eine Subtyp-
noch eine Supertypbeziehung genügt. Der Head kann im Body sowohl Argument- als
auch Ergebnistyp einer Funktion sein. Zum Beispiel darf ein Paket mit Head
`§INT` nicht auf `WITH [§INT | §BOOL]` passen.

Die Namen gebundener Typvariablen beeinflussen die Gleichheit nicht. Freie
Variablen behalten dagegen ihre Identität. Record-Felder werden verglichen; die Reihenfolge von Union-Alternativen
ist für die Gleichheit unerheblich.

`WITH §DEF t € §TYPE` bindet den Head; der Body wird mit genau dieser Bindung
typisiert. `WITH _` ignoriert den Head. Konkrete Head-Patterns decken nicht den
gesamten Contract ab; dafür ist ein weiterer Fall erforderlich. Auch der Body
kann durch ein Pattern weiter eingeschränkt werden.

Ein abstrakt gebundener Head bleibt ein fester, unbekannter Typwert.
Die Inferenz darf ihn nicht nachträglich auf einen passenden konkreten Typ
setzen. Ein Body vom abhängigen Typ `Head` kann daher erst nach einem passenden
konkreten Head-Match wie ein Integer verwendet werden.

### Typkonstruktoren und Module

Ein Head kann ein Typkonstruktor sein:

```spo
§DEF Identity... = [§GENERIC t t]
§DEF Contract = [
	§SIG_WITH F € [§TYPE => §TYPE] IN [.F §INT]
]
§DEF Package = §SIG Contract WITH Identity... IN 7
```

Solange `F` abstrakt ist, bleibt `.F t` als Typanwendung erhalten. Beim Einsetzen
eines konkreten Konstruktors wird die Anwendung ausgewertet. Das gilt auch für
verschachtelte Anwendungen wie `.(.F tError) tValue`.

Module können so ihre Repräsentation und die dazugehörigen Funktionen gemeinsam
exportieren. Consumer binden den Head und verwenden die Funktionen über den
Contract. Auspacken und erneutes Verpacken erhält den Zusammenhang zwischen
Head und Body.

## Umsetzung

Die Verarbeitung bleibt in den vorhandenen Phasen:

1. Der Parser erzeugt SIG-Ausdrücke, Contracts und Patterns.
2. Das Desugaring verarbeitet deren Bestandteile sowie die Pipe-Syntax.
3. Die SPO-Typprüfung prüft Head-Kind und Body-Typ und führt lokale Bindungen.
4. Die IL-Erzeugung bildet Konstruktion, Prüfung und Projektionen ab.
5. IL-Prüfung und VM prüfen die entsprechenden Operationen erneut.

Die vorhandenen Strukturen aus `src/Common` bleiben die Grundlage.
`§INT` ist ein Wert vom Typ `§TYPE`. Die Auswertung eines Typausdrucks
liefert den Typwert, während seine Typannotation dessen Typ beschreibt.
Beide Informationen bleiben getrennt.

Im SPO-Scope enthält `TypeValue` den bekannten oder symbolischen Wert einer
Typbindung. Aufgelöste Identifier behalten diesen Wert für die IL-Erzeugung.
Die IL-Prüfung wertet Typkonstruktionen lokal aus und führt diese Werte
getrennt von den Registertypen. Aliase und Paarprojektionen reichen die
vorhandenen Werte weiter. Dafür gibt es keinen zusätzlichen Sprachtyp und
keinen `§VALUE`-Befehl.

`[§FREE]` erzeugt ausschließlich eine freie Typvariable vom Typ `§TYPE`.
Die IL-Syntax erlaubt dort keine zusätzliche Typangabe.
Ein SIG-Parameter besitzt eine eigene Deklaration; sie ist keine Inferenzvariable
und darf nicht als eigenständiger Wert aus dem Contract entkommen:

```text
Kind := [TYPE => TYPE]
F := [§SIG_HEAD Kind]
Body := [.F INT]
Contract := [§SIG_WITH F IN Body]
```

Die Anwendung in `Body` verweist auf genau diese Bindung von `F`. Bei bekanntem
Konstruktor wird sie ausgewertet, sonst bleibt sie als Anwendung erhalten.
Die Bindung wird nur durch den zugehörigen Head ersetzt, nicht durch Subtypinferenz.
Beim Auspacken entsteht ein fester Verweis auf den enthaltenen Head.

Im IL gibt es für Typabstraktionen ausschließlich `[§ALL t => Body]`:

```text
t := [§FREE]
Body := [t => t]
T := [§ALL t => Body]
Applied := [.T INT]
```

`T` hat den Typ `[TYPE => TYPE]`, `Body` und `Applied` haben den Typ `TYPE`.
Die zusätzliche IL-Syntax `[§GENERIC t => Body]` und `TypeConstructor` entfallen.
Parser, IL-Erzeugung, Typprüfung und VM verwenden gemeinsam `TypeGeneric` bzw.
`Generic`. Die Anwendung ersetzt den gebundenen Parameter im Body; dieselbe
Auswertung gilt beim normalen Funktionsaufruf `.T INT_TYPE`.

Die VM unterscheidet weiterhin Typwerte, Typfunktionen und die
Bindungsdeklarationen beim Aufbau von Contracts. Eine generische Signatur wird
als dieselbe Typabstraktion transportiert, also mit Funktionstyp. `IsSignature`
prüft, ob eine Typbeschreibung ein Typwert oder eine deklarierte Familie solcher
Signaturen ist. Diese Prüfung wird von SPO, IL und VM verwendet. `KindType`
bestimmt davon unabhängig den Typ des beschriebenen Werts. Es gibt keine zweite
Darstellung für dieselbe Abstraktion und keine Kontextabhängigkeit bei ihrer
Konstruktion.

Typkonstruktor-Anwendung und Gleichheit liegen bei den gemeinsamen
Typoperationen. SIG-Werte behalten ihren Contract, damit das Matching auch
Contracts unterscheiden kann, deren Bodies für einen einzelnen Head gleich
aussehen. Der bestehende mutable AST-Annotationsmechanismus wird nicht ersetzt.

## Pipes ohne weitere Argumente

```spo
7 §> .Right
.Left §< 8
```

Hier wird nur der weitergereichte Wert übergeben. Ein ausdrücklich angegebenes
`()` bleibt dagegen ein Argument:

```spo
9 §> .RightWith ()
.Left () Then §< 10
```

Die Unterscheidung folgt aus den Argumentpositionen des aufgerufenen Namens.

## Prüfung

```text
dotnet src/mRunTests.cs --no-restore
```

Die bestehenden Tests `03_18_MatchSigConcrete`, `03_19_MatchSigHead`,
`03_20_MatchSigUnion`, `07_19_SigHigherKind` und die Modul-Consumer beschreiben
die SIG-Anwendungen. `04_10_PipeWithoutArguments` prüft beide Pipe-Richtungen
sowie ausdrücklich übergebene leere Werte.

Der vorhandene Testlauf aktualisiert `.ILT`-Dateien. Diese Dateien sind
generierte Beispiele; die `.result.SPO`-Dateien geben das erwartete Verhalten an.

Der vollständige Lauf umfasst aktuell 578 Tests. Zusätzliche Regressionstests
prüfen die Trennung von Typwerten und Typfunktionen, unveränderliche
SIG-Bindungen, normale Konstruktoraufrufe, das Verbot entkommender
Bindungsdeklarationen und exakte Head-Gleichheit. `07_20_GenericSignatureAlias`
prüft die Verwendung einer benannten Typabstraktion als generische Signatur.
Die direkten SPO- und IL-Tests prüfen außerdem den Funktionstyp von `§ALL`,
currierte Anwendungen und die gemeinsame Darstellung in beiden Verwendungen.

Bei einer noch abstrakten Typanwendung kennt die VM die Repräsentation des
Ergebnisses nicht. Ihre Bindung und Argumente werden statisch im SPO und IL
geprüft. Für das Matching eines konkreten SIG-Pakets wird dessen tatsächlicher
Head eingesetzt, bevor die VM den Body prüft.
