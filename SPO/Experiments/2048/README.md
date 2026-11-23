# 2048: Tupel und Listen im SPO-Interpreter

Das Experiment vergleicht die unveränderte Spiellogik aus `Apps/2048` mit `Logic.spo` in diesem Verzeichnis.
Die Alternative verwendet vier persistente Listen mit je vier Integer-Werten und die vorhandene Listenbibliothek.
Die spielbare Anwendung, der Compiler und die Runtime werden durch das Experiment nicht umgestellt.

## Wiederholen

Vom Repository-Verzeichnis mit dem .NET-11-SDK:

```powershell
dotnet run --configuration Release --file SPO/Experiments/2048/Compare.cs
```

Nur die Korrektheit prüfen, ohne Messdateien zu überschreiben:

```powershell
dotnet run --configuration Release --file SPO/Experiments/2048/Compare.cs -- --check
```

Der vollständige Lauf schreibt [Results.md](Results.md) mit allen Messwerten und
[results.json](results.json) mit Einzelstichproben, Umgebungsdaten, Quelltextkennzahlen und SHA-256-Hashes.
Eine Messung aus einer anderen Buildkonfiguration wird abgelehnt.
Die .NET-Kompilierung des C#-Treibers liegt vor sämtlichen Messungen.

## Gegenstand und Korrektheit

Beide Varianten exportieren Initialisierung, Zug, Kachelzugriff, Zugmöglichkeit und größte Kachel.
Das Brett unterscheidet sich im SPO-Typ; Punktestand und Generatorzustand haben denselben Vertrag.
Der Treiber erzeugt und zerlegt beide Bretter über dieselbe Darstellung aus 16 Zahlen.
Die Zerlegung prüft genau vier Zeilen mit je vier Zellen und liegt außerhalb der Zeitmessungen.

Vierertupel und Listen mit vier Elementen sind in der VM beide verschachtelte Paare mit leerem Anfang.
Das Experiment vergleicht somit spezialisierte Tupeloperationen mit allgemeinen Listenoperationen auf derselben
grundlegenden Wertdarstellung. Die Tupelsignatur garantiert die Größe statisch; bei der Listenvariante ist 4 × 4
eine durch Initialisierung, Operationen und Tests erhaltene Invariante.

Vor den Messungen werden 87 Initialisierungs- und Zugfälle für beide Varianten geprüft:

- Sieben Seeds einschließlich 0, negativer Werte und Integer-Grenzen; die Referenz rechnet den Generator mit 64 Bit.
- Neun fest vorgegebene Zeilenfälle in allen vier Richtungen: Lücken, mehrere Paare, Merge-Reihenfolge und große Kacheln.
- Leeres, gepacktes und blockiertes Brett, ausschließlich horizontale bzw. vertikale Merges, letzte freie Zelle
  und ein Brett mit mehreren gleichzeitig verändernden Zeilen.
- Eine Folge aus 16 Zügen mit fortgesetztem Zufallszustand.

Hinzu kommen größte Kachel, Zugmöglichkeit, alle 16 Zellzugriffe und vier Zugriffe außerhalb des Bretts auf sieben Brettern.
Die Zugreferenz in C# verwendet unabhängig von den SPO-Implementierungen Arrays und Schleifen.
Bei den festen Zeilenfällen wird auch diese Referenz gegen die vorgegebenen Ergebnisse geprüft.
Verglichen werden alle Zellen, Punkte und der genaue Zufallszustand; der Eingabezustand muss erhalten bleiben.

## Messmethode

- Ein Prozess, Release-Build, derselbe Interpreter und derselbe Compiler-Stack-Thread für beide Varianten.
- Korrektheitslauf und Kalibrierung wärmen die Ausführung auf.
- Je Operation dieselben vorbereiteten Argumente für beide Varianten.
  Die Kalibrierung verdoppelt die Batchgröße für jede Variante einzeln, bis sie mindestens 100 ms benötigt.
  Unterschiedliche Wiederholungszahlen werden durch die Angabe pro Aufruf berücksichtigt.
- Sieben Stichproben pro Variante; die Reihenfolge Tupel/Liste wechselt zwischen den Stichproben.
- Berichtet werden Median und Min–Max der Mikrosekunden pro Aufruf sowie der Median zugewiesener Bytes pro Aufruf.
  Die Bytezahl stammt aus `GC.GetAllocatedBytesForCurrentThread` und beschreibt keine verbleibende Heapgröße.
- Feldsuche im Modul, Argumentaufbau, Konvertierung, Referenzberechnung, Ergebnisvergleich, Bericht und Konsolenausgabe
  liegen außerhalb der gemessenen Operationsbatches. VM-Aufruf und dessen Wrapper sind bei beiden Varianten enthalten.
