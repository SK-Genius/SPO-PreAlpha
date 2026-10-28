# Snake als SPO-Beispiel

Vom Repository-Verzeichnis starten:

```powershell
dotnet run --file SPO/src/Program.cs
```

Pfeiltasten oder WASD bewegen die Schlange.
Leertaste oder ein linker Mausklick pausieren.
R startet neu;
Esc, Q oder Strg+C beenden.
Fokusverlust pausiert das Spiel.
Bei einem zu kleinen Fenster erscheint die benötigte Größe und die Bewegung wartet auf ausreichend Platz.

## Den Code lesen

| Datei | Aufgabe |
| --- | --- |
| Snake.spo | Event, Zustand und Darstellung verbinden; App-Vertrag exportieren. |
| Input.spo | Terminal-Eingaben in Spielaktionen übersetzen. |
| Logic.spo | Aus einem Zustand und einer Aktion den nächsten Zustand berechnen. |
| Render.spo | Den Zustand im Terminal zeichnen. |
| State.SIG | Gemeinsamer, struktureller Spielzustand für Logic, Render und Snake. |
| SnakeBody.SIG | Konkrete Liste von Positionen vom Schwanz zum Kopf. |
| Render.SIG | Zeichenmethode auf Terminal.SIG und Größenprüfung. |

Der Einstieg ist Frame... in Snake.spo:

1. Ein Resize leert die Darstellung und zeichnet den bestehenden Zustand neu.
2. ActionForEvent... übersetzt andere Events in Aktionen.
3. Bei zu kleinem Fenster wartet ein Bewegungsschritt.
4. `State .After Action` liefert den nächsten unveränderlichen Zustand.
5. `aTerminal : DrawGame NextState .` zeichnet ihn.
6. Der Host erhält `(NextState, ContinueRunning)` und präsentiert das fertige Bild.

Input und Logic brauchen kein Terminal.
Methoden auf dem Terminal-Interface verändern externe Ressourcen über dessen gebundenen veränderlichen Empfänger.
Der Host öffnet und schließt die Konsole;
SPO erhält eine bereits bestehende Capability.

Die SIG-Dateien beschreiben die öffentlichen Verträge.
In den Implementierungen stehen die benötigten Parametertypen einmal;
Rückgaben, lokale Werte und Match-Bindungen werden aus ihren Ausdrücken abgeleitet.
Die gemeinsame Ergebnisgrenze `[tGameState, §BOOL]` konkretisiert vor der Tupelzerlegung die unterschiedlichen Rückgabetypen der Event-Match-Zweige.

Feste Datenwerte tragen `c`, auch beim Import und bei Berechnungen ausschließlich aus Konstanten.
Argumente tragen `a`;
davon direkt oder indirekt abhängige lokale Werte haben kein `c`.
Daher heißen die Größenhinweise im Renderer `cWidthHint` und `cHeightHint`, während `ScoreText` vom übergebenen Spielzustand abhängt.

## Lesbare Aufrufe

```spo
§DEF NextState = State .After Action
§DEF NextBody = BodyAfterTailMove .FollowedBy NextHead
§DEF cFoodStyle = cDefaultStyle .WithForeground #Red §> .WithBlink §TRUE
aTerminal : Draw "*" At FoodPosition WithStyle cFoodStyle .
```

FollowedBy hängt eine Zelle an eine neue Liste an.
WithForeground und WithBlink liefern neue Style-Records.
Draw nennt Text, Position und Style in einem Aufruf.
Positionen beginnen bei `(0, 0)` im sichtbaren Fenster;
die Runtime übernimmt Buffer-Offsets und Clipping.
Zeichnen verschiebt den Cursor nicht.

Input liest benannte Tasten und strukturelle Record-Patterns.
Es erwähnt nur die benötigten Felder, etwa `Modifiers: {Control: §DEF Control}`.
Windows-Codes und Bitmasken kommen im Spiel nicht vor.
PressedButtons unterscheidet den ersten Mausdruck vom Halten.
IsRepeat verhindert mehrfaches Pausieren bei gehaltener Leertaste.
Ein Match unterscheidet Events, ein innerer Match die Tastennamen.

## Spielzustand und Bewegung

| Feld | Bedeutung |
| --- | --- |
| SnakeBody | Persistente Liste vom Schwanz zum Kopf. |
| MoveDirection | Richtung des zuletzt ausgeführten Schritts. |
| PendingDirection | Gewünschte Richtung für den nächsten Tick. |
| FoodPosition | Position des Futters. |
| RandomState | Zustand des Zufallsgenerators für den nächsten Apfel. |
| Phase | Playing, Paused, GameOver oder Won. |

Die beiden Richtungen verhindern Kehrtwenden auch bei mehreren Eingaben zwischen zwei Ticks.
Die Gegenrichtungen werden direkt als benannte Paare geprüft.
Der Score wird aus Körperlänge minus Anfangslänge berechnet.

