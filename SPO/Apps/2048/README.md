# 2048 als SPO-Konsolenspiel

Vom Repository-Verzeichnis in einer Windows-Konsole starten:

```powershell
dotnet run --file SPO/src/Program.cs -- SPO/Apps/2048
```

Pfeiltasten oder WASD schieben alle Kacheln in die gewählte Richtung.
Die farbige Anzeige zeigt sofort das Ergebnis; es gibt keine Animationen oder zeitgesteuerten Spielzüge.
R startet neu, Esc/Q oder Strg+C beenden das Spiel.
Ein zu kleines Fenster zeigt die benötigte Größe und wartet mit weiteren Zügen.

Das Spielfeld hat 4 × 4 Zellen und beginnt mit zwei zufälligen Kacheln.
Gleiche Kacheln verschmelzen pro Zug höchstens einmal; ihr neuer Wert wird zu den Punkten addiert.
Nur nach einem verändernden Zug erscheint eine neue Kachel auf einem freien Feld: zu 90 % eine 2, sonst eine 4.
Mit 2048 erscheint eine Gewinnmeldung, und das Spiel lässt sich fortsetzen.
Ohne freie Felder oder kombinierbare Nachbarn endet das Spiel.
Die Regeln folgen dem [beschriebenen Spielprinzip](https://de.wikipedia.org/wiki/2048_(Computerspiel)#Spielprinzip).

`Input.spo` übersetzt Events in Aktionen; `Game.spo` verbindet Eingabe, Logik und Darstellung.
`Logic.spo` berechnet neue unveränderliche Zustände; `Render.spo` zeichnet über die vorhandene Terminal-Capability.
Der Zustand enthält nur Spielfeld, Punkte und Zufallszustand.
Das feste Spielfeld besteht aus vier Tupeln mit je vier Zahlen; 0 bezeichnet eine leere Zelle.
Für alle Richtungen wird dieselbe Linksbewegung verwendet, indem das Feld vorher und nachher gespiegelt bzw. transponiert wird.
Gewinn und Spielende werden aus dem Spielfeld abgeleitet.
`Colors` im Renderer erhält vor der Tupelzerlegung den Typ `[Color, Color]`,
damit die unterschiedlichen Farbpaar-Typen der Match-Zweige auf einen gemeinsamen Tupeltyp erweitert werden.
Am App-Export wird das Record-Ergebnis von `NewGameWithSeed...` direkt als `Initial` verwendet.
Sein Typ enthält genau Board, Score und RandomState und ist unveränderlich.
Konvertierungen auf einen Record-Typ mit weniger Feldern übernehmen nur dessen benannte Felder.

Wie Snake verwendet die App den expliziten Zufallsgenerator der Bibliothek.
Der erste Start nutzt Seed 1 und ist reproduzierbar; ein Neustart setzt die vorhandene Zufallsfolge fort.
Ticks und nicht relevante Events verändern den Zustand nicht und zeichnen kein neues Bild.
`startup.jni` verwendet dieselbe Terminal-Runtime wie Snake.

Prüfen, ohne eine Spielkonsole zu öffnen:

```powershell
dotnet run --file SPO/src/Program.cs -- SPO/Apps/2048 --check
dotnet run --file SPO/Tests/2048.Tests.cs
```

Die Tests prüfen alle vier Richtungen, Lücken, Merge-Reihenfolge, Punkte, unveränderte Züge,
reproduzierbare Kacheln, die letzte freie Zelle, Spielende, Weiterspielen nach 2048,
Tasteneingaben, Neustart, Fenstergröße und farbige Darstellung ohne Blinken.
