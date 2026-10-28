# Immediate GUI in SPO

Das Demo orientiert sich an `GUI_Lib/ImGUI.SDL3.Demo`: Name eingeben, Save auslösen und die Ausrichtung der umbrochenen Absätze ändern.

Vom Repository-Hauptverzeichnis:

```powershell
dotnet run --file SPO/src/Program.cs -- SPO/Apps/GUI_Demo
dotnet run --file SPO/src/Program.cs -- SPO/Apps/GUI_Demo --check
```

Der Host benötigt die native SDL3-Bibliothek ab Version 3.2.
Eine lokal installierte `SDL3-CS.Native`-Version im NuGet-Cache wird automatisch gefunden.
Alternativ kann der Bibliothekspfad explizit gesetzt werden:

```powershell
$env:SDL3_LIBRARY = 'D:\Pfad\zu\SDL3.dll'
dotnet run --file SPO/src/Program.cs -- SPO/Apps/GUI_Demo
```

Aus dem Verzeichnis `SPO` lautet derselbe Aufruf:

```powershell
dotnet src\Program.cs --no-restore -- Apps\GUI_Demo
```

`startup.jni` wählt mit `runtime=../../Runtime/GUI/Program.cs` das SDL-Plugin.
Der gemeinsame Launcher startet es und reicht Optionen und Exitcode durch.
Die Runtime referenziert weder GUI_Lib noch SDL3-CS oder andere verwaltete Bibliotheken.
Der Modulcheck und die Headless-Tests benötigen kein SDL.
Ein anderer App-Ordner oder eine `startup.jni` kann als Argument angegeben werden.

## Bedienung

- Klicken setzt den Fokus;
  Buttons aktivieren beim Loslassen innerhalb ihrer Fläche.
- Tab und Shift+Tab wechseln den Fokus mit Umlauf.
- Enter und Space aktivieren den fokussierten Button.
- Text wird am Ende angefügt;
  Backspace entfernt das letzte UTF-16-Zeichen.
- Save übernimmt den Text in die Statusanzeige;
  es wird keine Datei geschrieben.
- Left, Block, Center und Right ändern die Absatzausrichtung.
- Fenstergröße ändern berechnet das Layout neu;
  das Mausrad scrollt überlaufenden Inhalt.
- Escape oder Schließen beendet das Programm.

## Aufteilung

`../../Libs/GUI/GUI.spo` implementiert die GUI als unveränderliche Werte:

- Elementbäume aus Text, Button, TextInput, Paragraph und verschachtelten Listen;
- horizontales und vertikales Layout mit Padding, Abstand, Ausrichtung und Stretch;
- Wortumbruch und Blocksatz mit linksbündiger letzter Zeile;
- Hit-Tests, Fokusreihenfolge, Hover, gedrückte Buttons und Klick-Abbruch;
- Texteingabe und Backspace;
- Zeichenreihenfolge, Widgetfarben, Rahmen, Caret und Clipping.

Koordinaten, Abstände und Größen sind logische Fensterpixel;
Farben sind RGB-Tupel mit Werten von 0 bis 255.
Interaktive Elemente bekommen eindeutige, positive IDs.
`0` bedeutet kein Fokus beziehungsweise keine Aktivierung.
Listen unterstützen Start, Center, End, SpaceBetween und SpaceEvenly entlang ihrer Achse sowie Stretch quer dazu.
Absätze verwenden Start, Center, End oder SpaceBetween für Blocksatz.

`Demo.spo` exportiert `Initial` und `Frame...` gemäß Libs/GUI/App.SIG.
Frame... verarbeitet ein Ereignis, baut den Elementbaum aus dem Zustand neu auf und zeichnet die Oberfläche.
Es liefert `(NextState, ContinueRunning)`.
Die Anwendungsdaten sind Name, Status und Ausrichtung.
Der GUI-Zustand enthält Fokus, aktives Element, Mausposition, Scrollposition und die Hit-Flächen der Eingabeelemente des vorherigen Frames.
Der Elementbaum entsteht bei jedem Frame neu.
Die Runtime reicht den aktuellen Zustandswert zwischen Frame-Aufrufen weiter.

`../../Runtime/GUI/` enthält das Runtime-Plugin, die Capability-Brücke und direkte SDL3-Aufrufe für Fenster, Zeichenprimitive und Eingabeereignisse.
Ein privater Identitätstoken und seine Lebensdauer schützen das Handle.
Native Pointer bleiben in C#.
Das Frontend arbeitet in logischen Fensterpixeln;
der Renderer berücksichtigt das Verhältnis zu den Ausgabepixeln.

