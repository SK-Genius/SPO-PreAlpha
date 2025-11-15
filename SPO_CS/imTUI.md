## TUI Design Document (v0)

### Ziel

Ein einzelner **Double-Buffer**, der effizient in ein Terminal rendert. Er abstrahiert keine Terminals – er gehört **einer Sitzung**, hat **eine feste Größe**, und **diffed** Frame-zu-Frame, um minimalen ANSI-Output zu erzeugen.

---

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

---

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

#### Reset-Regel

* Nach Frame: `SGR 0`
* Innerhalb Frame: nur Style-Wechsel

---

### Datenmodell

| Struktur         | Bedeutung                    |
| ---------------- | ---------------------------- |
| **Cell**         | (`char`, `Style`)            |
| **Style**        | (`Fg`, `Bg`, `Mods`)         |
| **Buffer**       | 2D-Raster `Width × Height`   |
| **DoubleBuffer** | (Front, Back) + Diff/Present |

---

### Frame-Ablauf

1. App schreibt in `Back`
2. `Present()` vergleicht `Back` mit `Front`, sendet nur Diffs
3. `Front` ← `Back`

---

### Optimierungen

* Keine Clears: nur Diffs
* Runs gleicher Styles zusammenfassen
* ANSI minimieren (keine redundanten SGR)
* Cursor 1-basiert
* UTF-8 direkt, keine Allokationen

---

### Grenzen / Annahmen

* Kein TrueColor, keine Wide-Chars
* Kein Scrolling, kein History
* Kein Multi-Threading

---

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

---

### Frame / 2D-View auf den Buffer

#### Struktur von tFrame

Ein `tFrame` ist eine **2D-Sicht** auf einen `tCellBuffer` und enthält:

* `Buffer : tCellBuffer` – Referenz auf den gemeinsamen Speicher.
* `ClipRect : tRectInt32` – Bereich in Buffer-Koordinaten, in den dieser Frame schreiben darf.
* `LayerValue : int` – Layer dieses Frames.
* `CurrentSize : t2xInt32` – tatsächlich genutzte Größe relativ zum Frame-Ursprung `(0,0)`.

Weitere Zustände wie Cursor-Position werden **nicht** im Frame selbst gespeichert, sondern von Containern lokal verwaltet.

`tFrame` ist die zentrale Basis für alle Zeichen- und Layout-Operationen; alle Container und Widgets sind konzeptionell nur Funktionen/Methoden, die auf einem `tFrame` arbeiten.

#### Layer-Konzept

Um Frames sauber abschließen zu können (z. B. Rahmen nachträglich zeichnen, Hintergrund füllen), bekommt jede Zelle und jeder Frame einen **Layer-Wert**:

* **tCell.LayerValue**: `int`, der Layer, auf dem diese Zelle zuletzt beschrieben wurde.
* **tFrame.LayerValue**: `int`, der Layer dieses Frames.
* **Subframe-Regel**: `Child.LayerValue = Parent.LayerValue + 1`.

Damit gilt:

* Beim Schreiben über einen Frame werden nur Zellen auf **dem Layer des Frames** beschrieben.
* Ältere Inhalte aus äußeren Frames bleiben im Buffer (Layer kleiner), können aber am Ende gezielt überschrieben werden.

#### Dynamische, tatsächliche Größe des Frames

* Zu Beginn eines Frames ist nur die **maximale Fläche** bekannt (das Clip-Rect).
* Die **tatsächlich genutzte Größe** ergibt sich aus den Zellen, in die auf diesem Frame-Layer geschrieben wurde.
* `CurrentSize` ist ein **t2xInt32**, da die Position relativ zum Frame immer `(0,0)` ist.
* Es repräsentiert ausschließlich die maximale Breite und Höhe, in die dieser Frame geschrieben hat.
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

---

### Layoutsystem & Widgets

#### Beispiel für typische Benutzung

Ein Container ruft seine Children über Lambdas auf. Ein fiktives Beispiel:

```
aFrame.Row(
  (aFrame, aFocusPath, aInput) => {
    aFrame.DrawLabel("Test1");

    if (aFrame.DrawButton("Click", aFocusId: 1)) {
      aFrame.DrawLabel("Done");
    }
  }
);
```

Dies zeigt die Grundidee:

