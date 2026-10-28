# Runtime-Plugins

Alle Apps starten über `SPO/src/Program.cs`.
Die App wählt ihre Runtime in `startup.jni`;
der Launcher enthält keine Liste von Terminal-/GUI-Runtimes.

```text
runtime=../../Runtime/GUI/Program.cs
entry=Demo
modules:
|Demo:
||path=Demo.*
```

Runtime- und Modulpfade sind relativ zur `startup.jni`.
Der Launcher erwartet einen nichtleeren Runtime-Pfad zu einer vorhandenen `.cs`-, `.dll`- oder `.exe`-Datei.
Die Runtime läuft in einem eigenen Prozess.
C#-Quelldateien und .NET-DLLs starten über `dotnet`, EXE-Dateien direkt.
Eine neue Runtime benötigt weder eine Registrierung noch eine Referenz im Launcher.

Der gemeinsame Aufruf aus `SPO` ist:

```powershell
dotnet src\Program.cs --no-restore -- Apps\GUI_Demo
dotnet src\Program.cs --no-restore -- Apps\Snake --check
```

## Prozessvertrag

- Erstes Plugin-Argument: absoluter Pfad zur `startup.jni`.
- Weiteres Argument: optional `--check`.
- Arbeitsverzeichnis: der Ordner der `startup.jni`.
- Standardeingabe, Standardausgabe und Fehlerausgabe werden geerbt.
- Der Launcher wartet und liefert den Exitcode der Runtime unverändert zurück.

Pfade und Optionen werden als einzelne Argumente übergeben.
Leerzeichen in App-/Pluginpfaden benötigen keine zusätzliche Quotierung in der Konfiguration.
Die Runtime lädt den Modulgraphen und prüft den App-Vertrag.
Anschließend ruft sie `Frame...` mit Zustand und Ereignis auf;
der Empfänger ist ein gebundenes Interface-Objekt.
Dessen privater veränderlicher Empfänger bleibt im Host.
Terminal verwendet Libs/App.SIG und Terminal.SIG, GUI verwendet Libs/GUI/App.SIG und Surface.SIG.
`Initial` liefert den Anfangszustand;
`Frame...` verarbeitet Eingaben und zeichnet, dann liefert es `(NextState, ContinueRunning)`.
Die Runtime präsentiert den Frame, führt die Eventschleife aus und gibt native Ressourcen vor dem Ende wieder frei.
Die GUI-Runtime ruft nach einem Eingabe-Frame einmal Frame... mit Redraw auf.
Dadurch werden auch früher aufgebaute Widgets mit dem zuletzt berechneten Anwendungszustand dargestellt.
Redraw enthält keine erneute Eingabe.
Terminal-Apps geben zusätzlich `TickMilliseconds` an.
`startup.jni` enthält ausschließlich Dateimodule und ihre Importe;
native Capabilities werden über den App-Vertrag von außen übergeben.
`--check` muss ohne Fenster und native Ressourcen auskommen.

Die mitgelieferten Plugins sind `Terminal/Program.cs` und `GUI/Program.cs`.
Sie verwenden den gemeinsamen SPO-Modullader sowie `mRuntime.cs` für VM-Aufrufe und Recordzugriffe.
GUI benötigt keine Terminal-Implementierung.
Eine externe Runtime kann ihren eigenen Einstieg verwenden und erhält dieselbe Konfiguration.

Bei `.cs`-Plugins übernimmt das installierte .NET SDK Restore und Build des Plugins.
Das vor `--` angegebene `--no-restore` betrifft den Launcher.
Für eine bereits veröffentlichte Runtime kann `runtime` direkt auf deren DLL oder EXE zeigen.

## Prüfung

```powershell
dotnet run --file SPO/Tests/Launcher.Tests.cs
dotnet run --file SPO/src/Program.cs -- SPO/Apps/GUI_Demo --check
dotnet run --file SPO/src/Program.cs -- SPO/Apps/Snake --check
```

Die Launchertests starten unabhängige C#-/DLL-/EXE-Plugins und prüfen relative Pfade, Leerzeichen, Optionen, Exitcodes und fehlende/ungültige Runtime-Angaben.