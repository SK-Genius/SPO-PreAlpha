## TUI Design Document (v0)

### Ziel

Ein einzelner **Double-Buffer**, der effizient in ein Terminal rendert. Er abstrahiert keine Terminals – er gehört **einer Sitzung**, hat **eine feste Größe**, und **diffed** Frame-zu-Frame, um minimalen ANSI-Output zu erzeugen.

### Hauptprinzipien

1. **Ein Terminal, ein Buffer**
   Kein Multi-Target, keine Off-Screen-Instanzen. `Present` schreibt direkt an das Terminal-Capability-Objekt.
2. **Doppel-Buffer**
   * `Front` = sichtbarer Zustand
   * `Back` = Frame-Ziel
3. **Rasterbasiert**
   * 2D-Gitter aus Zellen (`Cell`)
   * Jede Zelle = Zeichen + Style
4. **Keine Layoutlogik**
   * Buffer ist rein low-level, IMGUI baut darauf auf.

### Stylemodell

#### Farben

* 16 feste Farben (8 normal, 8 bright)
* Mapping ANSI: 30–37/40–47 (normal), 90–97/100–107 (bright)

#### Modifiers

| Modifier      | ANSI-SGR | Verhalten           |
| ------------- | -------- | ------------------- |
| Bold          | 1        | evtl. hellere Farbe |
| Underline     | 4        | Unterstrichen       |
| Strikethrough | 9        | Durchgestrichen     |
| Blink         | 5        | Meist ignoriert     |

### Datenmodell

| Struktur         | Bedeutung                      |
| ---------------- | ------------------------------ |
| **Cell**         | (`char`, `Style`, `Layer`)     |
| **Style**        | (`FGColor`, `BGColor`, `Mods`) |
| **Buffer**       | 2D-Raster `Width × Height`     |
| **DoubleBuffer** | (Front, Back) + Diff/Present   |

### Frame-Ablauf

1. App schreibt in `Back`
2. `Present()` vergleicht `Back` mit `Front`, sendet nur Diffs
3. `Front` ← `Back`

### Optimierungen

* Keine Clears: nur Diffs
* Runs gleicher Styles zusammenfassen
* ANSI minimieren (keine redundanten SGR)
* Cursor 1-basiert
* UTF-8 direkt, keine Allokationen

### Grenzen / Annahmen

* Kein TrueColor
* keine Wide-Chars
* Kein Scrolling
* kein History
* Kein Multi-Threading

### Hilfsklassen für Geometrie

#### t2xInt32

* Ein einfacher **zweidimensionaler Vektor** aus `int`.
* Wird genutzt für Positionen, Offsets, Größenberechnungen.
* Eigenschaften:
  * `X : int`
  * `Y : int`
* Operationen (konzeptionell):
  * Addition/Subtraktion von Vektoren
  * Skalierung
  * Min/Max für Clamping

#### tRectInt32

* Ein **Rechteck** basierend auf `t2xInt32`.
* Definiert sowohl **Position** als auch **Größe**.
* Eigenschaften:
  * `Pos : t2xInt32`
  * `Size : t2xInt32`
* Abgeleitete Werte:
  * `Min : t2xInt32`, `Max : t2xInt32`
* Operationen (konzeptionell):
  * Schnittmengenbildung (für Clipping)
  * Test „enthält Punkt?“
  * Verschiebung

Diese Hilfstypen sind die Basis für alle Frame- und Layoutoperationen.

### Frame / 2D-View auf den Buffer

#### Struktur von tFrame

Ein `tFrame` ist eine **2D-Sicht** auf einen `tCellBuffer` und enthält:
* `Buffer : tCellBuffer` – Referenz auf den gemeinsamen Speicher.
* `ClipRect : tRectInt32` – Bereich in Buffer-Koordinaten, in den dieser Frame schreiben darf.
* `LayerValue : int` – Layer dieses Frames.
* `CurrentSize : t2xInt32` – tatsächlich genutzte Größe relativ zum Frame-Ursprung `(0,0)`.

