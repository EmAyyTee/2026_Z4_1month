# Level 01 — wyspa

Scena: `Assets/Scenes/Scene01/Level01.unity`.

Otwórz `Window > 2D > Tile Palette` i wybierz `IslandTerrainPalette`. Ziemię maluj regułą `IslandGround` na `Ground`, a pomosty regułą `WoodPlatform` na `Platforms`. Krawędzie i zakończenia dobierają się automatycznie. Jedna komórka zajmuje 1 jednostkę świata, zgodnie ze skalą poziomów Krystiana.

`Ground` ma pełne kolizje komórek. `Platforms` używa cienkich kształtów odpowiadających deskom. Obie warstwy łączą kolizje przez Composite Collider 2D i mają tag Ground wymagany przez PlayerMovement. Dekoracje oraz tło nie mają kolizji.

Warstwy `Background`, `Decorations` i `Foreground` rozdzielają tło, ozdoby przy terenie i elementy przed postacią. Większe grafiki, takie jak palmy, są obiektami SpriteRenderer pod odpowiednią warstwą. Można je przesuwać niezależnie od kafelków.

Ziemia, drewniane pomosty, palmy, tło, beczki i totemy korzystają z istniejących grafik Treasure Hunters. Pliki w `Assets/Maps/IslandArt` to kopie z ustawieniami importu dla tej mapy: dokładne wycinki, 32 PPU, Point i bez kompresji. Oryginalne arkusze i ich ustawienia nie są zmieniane.

`DynamicObstacles` zawiera beczkę na pochylni, spawner aktywowany blisko gracza, dwa zapadające segmenty mostu i dwa totemy. Beczka ma prędkość początkową. Most reaguje na wejście od góry i zmienia kolor przed upadkiem. Flagi oraz skrzynia są punktami orientacyjnymi; nie dodają systemu kończenia poziomu ani ekwipunku.

Kolejne prefaby przeszkód umieszczaj jako dzieci `DynamicObstacles` z lokalną skalą 1. Rodzic ma skalę 1 zgodną z terenem. Pociski i nowe beczki dziedziczą skalę emitera.

`Tools > Maps > Rebuild Level 01` odtwarza zaprojektowany układ. Nadpisuje scenę i prefaby przeszkód; najpierw zapisuje ich kopie w `Library/MapBackups`. Zmiana kodu ani otwarcie Unity nie przebudowują mapy automatycznie. Po ręcznym malowaniu zapisuj scenę i nie uruchamiaj przebudowy, jeśli chcesz zachować swój układ.

Generator jest podzielony na import i paletę (`IslandArtBuilder`), układ i dekoracje (`IslandSceneBuilder`), prefaby (`IslandObstacleBuilder`) oraz weryfikację (`IslandMapVerification`). `MapBootstrapper` uruchamia te kroki.

`Tools > Maps > Validate Level 01` kontroluje warstwy i komponenty. Metoda `MapBootstrapper.BuildAndVerify`, przeznaczona do uruchomienia Unity w trybie batchmode bez argumentu `-quit`, otwiera zapisaną scenę w Play Mode i sprawdza dobór sprite'ów oraz rzeczywiste kształty kolizji. Sprawdza też lądowanie ciał na ziemi i pomoście oraz spadanie w przepaść. Zapisuje cztery podglądy w `Library/MapPreviews` i kończy proces Unity. Testy te nie zastępują przejścia poziomu postacią.

Scena zawiera wspólny `GameplayRig` z graczem i kamerą Cinemachine. `PlayerSpawn` wskazuje miejsce startu, a `LevelExit` punkt do podłączenia przejścia poziomu. Sterowanie i konfigurację opisuje `GameplaySetup.md`.
