# Level 02 — układ z GDD

Scena: `Assets/Scenes/Scene02/Level02.unity`.

Układ opiera się na rysunku w sekcji **Level II** załączonego GDD (rysunek opisany „3. Dash!”). Zachowuje dolny dziedziniec ze startem po lewej, galerię powrotną, odcinek nad wodą, zachodnie platformy, piętra po prawej, górny punkt ze skarbem oraz dolne prawe miejsce oznaczone w szkicu jako B. Rysunek nie ma skali; szerokości pomostów, wysokości przesiadek i zakresy ruchu wind dostosowano do istniejącego skoku. Dodano krótkie połączenia i stopnie zejścia przy prawej krawędzi.

## Edycja terenu

W `Window > 2D > Tile Palette` wybierz istniejącą paletę `IslandTerrainPalette`. Maluj ziemię na `Ground`, a cienkie drewniane pomosty na `Platforms`. Reguły dobierają krawędzie. Obie warstwy mają Tilemap Collider 2D, Composite Collider 2D, Static Rigidbody2D i tag Ground. `Background`, `Decorations` i `Foreground` rozdzielają tło i ozdoby bez kolizji.

Jedna komórka ma 0,32 jednostki świata. `Environment` i `DynamicObstacles` zapewniają tę skalę; dzieci powinny zachować lokalną skalę 1.

## Przeszkody

- Jedenaście instancji `MovingPlatform` obsługuje przejazdy i windy. `Local Travel` określa wektor trasy w komórkach, `Speed` prędkość w jednostkach świata na sekundę, a `Endpoint Pause` czas postoju. Po zaznaczeniu platformy widać jej trasę. Przewożenie nie zmienia rodzica postaci.
- Dwa istniejące prefaby `CollapsingPlatform` leżą nad wodą. Reagują na wejście od góry i zapadają się z opóźnieniem.
- Dwa istniejące totemy pilnują galerii i wschodniego piętra.
- `SpikeStrip` i `HazardWater` mają wyzwalacze obrażeń. Domyślnie zadają 20 obrażeń z odstępem jednej sekundy. To ustawienia do wyważenia podczas testu postacią. Woda nie wprowadza pływania ani automatycznej teleportacji.

Nowe prefaby są w `Assets/Prefabs/Level02`. Grafiki kolców, wody, nieba i łańcucha są kopiami istniejących plików Treasure Hunters, zaimportowanymi w `Assets/Maps/Level02Art`. Scena, prefaby i grafiki pierwszego poziomu nie są przebudowywane przy tworzeniu Level02.

## Integracja rozgrywki

Tak jak Level01, scena nie zawiera jeszcze gracza ani kamery śledzącej. `LevelMarkers` zawiera `PlayerSpawn`, `LevelExit`, `MeleeTutorial`, `BossEncounter`, `UpperLookout` oraz punkty dla przeciwników. Są to miejsca integracji, nie działający tutorial, przeciwnicy ani boss. Skrzynie są punktami orientacyjnymi. System monet, kluczy, przejścia poziomu i blokowania wyjścia pozostaje do podłączenia.

## Generator i sprawdzanie

`Tools > Maps > Rebuild Level 02` odtwarza układ ze źródła `Level02Layout`. Przed nadpisaniem istniejącej sceny zapisuje jej kopię i kopie prefabów Level02 w `Library/MapBackups`. Nie uruchamiaj przebudowy po ręcznym malowaniu, jeśli chcesz zachować własne zmiany. Otwarcie Unity nie przebudowuje sceny automatycznie.

`Tools > Maps > Validate Level 02` sprawdza strukturę i kolizje. `Level02Bootstrapper.BuildAndVerify` uruchamiana w batchmode bez `-quit` tworzy scenę, otwiera ją w Play Mode, sprawdza krawędzie grafik, kolizje, prześwity tras wind, przewożenie ciał oraz lądowanie i spadanie. Zapisuje podglądy `Level02-*` w `Library/MapPreviews` i zamyka proces Unity. To nie zastępuje przejścia całej mapy postacią i wyważenia poziomu trudności.
