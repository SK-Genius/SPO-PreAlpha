# Gemessene Ergebnisse

2026-10-29T08:41:44.1802613+00:00; .NET 11.0.0-rc.3.26522.148; X64; Release.

Korrektheit: 87 Initialisierungs-/Zugfälle plus Abfragen für beide Varianten.
Sieben Stichproben; Zeitangaben pro Aufruf in Mikrosekunden, Speicher in zugewiesenen Bytes.

| Operation | Variante | Wiederholungen je Stichprobe | Median µs | Min–Max µs | Median Bytes |
| --- | --- | ---: | ---: | ---: | ---: |
| Startup/check | tuple | 1 | 286225.500 | 251393.100–321617.800 | 670147680 |
| Startup/init | tuple | 1 | 112654.300 | 107898.400–167236.900 | 670172256 |
| Startup/check | list | 1 | 1066045.600 | 1037470.100–1427559.500 | 4148978264 |
| Startup/init | list | 1 | 2133377.500 | 1960441.500–2211029.700 | 10946428472 |
| NewGame/1 | tuple | 64 | 1710.405 | 1560.889–1906.772 | 8466792 |
| NewGame/1 | list | 16 | 9845.881 | 9760.869–10269.525 | 45952792 |
| NewGame/13 | tuple | 64 | 1832.777 | 1824.250–1898.133 | 9794376 |
| NewGame/13 | list | 16 | 10322.506 | 10109.706–11494.400 | 47294576 |
| TileAt/first | tuple | 8192 | 14.596 | 14.302–16.742 | 80912 |
| TileAt/first | list | 1024 | 180.010 | 173.924–192.190 | 897296 |
| TileAt/last | tuple | 8192 | 22.689 | 21.716–23.952 | 124448 |
| TileAt/last | list | 512 | 306.873 | 298.386–329.020 | 1550688 |
| TileAt/outside | tuple | 8192 | 18.577 | 18.504–19.068 | 104264 |
| TileAt/outside | list | 512 | 270.879 | 244.152–279.390 | 1268488 |
| Largest/dense | tuple | 256 | 465.596 | 456.422–527.098 | 2539328 |
| Largest/dense | list | 64 | 1920.353 | 1876.195–2086.744 | 9053768 |
| Largest/sparse | tuple | 256 | 468.973 | 457.940–532.225 | 2557720 |
| Largest/sparse | list | 64 | 1904.772 | 1891.705–2140.742 | 9072160 |
| CanMove/sparse | tuple | 512 | 320.103 | 310.342–346.685 | 1746320 |
| CanMove/sparse | list | 128 | 920.152 | 899.646–941.770 | 4412968 |
| CanMove/blocked | tuple | 128 | 1292.186 | 1179.630–1350.592 | 6324936 |
| CanMove/blocked | list | 16 | 7760.950 | 7573.975–8663.956 | 37220240 |
| CanMove/horizontal | tuple | 2048 | 77.380 | 75.586–84.071 | 431112 |
| CanMove/horizontal | list | 32 | 3319.203 | 3227.137–3730.056 | 15842336 |
| CanMove/vertical | tuple | 2048 | 81.780 | 78.159–84.464 | 431112 |
| CanMove/vertical | list | 16 | 7689.369 | 7380.762–8137.344 | 37147952 |
| CanMove/last-free | tuple | 128 | 1117.052 | 1093.304–1214.941 | 6232048 |
| CanMove/last-free | list | 128 | 889.159 | 876.074–984.037 | 4412968 |
| Move/mixed-left | tuple | 64 | 1425.098 | 1414.197–1661.098 | 7743920 |
| Move/mixed-left | list | 16 | 6724.794 | 6592.419–7091.325 | 33475920 |
| Move/mixed-right | tuple | 128 | 1352.422 | 1334.780–1454.369 | 7227592 |
| Move/mixed-right | list | 16 | 7580.344 | 7485.744–7721.144 | 37498800 |
| Move/mixed-up | tuple | 64 | 1799.145 | 1625.605–1913.073 | 9063880 |
| Move/mixed-up | list | 16 | 12009.688 | 11802.850–12321.750 | 59455416 |
| Move/mixed-down | tuple | 128 | 967.058 | 959.728–1069.904 | 5204248 |
| Move/mixed-down | list | 8 | 12716.763 | 12675.612–14208.862 | 63080384 |
| Move/packed-noop | tuple | 128 | 1011.120 | 967.168–1109.684 | 5417880 |
| Move/packed-noop | list | 64 | 3008.430 | 2974.498–3119.747 | 14833952 |
| Move/blocked-noop | tuple | 128 | 1074.441 | 1070.116–1104.207 | 5872944 |
| Move/blocked-noop | list | 32 | 4498.309 | 4185.181–4623.262 | 20958728 |
| Move/last-cell | tuple | 128 | 1080.446 | 1060.132–1196.503 | 5704104 |
| Move/last-cell | list | 16 | 7684.663 | 7622.887–7863.863 | 37069080 |
