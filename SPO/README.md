# SPO-Demos

Vom Repository-Hauptverzeichnis mit dem installierten .NET SDK starten:

```powershell
dotnet SPO/src/Program.cs -- SPO/Apps/Snake
dotnet SPO/src/Program.cs -- SPO/Apps/GUI_Demo
```

Ohne App-Pfad startet Snake.
`--check` prüft den vollständigen Modulgraphen und den App-Vertrag, ohne ein Fenster zu öffnen oder SDL zu laden:

```powershell
dotnet SPO/src/Program.cs -- SPO/Apps/Snake --check
dotnet SPO/src/Program.cs -- SPO/Apps/GUI_Demo --check
```

[Snake](Apps/Snake/README.md) verwendet die Windows-Konsole.
Die [GUI-Demo](Apps/GUI_Demo/README.md) benötigt SDL3 ab Version 3.2;
`SDL3_LIBRARY` kann auf die native Bibliothek zeigen.
Eine lokal installierte `SDL3-CS.Native`-Version im NuGet-Cache wird ebenfalls gefunden.

## Interpreter und Performance

Die Demos werden mit dem vorhandenen SPO-Interpreter ausgeführt.
Besonders die GUI ist damit aktuell unbedienbar langsam.
Sie bleibt als Demonstration der Sprachkonzepte und als Ausgangspunkt für zukünftige Performancevergleiche erhalten.
Widgets, Layout, Wortumbruch und Interaktion bleiben in SPO.

Für Vergleiche dieselbe App, Fenstergröße und Eingabefolge verwenden und den Compilerstand sowie die .NET-Version .
Start/Kompilierung und Frame-Ausführung getrennt messen;
die erste Darstellung und das Neuzeichnen nach Eingaben sind unterschiedliche Arbeitslasten.
Die vorhandenen Headless-Tests liefern reproduzierbare Abläufe und prüfen dabei die Ergebnisse.

## Aufbau

`Apps/` enthält die Anwendungen, `Libs/` ihre gemeinsamen SPO-Bibliotheken und SIG-Verträge.
Beide Apps verwenden die vorhandene Standardbibliothek unter `src/Std.ILT`.
`src/Program.cs` startet das in `startup.jni` ausgewählte [Runtime-Plugin](Runtime/README.md).
Runtime- und Modulpfade beziehen sich auf die jeweilige Startkonfiguration.

`src/mStartup.cs` prüft den Modulgraphen, erkennt fehlende Importe und Zyklen und teilt gemeinsame Abhängigkeiten.
Bei `.*` wird die SPO-Quelle bevorzugt;
fehlt sie, wird ILT geladen.
Die Übersetzung erzeugt ILT aus den aktuellen Quellen.
Apps erhalten das gebundene Terminal-/Surface-Interface erst beim Frame-Aufruf.
Die erzeugten ILT-Dateien unter `Apps/` und `Libs/` sind in Git ignoriert.

Die Demo-Hosts verwenden für den rekursiven Compiler einen eigenen Thread mit 32 MiB Stack.
Das vermeidet Stacküberläufe der unoptimierten C#-Compilerpfade bei größeren Modulen und verändert die Ausführung des SPO-Interpreters nicht.

[API-Stil und Review](API-Design.md) beschreiben die verwendeten Aufrufkonventionen.

## Prüfung

```powershell
./SPO/Tests/Readability.ps1
dotnet SPO/Tests/Launcher.Tests.cs
dotnet SPO/Tests/Runtime.Tests.cs
dotnet SPO/Tests/GUI.Tests.cs
dotnet SPO/Tests/SDL3.Tests.cs
dotnet SPO/Tests/WindowsTerminal.Tests.cs
```

Die Headless-Tests prüfen Bibliotheken, Gameplay, Widget-Interaktionen sowie Identität und Lebensdauer der Capabilities.
Der SDL-Test verwendet den Dummy-Videotreiber mit echtem Renderer und schreibt `.tmp/GUI_Demo.bmp`.
Der native Terminaltest benötigt eine Windows-Konsole.

Die vollständigen Gameplay-,
GUI- und SDL-Interaktionstests können im aktuellen Interpreter sehr lange laufen,
insbesondere beim fast vollen Spielfeld und beim Zeichnen der vollständigen GUI.
Bei der Übernahme am 27.
Oktober 2026 bestanden 969 Compiler-/VM-Prüfungen, beide App-Startchecks, der native Windows-Terminaltest sowie die Lesbarkeitsprüfung.
Die großen Interpreterfälle wurden nach mehreren Minuten beendet und sind nicht vollständig bestätigt.
Bis dahin bestanden die Prüfungen für Replay, Apfelwahl und Zustandserhalt sowie GUI-Scopes, direkte Widget-Ergebnisse, getrennte Texteingaben, Layout und Wortumbruch.