* `Row` ist ein Container (Extension auf `tFrame`).
* Das Body-Lambda erhält `aFrame`, `aFocusPath` und `aInput`.
* Widgets wie `DrawLabel` und `DrawButton` sind Elementfunktionen, die auf dem Frame zeichnen und Größen/Fokus/Input lokal verwalten.
* Der Container verwaltet intern den Cursor und erzeugt für jedes Child einen Subframe.

#### Container als Extension-Methoden

#### Container als Extension-Methoden

* Ein **Container** ist konzeptionell eine **statische Extension-Methode** auf `tFrame`.
* Der Container verwaltet intern eine **lokale Cursor-Position** (z. B. für H-/V-Flow) und erzeugt für jedes Kind einen neuen Subframe.
* Kinder sind wiederum nur Funktionen, die einen `tFrame` (Subframe) entgegennehmen und darauf zeichnen.

Beispiele (konzeptionell):

* `HFlow(this tFrame aFrame, …)`
* `VFlow(this tFrame aFrame, …)`
* `Row/Col/Split` etc.

Der Einstiegspunkt bleibt immer ein `tFrame`, der vom Buffer abgeleitet wurde.

#### Grundidee

* Kein Solver, nur Flow & Container.
* Verschachtelung per Lambda statt `Begin/End`.
* Jeder Container setzt Kontext über Subframes (`ClipRect`, `LayerValue`, `CurrentSize`).

#### Container

1. **Group(aRect, aBody)**: setzt neuen Ursprung + Clip über einen Subframe.
2. **HFlow(aSpacing, aBody)**: horizontale Folge, Cursor bewegt sich rechts.
3. **VFlow(aSpacing, aBody)**: vertikale Folge, Cursor bewegt sich nach unten.
4. **SplitH / SplitV**: einfache Zweiteilung über zwei Subframes.

#### Widgets (IMGUI-Stil)

Widgets sind **Fabriken für Elemente**:

* Ein **Widget** ist eine Funktion, die aus seinen Parametern (z. B. Text, Optionen) eine **Elementfunktion** erzeugt.
* Ein **Element** ist die eigentliche Funktion mit der Signatur

  > `(aFrame : tFrame, aFocusPath : tStream<tInt32>, aInput : tInput) => (FocusPath : tStream<tInt32>, HasConsumedInput : tBool)`

Beispiel (konzeptionell):

* `Label : (aText : tText) => Element`
* Aufruf: `var e = Label("Hallo");` und später `e(aFrame, aFocusPath, aInput)`.

Damit lässt sich ein Widget einmal konfigurieren (z. B. Text, Style) und dann als Element in Container/Layouts einhängen.

Alle Widgets liefern eine genutzte Größe (`t2xInt32`) und ggf. Interaktionsergebnis.
(`t2xInt32`) und ggf. Interaktionsergebnis.

| Widget    | Beschreibung | Rückgabe                        |
| --------- | ------------ | ------------------------------- |
| Label     | reiner Text  | `t2xInt32`                      |
| Button    | klickbar     | `bool pressed` + `t2xInt32`     |
| Checkbox  | Toggle       | `bool toggled` + `t2xInt32`     |
| InputLine | Textinput    | `tEdit` + `t2xInt32`            |
| ListView  | Liste        | `int? newSelected` + `t2xInt32` |

#### IDs & Fokus

* Widget-IDs (z. B. für visuelles Feedback) können explizit (`aId`) bleiben.
* Der **Fokus** eines Elements wird unabhängig davon über einen **FocusPath** gesteuert (siehe nächster Abschnitt).

#### Theming

`tTheme` enthält Styles (`Normal`, `Hot`, `Focus`, `Disabled`).

#### Clipping & Text

* Alles außerhalb `ClipRect` eines Frames wird ignoriert.
* Kein Wrap v0 (später `LabelWrap`).

---

### Fokus- und Eingabemodell

#### Layer-Konzept

Um Frames sauber abschließen zu können (z. B. Rahmen nachträglich zeichnen, Hintergrund füllen), bekommt jede Zelle und jeder Frame einen **Layer-Wert**:

* **tCell.LayerValue**: `int`, der Layer, auf dem diese Zelle zuletzt beschrieben wurde.
* **tFrame.LayerValue**: `int`, der Layer dieses Frames.
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

---

### Eigenschaften

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

---

### Layoutsystem & Widgets

#### Grundidee

* Kein Solver, nur Flow & Container.
* Verschachtelung per Lambda statt `Begin/End`.
* Jeder Container setzt Kontext (`Origin`, `Clip`, `Flow`).

#### Container