Weitere Zustände wie Cursor-Position werden **nicht** im Frame selbst gespeichert, sondern von Containern verwaltet.

Buffer ist private und tFrame operationen sind write only. Widgets können also nicht den Inhalt von darunter ligenden Layer (und auch keinem anderen Layer) abfragen.

`tFrame` ist die zentrale Basis für alle Zeichen- und Layout-Operationen.

#### Layer-Konzept

Um Frames sauber abschließen zu können (z. B. Rahmen nachträglich zeichnen, Hintergrund füllen), bekommt jede Zelle und jeder Frame einen **Layer-Wert**:
* **tCell.LayerLevel**: `int`, der Layer, auf dem diese Zelle zuletzt beschrieben wurde.
* **tFrame.LayerLevel**: `int`, der Layer dieses Frames.
* **Subframe-Regel**: `Child.LayerLevel = Parent.LayerLevel + 1`.

Damit gilt:
* Beim Schreiben über einen Frame werden nur Zellen auf **dem Layer des Frames** beschrieben.
* vorherige Inhalte aus unteren Layers bleiben im Buffer (Layer kleiner), können aber am Ende gezielt überschrieben werden.

#### Dynamische, tatsächliche Größe des Frames

* Zu Beginn eines Frames ist nur die **maximale Fläche** bekannt (das Clip-Rect).
* Die **tatsächlich genutzte Größe** ergibt sich aus den Zellen, in die auf diesem Frame-Layer geschrieben wurde.
* `CurrentSize` ist ein **t2xInt32**. (kein tRectInt32, da die Position relativ zum Frame immer `(0,0)` ist)
* Es repräsentiert ausschließlich die maximale Breite und Höhe, in die dieser Frame geschrieben hat.
* Der Frame-Kontext hält laufend eine **CurrentSize**:
  * Nach jedem Zeichnen wird `CurrentSize` anhand der betroffenen Zellen geupdatet (maximale X/Y, relativ zum Frame-Ursprung).
  * `CurrentSize` ist jederzeit auslesbar und beschreibt das minimal umschließende Rechteck der vom Frame beschriebenen Zellen.
* Am Ende kann diese Größe verwendet werden, um z. B. einen **Rahmen** exakt um den Inhalt zu zeichnen, ohne vorher alle Maße zu kennen.
* Beim Zeichnen kann ein Flag benutzt werden der audrückt ob man die zellen immer überschreiben möchte, oder nur wenn sie ein kleineren LayerLevel hat. Somit lässt sich am ende der gesamte genutzte Hintergrund füllen. das Flag sollte tBlendMode heisen und zunächst nur Allways und IsHigerLayer besitzen. Dies wird in zukunft vilecht noch um OnlyBGColor, OnlyBGCplor, OnlyColors, OnlyStyle und so ergänzt.

Damit sind drei Ziele erreicht:
* **Dekoration nach Inhalt**: Rahmen & Hintergründe können erst am Ende, aber trotzdem lokal und korrekt, gezeichnet werden.
* **Kein unkontrolliertes Übermalen**: Äußere Frames bleiben solange sichtbar, bis ein innerer Frame sie explizit füllt.
* **Layout-Information**: Container bekommen eine zuverlässige, tatsächlich genutzte Größe für ihr Flow-Layout.

**Nutzung der finalen Größe**:
* `CurrentSize` (oder eine angepasste Variante) wird dem Container (z. B. `Col`, `Row`, `HFlow`, `VFlow`) zurückgemeldet.
* Der Container nutzt diese **endgültige Frame-Größe**, um seinen Cursor für den nächsten Frame weiterzubewegen.

#### Idee