AdvancedState... ist eine lokale Berechnung innerhalb von `...After...`.
Sie berechnet den nächsten Kopf und entfernt den Schwanz, sofern kein Futter gefressen wird.
Erst dann erfolgt die Kollisionsprüfung.
So darf der Kopf die im selben Schritt frei werdende Schwanzzelle betreten.

Vektoroperatoren werden beim Import markiert: `...+...: §DEF ..._+_...` und `...==...: §DEF ..._==_...`.
Die Bewegung liest sich als `HeadPosition ._+_ Step`, der Futtervergleich als `NextHead ._==_ FoodPosition`.

Die Wandprüfung liest sich als Bereich: `1 .<= (NextHead .X) <= cBoardWidth`.
Das Spielfeld umfasst 28 × 16 Zellen;
die Wand umgibt diesen Bereich.
Ist es voll, wechselt die Phase zu Won.
Nach dem Fressen liefert `RandomState .NextRandom` den nächsten Generatorzustand.
Daraus entsteht ein Index unter den freien Zellen.
`FindFreeCellOutside...Skipping...` zieht zeilenweise die Anzahl freier Zellen ab und sucht anschließend nur in der gewählten Zeile die freie Spalte.
Auch bei nur einer freien Zelle wird der Apfel direkt gefunden;
zufälliges Probieren belegter Positionen entfällt.

Der Generatorzustand ändert sich nur, wenn ein neuer Apfel erscheint.
Bewegung, Richtungswechsel, Pause und Kollision behalten ihn bei.
Der erste Apfel steht weiterhin bei `(20, 8)`.
Anfangszustand und Neustart verwenden den Seed `1`, damit dieselbe Eingabefolge dieselben Apfelpositionen erzeugt.
Andere Seeds können im Spielzustand stehen;
versteckten globalen Zufallszustand gibt es nicht.

## Darstellung und Bibliotheken

Der Renderer leitet Rahmenbreite, Hilfszeile und benötigte Fenstergröße aus Spielfeld und Hilfstext ab.
Text.Repeated erzeugt Rahmen/Leerzeilen als reine Werte.
Text.AsDecimalText formatiert den Score;
dafür braucht Render keine Ziffernliste.
Die beiden rekursiven Zeichenmethoden dienen der Traversierung von Spielfeldzeilen und Körperzellen.

Jedes Update zeichnet zunächst in den Runtime-Buffer.
Der Host präsentiert das fertige Bild gemeinsam;
Zwischenschritte beim Leeren und Zeichnen bleiben unsichtbar.
Styles und Zustand bleiben unveränderlich.
Die Terminal-Capability ist der veränderliche Empfänger der Zeichenmethoden.

`startup.jni` wählt mit `runtime=../../Runtime/Terminal/Program.cs` das Terminal-Plugin und ordnet Module und Importe zu.
Der Launcher `src/Program.cs` benötigt keine Kenntnis der Terminal-Capabilities.
Relative Pfade beginnen beim Startup-Verzeichnis.
Gemeinsam verwendete Bibliotheken werden einmal initialisiert.
State.SIG lädt SnakeBody.SIG direkt;
Logic, Render und Anwendung verwenden diesen gemeinsamen Zustand ohne zusätzliche abstrakte Körpertypen.
ListType.SIG definiert die persistente Liste einmal für Bibliothek und Anwendung.
Die Listenimporte in Logic und Render binden den Typ ausdrücklich an diese Definition.

Render.SIG und App.SIG verwenden Terminal.SIG als gebundenes Interface.
Der Host übergibt an Frame... Zustand und Event sowie das Interface als Empfänger.
Der Modulgraph benötigt dafür keine native Capability.
Der Renderer verwendet `aTerminal : DrawGame NextState .`;
seine rekursiven Zeichenmethoden reichen dasselbe Interface an der Empfängerposition weiter.
Methoden und private Empfänger können in SPO nicht aus dem Interface extrahiert werden.
App.SIG verbirgt den Spielzustand in einer einzelnen existenziellen Bindung.

## Review und Tests

[API-Design und Review-Prozess](../../API-Design.md) dokumentiert Quellen, verglichene Varianten, SPO-Grenzen und die Refactoring-Kriterien.

```powershell
./SPO/Tests/Readability.ps1
dotnet run --file SPO/src/Program.cs -- --check
dotnet run --file SPO/Tests/Runtime.Tests.cs
```

Die Tests prüfen unter anderem Kehrtwenden,
mehrere Eingaben pro Tick,
Pausen,
Futter,
reproduzierbare Zufallsfolgen,
eine letzte freie Zelle,
Schwanzzellen,
Wand-/Körperkollision,
ein volles Spielfeld,
kleine Fenster,
Offsets,
Clipping,
Cursor-Unabhängigkeit und die Lebensdauer der Capability.