1. **Group(aRect, aBody)**: setzt neuen Ursprung + Clip.
2. **HStack(aSpacing, aBody)**: horizontale Folge, Cursor bewegt sich rechts.
3. **VStack(aSpacing, aBody)**: vertikale Folge, Cursor bewegt sich nach unten.
4. **SplitH / SplitV**: einfache Zweiteilung.

#### Widgets (IMGUI-Stil)

Alle liefern `tSize used` und ggf. Interaktionsergebnis.

| Widget    | Beschreibung | Rückgabe                     |
| --------- | ------------ | ---------------------------- |
| Label     | reiner Text  | `tSize`                      |
| Button    | klickbar     | `bool pressed` + `tSize`     |
| Checkbox  | Toggle       | `bool toggled` + `tSize`     |
| InputLine | Textinput    | `tEdit` + `tSize`            |
| ListView  | Liste        | `int? newSelected` + `tSize` |

#### IDs & Fokus

* Explizit (`aId`) vom Aufrufer.
* Fokusnavigation später.

#### Theming

`tTheme` enthält Styles (`Normal`, `Hot`, `Focus`, `Disabled`).

#### Clipping & Text

* Alles außerhalb `Clip` wird ignoriert.
* Kein Wrap v0 (später `LabelWrap`).

---

### Fokus- und Eingabemodell

#### Fokuspfad (FocusPath)

* Der Fokus eines Elements wird durch einen **Pfad aus Indizes** beschrieben: `tStream<tInt32>`.
* Ein Element erhält den Fokuspfad als Eingabe:

  * **Leere Liste** → das Element ist **nicht** im Fokus.
  * **Nicht-leere Liste** → das Element ist im Fokus. Die erste Zahl bestimmt, welches seiner Kindelemente fokussiert sein soll.

Ablauf pro Element:

* Root-Element bekommt den globalen `FocusPath` vom Aufrufer.
* Ist die Liste nicht leer:

  * Das Element **entnimmt die erste Zahl** (z. B. „Index des fokussierten Childs“).
  * Nach eigener Logik wählt es ein Kindelement aus und übergibt den **Rest** des Pfads an dieses Sub-Element.
* Alle anderen Kinder, die nicht fokussiert sein sollen, bekommen eine **leere Liste**.

Ein Element, das im Fokus ist, gibt wiederum einen **neuen Sub-Fokuspfad** zurück:

* Hat ein Sub-Element den Fokus, erhält es vom aufrufenden Element eine Liste mit **nur einer `0`** als Start (lokale Definition; globale Zusammensetzung erfolgt im Aufrufer).
* Der Aufrufer kann den von einem fokussierten Kind zurückgelieferten Pfad wieder mit seinem eigenen Index kombinieren, um einen neuen globalen `FocusPath` zu bilden.

Damit ist der Fokus vollständig über eine Liste von Indizes entlang der Elementhierarchie beschrieben, ohne globale, mutierbare Zustände.

#### Tastaturzustände

* Eingabe erfolgt ausschließlich über **Tastatur** (kein Maus, kein Touch in v0).
* Für jede Taste existiert ein Status mit zwei Flags:

  * `IsDownFlag = 0b10`
  * `WasDownFlag = 0b01`

Daraus ergeben sich abgeleitete Zustände:

* `Pressed  = 0b10` (jetzt unten, vorher oben)
* `Released = 0b01` (jetzt oben, vorher unten)
* `Hold     = 0b11` (jetzt unten, vorher unten)
* `None     = 0b00` (jetzt oben, vorher oben)

`WasDownFlag` bezieht sich auf die **vorherige Iteration** (vorheriges Frame).

#### Ereignis (iEvent)

* Zusätzlich zum Key-Zustand gibt es ein **iEvent**, das der Trigger für das Neuzeichnen ist.
* Typischerweise ist das ein `tKeyEvent`, kann aber auch etwas anderes sein (z. B. `tResizeEvent`, `tTaskEvent`).
* Nur `tKeyEvent` ist Teil des TUI-Moduls; andere Events sind projektspezifische Custom-Typen.

`tInput` bündelt:

* den aktuellen Key-Zustand (inkl. `IsDown/WasDown` für jede relevante Taste),
* das konkrete Ereignis (`iEvent`), das diesen Frame ausgelöst hat.

#### Element-Signatur

Ein Element ist konzeptionell eine Funktion mit der Signatur:

> `(aFrame : tFrame, aFocusPath : tStream<tInt32>, aInput : tInput) => (FocusPath : tStream<tInt32>, HasConsumedInput : tBool)`