- Der Aufruf jeder Operation verwendet wieder denselben unveränderten Eingabewert. Dadurch misst ein Zugfall
  immer dieselbe Arbeit und entwickelt sich nicht während der Stichprobe in ein blockiertes Brett.
- `Startup/check` misst Konfigurationsladen sowie SPO-Prüfung/Übersetzung einschließlich benötigter Abhängigkeiten.
  `Startup/init` misst danach das vorhandene `Entry.Init`, einschließlich Linking, Opcode-Erzeugung und Modulinitialisierung.
  Sieben frische Modulgraphen werden in einem bereits aufgewärmten Prozess geladen; es sind keine Kaltstartmessungen.

Die Messung vergleicht vollständige Implementierungen. Zusätzlich zur allgemeinen Listenverarbeitung unterscheiden sich
Traversierungsrichtung und Anzahl der Zwischenergebnisse. Insbesondere liest `List.Any` vom Listenende; die Tupelprüfung
auf weitere Züge beginnt links oben. Unterschiedliche Abbruchpositionen gehören deshalb zur Erklärung der Einzelfälle.
Überlappende Min–Max-Bereiche werden konservativ als unentschieden behandelt.

## Ergebnis des Laufs vom 29. Oktober 2026

Gemessen mit SDK `11.0.100-rc.3.26522.148`, Runtime `.NET 11.0.0-rc.3.26522.148`,
Windows `10.0.26300`, x64, 12 logischen Prozessoren und Release-Konfiguration.
Der gespeicherte Lauf entstand vor der zusätzlichen Argument-/Empfängerprüfung im VM-Aufruf `mVM.Call`
und vor den Record-Konvertierungen an Typgrenzen.
Ein neuer Lauf enthält deren Aufwand; die unten gespeicherten Werte wurden dafür nicht neu gemessen.
87 Initialisierungs-/Zugfälle sowie die zusätzlichen Abfragen und Zustandserhaltungsprüfungen bestanden.
Alle gemessenen Zeitintervalle der beiden Varianten waren in diesem Lauf getrennt;
die folgende Zuordnung ist daher nach dem konservativen Min–Max-Kriterium eindeutig.

Auswahl der Mediane; Zeit pro Aufruf in Millisekunden, Zuweisungen in dezimalen MB:

| Fall | Tupel ms | Liste ms | Zeit Liste/Tupel | Tupel MB | Liste MB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Neues Spiel, Seed 1 | 1,710 | 9,846 | 5,76× | 8,47 | 45,95 |
| Erste Zelle lesen | 0,0146 | 0,1800 | 12,33× | 0,081 | 0,897 |
| Größte Kachel, dicht | 0,466 | 1,920 | 4,12× | 2,54 | 9,05 |
| Weitere Züge, blockiert | 1,292 | 7,761 | 6,01× | 6,32 | 37,22 |
| Weitere Züge, nur vertikal | 0,0818 | 7,6894 | 94,02× | 0,431 | 37,15 |
| Weitere Züge, letzte freie Zelle | 1,117 | 0,889 | 0,80× | 6,23 | 4,41 |
| Gemischtes Brett nach links | 1,425 | 6,725 | 4,72× | 7,74 | 33,48 |
| Gemischtes Brett nach unten | 0,967 | 12,717 | 13,15× | 5,20 | 63,08 |
| Gepacktes Brett, unveränderter Zug | 1,011 | 3,008 | 2,98× | 5,42 | 14,83 |

Alle sieben gemessenen Zugfälle liegen bei der Liste zwischen 2,98× und 13,15× der Tupelzeit.
Ihre Zuweisungen liegen zwischen 2,74× und 12,12× der Tupelwerte.
Die Zuweisungen umfassen VM-Ausführung, Aufruf-Wrapper und Zwischenergebnisse;
sie sind keine Messung des Speicherbedarfs des persistenten Bretts allein.

Für die Startphasen ergaben sich folgende Mediane:

| Phase | Tupel ms | Liste ms | Tupel zugewiesene MB | Liste zugewiesene MB |
| --- | ---: | ---: | ---: | ---: |
| Konfiguration und Prüfung/Übersetzung | 286,226 | 1066,046 | 670,15 | 4148,98 |
| `Entry.Init` nach der Prüfung | 112,654 | 2133,378 | 670,17 | 10946,43 |

Die Listenvariante lädt zusätzlich das vollständige vorhandene Listenmodul mit seinen generischen Verträgen und
Funktionen. Die gemessene Initialisierungsphase schließt dessen Linking und Opcode-Erzeugung ein.
Der Lauf isoliert diese Anteile nicht weiter; er weist daher keine einzelne Compiler- oder VM-Funktion als Ursache aus.

