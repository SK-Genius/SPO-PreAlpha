# Snake: Laufzeit-Typprüfungen

Messung vom 2026-11-04 unter Windows mit .NET 11.0.100-rc.3.26522.148.
Schlangenlänge 16, 20 Aufwärmaufrufe, fünf Stichproben mit jeweils 40 Aufrufen.
Die Tabelle zeigt die Mediane; MB sind dezimale Millionen Bytes.
Rohdaten: [results.csv](results.csv).

| Fall | Vorher ms/Aufruf | Nachher ms/Aufruf | Faktor | MB/Aufruf vorher -> nachher |
| --- | ---: | ---: | ---: | ---: |
| Bewegung, nur Logik | 2.453 | 0.431 | 5.69x | 11.85 -> 2.44 |
| Futteraufnahme, nur Logik | 14.035 | 2.865 | 4.90x | 65.78 -> 14.77 |
| Frame mit Bewegung | 16.976 | 3.148 | 5.39x | 83.46 -> 15.23 |
| Frame mit Futteraufnahme | 27.076 | 5.678 | 4.77x | 138.02 -> 27.64 |

Die Baseline ist der nach den Reverts wieder baubare Stand: entfernte Helfer und
Konvertierungsaufrufe sind bereinigt, Typverengungen behalten die Daten bereits bei.
Sie enthält noch die wiederholten Aufruf- und Pattern-Prüfungen.
Der Vergleich misst die anschließenden VM-/Generator-Optimierungen.
Beide Varianten wurden aus demselben Quellensatz als optimierte einzelne Assembly
gebaut und nacheinander ohne parallele Builds/Tests ausgeführt.
`DOTNET_TieredCompilation=0` war für beide Varianten gesetzt.

`frame` und `eat-frame` führen den echten Snake-Frame einschließlich Eingaben,
Zeichencode und nativen Terminal-Adaptern aus. Das Terminal meldet 80×25 Zeichen
und führt keine Bildschirmausgabe aus. OS-Ausgabe und Tick-Wartezeit werden nicht
gemessen. Jeder Aufruf startet mit demselben Zustand; Phase, Kopfposition und
Zufallszustand werden nach jeder Stichprobe geprüft.

## Aktuellen Stand ausführen

```powershell
$env:DOTNET_TieredCompilation = '0'
dotnet run --file SPO/Experiments/Snake/Compare.cs -- frame 16 40
```

Modi: `move`, `eat`, `frame`, `eat-frame`.
Danach folgen Länge und Aufrufe pro Stichprobe. Ohne Argumente: `move 16 100`.
Die normalen dateibasierten Projekte können durch andere Assembly-Grenzen andere
absolute Zeiten liefern als der dokumentierte Vergleichsaufbau.

Der Generator prüft bei bekannten Listen nur die noch offene Pattern-Struktur,
statt den bereits typisierten Inhalt erneut zu durchlaufen. Vollständig bewiesene
Aufrufe ohne Guard werden direkt ausgeführt. VM-Operationen und native Aufrufgrenzen
vertrauen den geprüften Verträgen. Dynamische Muster und Guards bleiben erhalten.
Typverengungen erzeugen weder konvertierte Kopien noch Laufzeit-Typ-Wrapper.

## Stand mit sichtbaren Typen am 2026-11-06

Die obigen Nachher-Werte stammen aus dem früheren Speichermodell.
Der aktuelle Stand verwendet sichtbare `ActualType`-Sichten ausschließlich für `ANY` und Unions,
bereitet alle ausführbaren Spezialisierungen vor VM-Start vor und prüft native Aufrufgrenzen.
Typverengungen bewahren verborgene Nutzdaten; Vergleiche und Matches beachten die sichtbaren Typen.
Dabei können Konvertierungen die Darstellung anpassen, etwa ein sichtbares `ANY`-Feld verpacken.

Gemessen mit demselben Aufbau: optimierte einzelne Assembly, `DOTNET_TieredCompilation=0`,
Länge 16, 20 Aufwärmaufrufe und fünf Stichproben mit jeweils 40 Aufrufen.
Während der Messung liefen keine weiteren Builds oder Tests.
Rohdaten: [results-actual-types-2026-11-06.csv](results-actual-types-2026-11-06.csv).

| Fall | Aktuell ms/Aufruf | Aktuell MB/Aufruf | Faktor gegenüber Revert-Baseline |
| --- | ---: | ---: | ---: |
| Bewegung, nur Logik | 1.188 | 4.559 | 2.06x |
| Futteraufnahme, nur Logik | 10.633 | 23.197 | 1.32x |
| Frame mit Bewegung | 12.947 | 26.766 | 1.31x |
| Frame mit Futteraufnahme | 18.924 | 46.277 | 1.43x |

Der aktuelle Stand ist in allen vier Fällen schneller und allokiert weniger als die Revert-Baseline.
Die früheren Nachher-Bestwerte werden noch nicht erreicht.
