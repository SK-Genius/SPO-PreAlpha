# Lesbare APIs in SPO

## Quellen und Übertragung

Qt bewertet eine API unter anderem nach Umfang, eindeutiger Bedeutung, Konsistenz und lesbarem Anwendungscode.
Besonders hilfreich sind die Abschnitte über statischen Polymorphismus, Benennung, Boolean-Parameter und die Prüfung realer Anwendungsfälle.
Hieraus übernehmen wir die Entwurfsprinzipien;
SPO braucht dafür keine Klassenhierarchien.
[Qt API Design Principles](https://wiki.qt.io/API_Design_Principles)

Swift fordert Klarheit an der Aufrufstelle und empfiehlt, Namen in ihrem Verwendungskontext zu prüfen.
Rollen sind aussagekräftiger als Typnamen;
unveränderliche Ergebnisse und verändernde Operationen sollen unterscheidbar sein.
In SPO übernehmen Infix-Namen und die getrennte Funktions-/Methodensyntax diese Aufgabe.
[Swift API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)

Explizite Bedeutung, einfache Strukturen und lesbarer Kontrollfluss ergänzen diese Regeln.
[PEP 20](https://peps.python.org/pep-0020/)

## Regeln

1. **Den Aufruf zuerst schreiben.**
   Er soll sich mit Empfänger, Argumenten und Ergebnis als verständlicher Satz lesen.
   Kürze ist kein eigenes Qualitätsziel.
2. **Funktionen liefern Werte.**
   `Style .WithForeground Color` liefert einen neuen Style;
   `State .After Action` einen neuen Spielzustand.
   Bestehende Werte bleiben erhalten.
   Methoden mit `§VAR`-Empfänger machen externe Effekte sichtbar.
3. **Beziehungen aussprechen.**
   `Draw Text At Position WithStyle Style` nennt die Rolle jedes Arguments.
   `Body .FollowedBy Head` hängt ein Element an;
   `Body .TrySplitLast` liefert optional `(Remaining, Last)`.
4. **Records nutzen.**
   `Modifiers: {Control: §TRUE}` benötigt keine Bitmasken, Wrapper-Klassen oder Getter.
   Ein Pattern erwähnt nur die benötigten Felder.
5. **Ein Koordinatensystem pro API.**
   Terminalpositionen beginnen bei `(0, 0)` im sichtbaren Fenster.
   Bufferposition und native Codes gehören ins Backend.
6. **Zahlen erklären.**
   Konfiguration und Layout haben Namen oder benannte Felder.
   Maße werden aus Inhalt und Spielfeld abgeleitet.
   `0` als Ursprung, `1` als einzelner Schritt und `10` beim Dezimalsystem benötigen keine Alias-Konstanten.
   Native Codetabellen stehen unmittelbar bei den benannten Feldern/Tasten.
7. **Abstraktionen müssen Arbeit sparen.**
   Einmal benötigte Rechnungen bleiben am Einsatzort.
   Eine lokale Funktion ist sinnvoll für Rekursion oder einen längeren, in sich verständlichen Schritt.
   Weiterreichende Wrapper entfallen.
8. **Zustand begründen.**
   Score und Fenstermaße werden berechnet.
   MoveDirection und PendingDirection bleiben getrennt: mehrere Eingaben zwischen zwei Ticks dürfen keine Kehrtwende ermöglichen.
   Gehaltene Tasten und vorherige Maustasten sind im Backend notwendig, um Wiederholungen und Flanken zu erkennen.
9. **Unabhängige Operationen.**
   Zeichnen verändert die Cursorposition nicht.
   Farbe, Sichtbarkeit, Position und Darstellung haben keine versteckten Kopplungen.
10. **Typgrenzen begründen.**
    Für jeden abstrakten Typ benennen, welches Modul ihn bereitstellt und welcher Verbraucher seine Darstellung nicht kennen soll.
    Gemeinsame App-Daten erhalten eine konkrete Typdefinition.
    Bei Generik prüfen, ob eine Operation generisch ist oder nur ihr Vertrag einen Typparameter erhält;
    eine ausgelagerte Signatur rechtfertigt allein keine zusätzliche Typfunktion.
11. **Typen aus dem Kontext ableiten.**
    Lokale Werte und Match-Bindungen bekommen keine Typangabe, wenn Ausdruck oder Pattern sie bereits festlegen.
    In der Implementierung genügen die benötigten Parametertypen;
    die vollständige Funktionssignatur wird nicht zusätzlich davor wiederholt.
    Generische Parameter benötigen vor `<=>` kein zusätzliches `€ §TYPE`.
    Nicht mehr benötigte Typ-Aliase entfallen.
    Eine Angabe bleibt, wenn sie eine notwendige Typgrenze setzt, etwa für ein noch unbestimmtes rekursives Ergebnis.
12. **Wertabhängigkeiten am Namen erkennen.**
    Feste Datenwerte tragen `c`:
    Literale,
    importierte Modulwerte und Ergebnisse reiner Berechnungen mit ausschließlich konstanten Eingaben,
    einschließlich verwendeter Werte aus äußeren Gültigkeitsbereichen.
    Das gilt auch für lokale Definitionen innerhalb einer Funktion.
    Argumente tragen `a`.
    Von ihnen direkt, indirekt oder durch Fallauswahl abhängige lokale Werte erhalten kein `c`.
    Methoden lesen veränderliche Ressourcen;
    ihre Ergebnisse sind ebenfalls keine Konstanten.
    Feldnamen beschreiben den Vertrag, Präfixe die lokale Bindung: `DefaultStyle: §DEF cDefaultStyle`.
    Funktionen behalten ihre Aufrufnamen.
13. **Vektoroperatoren beim Import markieren.**
    `...+...` aus Vector2 wird als `..._+_...` gebunden, `...==...` als `..._==_...`.
    Entsprechend liest sich die Bewegung als `HeadPosition ._+_ Step` und ein Positionsvergleich als `NextHead ._==_ FoodPosition`.

```spo
§DEF cFoodStyle = cDefaultStyle .WithForeground #Red §> .WithBlink §TRUE
§DEF NextState = State .After Action
aTerminal : HideCursor, Draw "*" At FoodPosition WithStyle cFoodStyle .
```

## Review mit Refactoring-Schleife

Für jede geänderte API zuerst zwei plausible Aufrufvarianten vergleichen.
Danach jede handgeschriebene Zeile der betroffenen Anwendung, Bibliothek und Runtime einschließlich Signaturen lesen.
Generiertes IL wird durch Compiler und Laufzeit geprüft;
es ist kein Entwurfsmaßstab für die SPO-Oberfläche.

Pro Zeile beziehungsweise zusammengehörigem Ausdruck diese Fragen beantworten:

| Kriterium | Prüffrage |
| --- | --- |
| Bedeutung | Sind Eingaben, Ergebnis und Zweck ohne Sprung zur Implementierung erkennbar? |
| Effekte | Ist sichtbar, welcher Wert neu entsteht oder welche Ressource verändert wird? |
| Einheiten | Sind Zahlen, Achsen, Indizes, Farben und Zeitangaben eindeutig? |
| Nähe | Stehen zusammengehörige Rechnungen zusammen, ohne unnötige Zwischenstation? |
| Ablauf | Sind Fälle, Abbruch und Zustandsübergänge klar? Werden rekursive Aufrufe nur im gewählten Fall ausgeführt? |
| Typgrenzen | Ist jede abstrakte Bindung begründet und jede Generik an dieser Stelle nötig? |
| Typangaben | Liefert jede Annotation zusätzliche Information, die sich hier nicht ableiten lässt? |
| Wertabhängigkeiten | Tragen feste Datenwerte `c`, Argumente `a` und argumentabhängige lokale Werte kein `c`? |

Bewertung: **2 = direkt verständlich**, **1 = Erklärung/Nachschlagen nötig**, **0 = mehrdeutig oder irreführend**.
Nicht anwendbare Kriterien werden ausgelassen.
Ein Wert unter 2 löst Refactoring und erneute Prüfung aus.
Es gibt keinen Durchschnittswert, der einen schlechten Aufruf ausgleichen darf.
Eine unvermeidbare Sprach- oder Protokollgrenze wird mit konkretem Beispiel dokumentiert.

Anschließend:

```powershell
./SPO/Tests/Readability.ps1
dotnet run --file SPO/Tests/Runtime.Tests.cs
dotnet run --file SPO/src/Program.cs -- --check
dotnet run --file SPO/Tests/WindowsTerminal.Tests.cs
```

Der letzte Test benötigt eine Windows-Konsole.
Die mechanische Prüfung umfasst alle Apps, handgeschriebene Bibliotheksquellen/Signaturen, Terminal- und GUI-Runtime sowie die GUI-Tests.
Sie stoppt bei Zeilen über 120 Spalten und bekannten API-Rückfällen.
Sie misst keine menschliche Verständlichkeit.
Nach Fehlern oder weiteren Änderungen werden die betroffenen Prüfungen erneut ausgeführt.