## Codeumfang und Lesbarkeit

Die Kennzahlen beziehen sich ausschließlich auf die beiden handgeschriebenen `Logic.spo`-Dateien.
Gemeinsame Bibliotheken, Signaturen, Treiber und Tests sind hier nicht mitgezählt.
Generiertes IL wurde nicht als Lesbarkeitsmaß verwendet.

| Kennzahl | Tupel | Liste |
| --- | ---: | ---: |
| Gesamte Zeilen | 276 | 208 |
| Nichtleere Zeilen | 268 | 199 |
| Nichtleere Zeilen nach dem Importblock | 251 | 167 |
| Explizite Typangaben, mechanisch als `€` gezählt | 33 | 42 |

Die Liste spart insgesamt rund 25 % der Zeilen und rund 33 % des Codes nach dem Importblock.
Sie benötigt mehr Imports und in dieser Fassung mehr explizite Typangaben.
Der `€`-Zähler beschreibt den geschriebenen Code und ist kein Beweis einer minimal möglichen Annotationszahl.
Im ersten ausführbaren Versuch ließ sich der benannte `Reduce`-Callback ohne Argumenttypen nicht prüfen
(`missing type for '_aLargest'`); seine beiden Integer-Argumente sind deshalb ausdrücklich typisiert.
Weitere wichtige Typgrenzen sind die Listenargumente und der gemeinsame Zeilen-/Score-Typ der Merge-Callbacks.

Die Transposition der Liste liest sich als „für jede Spalte jede Zeile abbilden“.
`Flatten` und `Reduce` formulieren die größte Kachel unmittelbar als Aggregation.
Die Suche freier Zellen wird als `Flatten`, `WithIndex` und `Where` beschrieben.
Das vermeidet mehrere handgeschriebene Indexrekursionen und die explizite Rekonstruktion von Vierertupeln.
Die Merge-Funktion filtert Nullen, verarbeitet die verbleibenden Werte paarweise und füllt mit Nullen auf.
Sie vermeidet die positionsabhängigen Vierer-Fallunterscheidungen, führt dafür eine eigene lokale Rekursion ein.

Diese Verkürzung spart in der aktuellen Bibliothek keine Traversierungen:

- Ein Kachelzugriff über `TryGet` kehrt zuerst die Zeilenliste und danach die ausgewählte Zeile um.
  Die Tupelvariante zerlegt die vorhandenen Paare direkt.
- `Map`, `MapWithIndex`, `Reduce` und `Take` verwenden in der vorhandenen Listenbibliothek vorbereitende Umkehrungen.
  Verkettete Operationen erzeugen zusätzliche Listen, Paare und Callback-Aufrufe.
- Der Zufallsschritt der Liste materialisiert die flache Zellliste, indizierte Zellen und die gefilterten freien Zellen,
  bevor er die gewählte Position ersetzt. Die Tupelvariante zählt freie Zellen und sucht anschließend die gewählte Zelle.
- Die Prüfung benachbarter Werte erzeugt über `Skip` und `ZipShort` Paarlisten; für vertikale Nachbarn wird transponiert.
  Die Tupelvariante kann beim hier verwendeten vertikalen Brett bereits an der ersten Position erfolgreich abbrechen.
- Bei der letzten freien Zelle kehrt sich der Vorteil um: `List.Any` findet sie vom Listenende aus sofort nach dem
  vorbereitenden `Flatten`; die Tupelvariante erreicht sie erst nach den übrigen Positionen.

Die Unterschiede bei `CanMove` sind deshalb besonders eingabeabhängig.
Die 94× aus dem vertikalen Beispiel sind keine allgemeine Geschwindigkeitsaussage über Listen.

## Empfehlung

Für die bestehende feste 4×4-Anwendung bleibt die Tupelvariante die bessere Wahl:
Sie garantiert die Größe im Typ und ist in allen gemessenen Zugfällen schneller und allokationsärmer.
Die kürzere Listenfassung rechtfertigt hier keine Umstellung der spielbaren App.

Die Listenfassung zeigt trotzdem einen konkreten Nutzen der vorhandenen Sequenzoperationen für die Lesbarkeit.
Als nächster separater Untersuchungsgegenstand bieten sich vermeidbare Umkehrungen, Zwischenlisten und die
Kosten der generischen Aufrufe an. Eine Bibliotheks- oder Runtime-Optimierung wurde in diesem Experiment nicht umgesetzt.
Die Ergebnisse gelten für diese beiden Implementierungen, den aktuellen Interpreter und den dokumentierten Lauf;
sie vergleichen keine grundsätzlich unterschiedlichen Speicherrepräsentationen von Tupeln und Listen.