* Ein `Frame` ist eine **2D-Sicht** auf den `tCellBuffer`.
* Ähnlich zu `Span<T>`: kein eigener Speicher, nur ein **Fenster** auf den vorhandenen Buffer.
* Alle Zeichenoperationen laufen über `Frame` und sind automatisch auf dessen Rechteck **geclippt**.
* Frames können **rekursiv** voneinander abgeleitet werden, aber der Einstiegs-Frame kommt immer vom Buffer.

#### Eigenschaften

* Referenziert genau einen `tCellBuffer`.
* Hält ein **Rect**: `(originX, originY, width, height)` in Buffer-Koordinaten.
* Alle Koordinaten, die Widgets/Layouts sehen, sind **lokal** zum Frame (0..width-1, 0..height-1).
* Schreibzugriffe außerhalb dieses Bereichs sind No-Op (hart geclippt).

#### Operationen (konzeptionell)

* `Frame.FromBuffer(buffer)` → Root-Frame über den ganzen Buffer.
* `Frame.Clip(rectLocal)` → Subframe:
  * Übersetzt `rectLocal` in Buffer-Koordinaten relativ zum aktuellen Frame.
  * Bildet die **Schnittmenge** mit dem Frame-Rect.
  * Liefert einen neuen Frame, der auf die Schnittmenge zeigt.
  * Ist die Schnittmenge leer → `Frame` mit `HasArea = false`, alle Draws sind No-Op.

#### Koordinatentransformation

* Lokal → Global: `(xGlobal, yGlobal) = (xLocal + originX, yLocal + originY)`.
* Widgets & Layouts arbeiten ausschließlich in lokalen Koordinaten.
* Der Buffer kennt keine Frames; `Frame` übersetzt nur nach global.

#### Rekursion

* Jeder Subframe kann wieder als Ausgangspunkt für weitere `Clip`-Operationen dienen.
* Es gibt keine Parent-Pointer, nur `(buffer, origin, size)` pro Frame.
* Dadurch bleibt die Struktur flach und billig, aber beliebig verschachtelbar.

### Layoutsystem & Widgets

#### Beispiel für typische Benutzung


```cs
__ => {
  __.Start(tDirection.L2R_T2B);
  __.Label($"Counter: {Count}");
  __.Break();
  if (__.Button(1, "Increment")) {
    __.Label($"{Count} -> {Count + 1}");
    Count += 1;
  }
  if (__.Button(2, "Decrement")) {
    __.Label($"{Count} -> {Count - 1}");
    Count -= 1;
  }
}
```

Ein Container ruft seine Children über Lambdas auf.

Ein fiktives Beispiel:

```cs
__ => {
  __.WrapedList(
    tDirection.L2R,
    tBorderStyle.DoubleBorder,
    [
      aCtx => { aCtx.Label("Item1"); }, 
      aCtx => { aCtx.Label("Item2"); },
    ]
  )
}
```

Dies zeigt die Grundidee:
* `WrapedList` ist ein Container (Extension auf `tContext`).
* Das Body-Lambda erhält Frame, FocusPath und Input.
* Widgets wie `DrawLabel` und `DrawButton` sind Elementfunktionen, die auf dem Frame zeichnen und Größen/Fokus/Input lokal verwalten.
* Der Container verwaltet intern den Cursor und erzeugt für jedes Child einen Subframe.

#### Container & Widget als Extension-Methoden

* Container sind im grunde Widgets welche andere Widgets über die argumente einbinden.
* Ein **Widgets** und **Container** sind konzeptionell **statische Extension-Methode** sie `tContext` "erweitern".
* Der Container verwaltet intern eine **lokale Cursor-Position** (z. B. für H-/V-Flow) und erzeugt für jedes Kind einen neuen Subframe.
* Kinder sind wiederum nur Funktionen, die einen `tContext` (Subcontext) entgegennehmen und darauf zeichnen.

Beispiele (konzeptionell):
* `HFlow(this tFrame aFrame, …)`
* `VFlow(this tFrame aFrame, …)`
* `Row/Col/Split` etc.