Das Demo ist ein Proof of Concept und Vorzeigeprojekt für SPO.
Textzerlegung, Listenverarbeitung und alle Layoutberechnungen bleiben in SPO.
Der aktuelle Interpreter macht die UI unbedienbar langsam.
Das Demo bleibt dennoch erhalten, um Sprachkonzepte zu zeigen und zukünftige Performanceverbesserungen mit demselben Aufbau und denselben Interaktionen vergleichen zu können.

Die Widget-API arbeitet unmittelbar beim Aufbau: AddButton liefert die Aktivierung zurück, AddTextInput den bearbeiteten Text.
Anwendungscode verwendet die Ergebnisse direkt am jeweiligen Widget:

```spo
§VAR Panel := cEmptyPanel, BeginFor Event With UI .
Panel :
	AddTextInput cNameId WithLabel "Name" Value Name => §DEF NextName
	AddButton cSaveId WithText "Save" Selected §FALSE => §DEF SaveClicked
.
§DEF NextStatus = §IF SaveClicked MATCH {
	§TRUE: "Saved: " .FollowedBy NextName
	§FALSE: Status
}
Panel : AddText NextStatus InColor cForeground .
```

GUI.SIG stellt dafür EmptyPanel und BeginFor...With... bereit.
Das Panel ist eine lokale Variable;
Methoden machen Aufbau und Eingabeverbrauch explizit.
Mit AddListWith...Do... werden verschachtelte Listen durch unmittelbar ausgeführte Prozeduren aufgebaut.
Die Prozedur erhält das Panel als veränderlichen Empfänger;
ihr Rückgabewert wird nach dem Einfügen der Liste unverändert weitergereicht:

```spo
Panel : AddListWith cStyle Do (
	(§VAR aPanel) : () {
		aPanel : AddTextInput cNameId WithLabel "Name" Value Name => §DEF NextName .
		§RETURN NextName
	}
) => §DEF NextName .
```

Zusätzliche veränderliche Werte stehen ebenfalls im Empfänger.
Dafür gibt es AddListWith...DoWithContext...;
gewöhnliche Argumente erteilen keine Zugriffsrechte:

```spo
(Panel, Counter) : AddListWith cStyle DoWithContext (
	(§VAR aPanel, §VAR aCounter) : () {
		aPanel : AddButton cSaveId WithText "Save" Selected §FALSE => §DEF Clicked .
		aCounter : Increment .
		§RETURN Clicked
	}
) => §DEF SaveClicked .
```

Increment steht hier für eine Methode auf dem Zähler, wie im Testverbraucher.
Der Kontext kann auch ein Tupel mehrerer veränderlicher Werte sein.
Beide Varianten unterstützen beliebige Rückgabewerte, einschließlich `()`.
Verschachtelte Aufrufe werden synchron jeweils einmal ausgeführt.
Ein früher Return beendet nur die übergebene Prozedur;
die Liste wird danach weiterhin ins Elternpanel eingefügt.
Der vorherige Inhalt wird lokal gesichert;
im Panel gibt es keinen Elternstack.
GUI.Scopes.spo prüft diese Fälle mit zusätzlichen Empfängern und Ergebnissen.

FinishWith...In... liefert `(NextUI, Frame)`: Der GUI-Zustand enthält nur die Eingabedaten für den nächsten Frame, Frame die aktuelle Darstellung.
Layout und Wortumbruch bleiben reine Berechnungen in SPO.
Ein Folgeframe ohne Eingabe zeigt auch die Änderung eines bereits zuvor aufgebauten Widgets, etwa die neue Button-Auswahl.
Er löst keine erneute Aktivierung oder Texteingabe aus.

Verglichen wurden Elementdaten mit späterem Activated-ID-Vergleich und direkte Widget-Ergebnisse im Aufbau.
Die direkten Rückgaben erhalten die Schreibweise des C#-Vorbilds.
IDs bleiben innerhalb der GUI für Fokus und Hit-Tests notwendig.
Die Anwendung benötigt keine zentrale Klickzuordnung oder separate Textbearbeitung.

Surface.SIG beschreibt ein gebundenes Interface für die externe Ressource.
Der Host übergibt es als Empfänger an Frame...;
Zustand und Event sind die Argumente.
Die Zeichenmethode verwendet dasselbe Interface in `aSurface : Draw Frame With Interaction .`.
Sämtliche Module werden ohne native Capability geladen.
Der Compiler verhindert das Extrahieren von Methoden und Empfänger;
Identität und Lebensdauer des privaten veränderlichen Empfängers prüft zusätzlich die Runtime.