Bedeutung:

* `aFrame` – Sicht auf den Bereich, in den dieses Element zeichnen darf.
* `aFocusPath` – eingehender Fokuspfad (leer = kein Fokus).
* `aInput` – Tastaturzustände und auslösendes Event.
* Rückgabe:

  * `FocusPath` – neuer Fokuspfad, den dieses Element für seine Substruktur definiert.
  * `HasConsumedInput` – ob dieses Element das aktuelle Eingabeereignis verarbeitet hat (z. B. für Stop der weiteren Propagation).

Mit dieser Signatur lassen sich sowohl einfache Widgets als auch komplexe Container auf dieselbe abstrakte Weise behandeln.

---

### Offene Punkte

1. Blink senden oder ignorieren?
2. Front-Copy: Vollkopie oder Flip-Flag?
3. Buffergröße automatisch oder manuell?
4. SplitH Maßeinheit (Zellen vs Prozent)?

---

### High-Level API: FrameContext

#### Motivation

Die Low-Level-Elementfunktion bildet die korrekte Semantik ab, ist aber zu umständlich für den normalen UI-Autor. Daher existiert eine **High-Level-API**, welche dieselben Funktionen kapselt, aber ergonomisch wie eine imperative DSL funktioniert.

#### FrameContext (tCtxForFrame)

Ein `FrameContext` (kurz `Ctx`) wird pro Frame oder Subframe erzeugt und dient als bequeme, lokal gültige "UI-Umgebung".

Ein `FrameContext` enthält:

* den zugrundeliegenden `tFrame`,
* den aktuellen **Cursor** (t2xInt32),
* Zugriff auf `Input` (Tastaturzustände + aktuelles iEvent),
* Zugriff auf Fokuswerkzeuge (`FocusPath`, `FocusNext()`, `FocusPrev()`, `RequestFocus()`),
* High-Level-Zeichnen (`DrawLabel`, `DrawButton`, …),
* Container-APIs (`Row`, `Col`, `HFlow`, `VFlow`, …).

Der UI-Autor sieht **nie** direkt die Low-Level-Elementsignatur. Stattdessen sieht er nur den `FrameContext`.

#### Trennung der Schichten

* **Low-Level:** `(tFrame, FocusPath, Input) => (FocusPath, Consumed)` bleibt intern und bildet die Grundlage für Rendering, Fokusweitergabe und Inputverbrauch.
* **High-Level:** `FrameContext` bietet Komfortmethoden und orchestriert intern die low-level Elemente.

#### Beispiel: High-Level-API in Benutzung

```
Ctx.Row(__ => {
    if (_.Input.Key[tKey.Right] is tKeyState.Down)
        _.FocusPath.FocusNext();

    _.DrawLabel("Test1");

    if (_.DrawButton("Click"))
        _.DrawLabel("Done");
});
```

Interpretation:

* `Row` erzeugt einen Subframe und einen neuen Child-`Ctx`.
* `DrawLabel` erzeugt intern ein Label-Element, wendet es **sofort** auf den Child-Frame an, aktualisiert Cursor und CurrentSize.
* `DrawButton` erzeugt intern ein Button-Element, wertet Fokus + Input aus und gibt `true/false` zurück.
* Fokusrouting und Input-Konsum passieren automatisch hinter den Kulissen.
* Am Ende eines Containers wird `CurrentSize` des Subframes verwendet, um den Cursor im Parent zu verschieben.

#### Regeln für die High-Level-API

1. Jeder Container erstellt für seinen Body **einen neuen FrameContext**, der auf einem Subframe basiert.
2. Jede Draw-Funktion erzeugt intern ein Low-Level-Element und führt es sofort aus.
3. Der Cursor bewegt sich **immer im High-Level**, niemals in `tFrame`.
4. `CurrentSize` des `tFrame` dient als Rückgabewert für Layout.
5. Fokuspfade werden komplett im High-Level neu zusammengesetzt, basierend auf Low-Level-Rückgaben.

Durch diese Architektur:

* bleibt der Kern deterministisch und testbar,
* aber der Nutzer bekommt eine leichte, imperative DSL.

---

### Nächste Schritte

* Schnittstellen für `tCtx` und Widget-Rückgabestrukturen (`tEdit`, `tHit`, `tFocusState`) definieren.
* Diff-Algorithmus und Swapstrategie evaluieren während der Implementierung.