Der Einstiegspunkt bleibt immer ein `tFrame`, der vom Buffer abgeleitet wurde.

#### Grundidee

* Kein Solver, nur Flow & Container.
* Verschachtelung per Lambda statt `Begin/End` (siehe Container).
* Jeder Container setzt Kontext über Subframes (`ClipRect`, `LayerValue`, `CurrentSize`).

#### Layer-Konzept

Um Frames sauber abschließen zu können (z. B. Rahmen nachträglich zeichnen, Hintergrund füllen), bekommt jede Zelle und jeder Frame einen **Layer-Level**:
* **tCell.LayerLevel**: `tInt32`, der Layer, auf dem diese Zelle zuletzt beschrieben wurde.
* **tFrame.LayerLevel**: `tInt32`, der Layer dieses Frames.
* **Subframe-Regel**: `Child.LayerValue = Parent.LayerValue + 1`.

Damit gilt:
* Beim Schreiben über einen Frame werden nur Zellen auf **dem Layer des Frames** beschrieben.
* Ältere Inhalte aus äußeren Frames bleiben im Buffer (Layer kleiner), können aber am Ende gezielt überschrieben werden.

#### Dynamische, tatsächliche Größe des Frames

* Zu Beginn eines Frames ist nur die **maximale Fläche** bekannt (das Clip-Rect).
* Die **tatsächlich genutzte Größe
* `CurrentSize` ist ein **t2xInt32**, da die Position relativ zum Frame immer `(0,0)` ist.
* Es repräsentiert ausschließlich die maximale Breite und Höhe, in die dieser Frame geschrieben hat.** ergibt sich aus den Zellen, in die auf diesem Frame-Layer geschrieben wurde.
* Der Frame-Kontext hält laufend eine **CurrentSize**:
  * Nach jedem Zeichnen wird `CurrentSize` anhand der betroffenen Zellen geupdatet (maximale X/Y, relativ zum Frame-Ursprung).
  * `CurrentSize` ist jederzeit auslesbar und beschreibt das minimal umschließende Rechteck der vom Frame beschriebenen Zellen.
* Am Ende kann diese Größe verwendet werden, um z. B. einen **Rahmen** exakt um den Inhalt zu zeichnen, ohne vorher alle Maße zu kennen.

#### Abschluss eines Frames

Beim „Schließen“ eines Frames passiert logisch Folgendes:
1. **Rahmen / Dekoration** (optional):
   * Auf Basis von `CurrentSize` können Widgets oder der Container z. B. einen Rahmen um den Inhalt zeichnen – ebenfalls auf dem Layer des Frames.
2. **Hintergrund füllen**:
   * Für alle Zellen im Frame-Rechteck, deren `LayerValue` **kleiner** ist als `Frame.LayerValue`, gilt:
     * Sie wurden nicht von diesem Frame beschrieben.
     * Sie werden nun mit einem **Leerzeichen** und der **Hintergrundfarbe des Frames** überschrieben.
   * Ergebnis: Der Frame-Bereich ist vollständig definiert (kein „Durchscheinen“ von äußeren Frames).
3. **Nutzung der finalen Größe**:
   * `CurrentSize` (oder eine angepasste Variante) wird dem Container (z. B. `Col`, `Row`, `HFlow`, `VFlow`) zurückgemeldet.
   * Der Container nutzt diese **endgültige Frame-Größe**, um seinen Cursor für den nächsten Frame weiterzubewegen.

Damit sind drei Ziele erreicht:
* **Dekoration nach Inhalt**: Rahmen & Hintergründe können erst am Ende, aber trotzdem lokal und korrekt, gezeichnet werden.
* **Kein unkontrolliertes Übermalen**: Äußere Frames bleiben solange sichtbar, bis ein innerer Frame sie explizit füllt.
* **Layout-Information**: Container bekommen eine zuverlässige, tatsächlich genutzte Größe für ihr Flow-Layout.