## Grenzen

Wie das Vorbild verwendet die Runtime die feste SDL-Debugschrift mit 8×8 Pixeln und ASCII-Glyphen.
Die SPO-Bibliothek berechnet daraus Zeichenbreiten und eine Zeilenhöhe von 16 logischen Pixeln.
UTF-16-Textwerte bleiben erhalten, auch wenn die Debugschrift ihre Zeichen nicht darstellen kann.
Einzelne Wörter werden nicht getrennt;
überlange Wörter werden am Fenster abgeschnitten.
[SDL_RenderDebugText](https://wiki.libsdl.org/SDL3/SDL_RenderDebugText) beschreibt diese nativen Einschränkungen.

Der Windows-UI-Automation-Provider des Vorbilds ist noch nicht portiert.
Textauswahl, frei positionierbarer Caret, Zwischenablage und IME-Kompositionsanzeige sind ebenfalls nicht Bestandteil dieses Demos.
Commit-Text von SDL wird verarbeitet.
Die Event-Brücke setzt einen 64-Bit-Prozess voraus;
ihre Struct-Offsets entsprechen dem stabilen SDL3-ABI in [SDL_events.h](https://github.com/libsdl-org/SDL/blob/release-3.2.28/include/SDL3/SDL_events.h).

Die Folds und ihre Aggregat-Parameter haben explizite gemeinsame Typen.
Ihre Ergebnisse können im erzeugten IL trotzdem eine Union verschiedener Record-Typen behalten.
Beispielsweise scheitert `{Words: §DEF Words, Word: §DEF Word} = SplitText`
auch mit `SplitText € tSplitWords` noch mit `expected record`.
`FinishWords`, `FinishWrapping` und der lokale Zugriff `Layout .Items` stellen deshalb
über ihre typisierten Parameter eine gemeinsame Record-Grenze her.
Auch `ReplaceWith` setzt eine notwendige Typgrenze:
Bei `aPanel := Panel .` mit leerem `Children`-Feld erwartet die Zuweisungsinferenz
derzeit einen Empfänger vom Typ `§VAR` eines Panels mit ausschließlich leerer Kinderliste.
Der typisierte Parameter `aValue € tPanel` hält die Zuweisung stattdessen beim vollständigen Paneltyp.
`PanelFor...With...` hält die Event-Auswertung rein.
Wird ihr Match direkt in `BeginFor...With...` mit seinem veränderlichen Panel-Empfänger verschoben,
scheitert aktuell bereits die Berechnung von `NextScroll` mit `opaque mutable access is not allowed`.

Match-Zweige werden derzeit als Funktionen übersetzt.
Ein darin eingefangener `var`-Empfänger würde eine veränderliche Funktions-Closure erzeugen.
Der Renderer erstellt deshalb mit einem reinen Fold aus den platzierten Elementen Zeichenmethoden.
Ihre Closures enthalten nur nachweislich unveränderliche Daten und andere Methoden.
Bounds-Records aus Funktionsaufrufen können im WIP-Zugriffsmodell verborgene veränderliche Zellen enthalten.
Die Zeichenmethoden erfassen deshalb X, Y, Width und Height einzeln und bauen den Bounds-Record unmittelbar am Zeichenaufruf.
Erst `aSurface : DrawItems .` bindet die externe Ressource als Methodenempfänger.
Veränderliche Kontexte bleiben an der Empfängerposition;
gewöhnliche Match-Argumente und Closure-Umgebungen erteilen keine solchen Rechte.

Lange Textliterale können beim rekursiven Prüfen ihrer Paartypen den nativen Stack erschöpfen.
Die beiden Demoabsätze entstehen deshalb mit `JoinText` aus kurzen Textteilen.
Die Teile und das Ergebnis sind unveränderlich.

## Prüfung

```powershell
./SPO/Tests/Readability.ps1
dotnet run --file SPO/Tests/GUI.Tests.cs
dotnet run --file SPO/Tests/SDL3.Tests.cs
```

Der native Test verwendet den SDL-Dummy-Videotreiber mit echtem Software-Renderer, injiziert SDL-Ereignisse und erzeugt `.tmp/GUI_Demo.bmp` für die visuelle Prüfung.
Er prüft außerdem das Freigeben und erneute Anlegen eines Fensters.
Die Headless-Tests verwenden zwei TextInputs und prüfen direkte Rückgaben,
getrennte Werte,
Tab/Shift+Tab,
Backspace im leeren Feld,
Mausfokus,
Klick-Abbruch,
Tastenwiederholung,
Fokusverlust und die Auswahl im Folgeframe.
