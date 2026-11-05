# Level 03 — układ z GDD

Scena: `Assets/Scenes/Scene03/Level03.unity`.

Mapa odtwarza główne obszary szkicu Level III: dolny korytarz ze startem po lewej (1), zygzak platform po prawej (2), prawą komnatę ze skrzynią (3), pochyłe wybrzeże (4), zachodni filar (5), jamę z kolcami (6), środkową galerię powrotną (7), korytarz pod skałami (8), górną galerię (9) oraz płaską arenę bossa (10). `LevelMarkers/GDDSections` pozwala znaleźć te miejsca w Hierarchy.

Rysunek nie ma skali. Odległości dopasowano do ruchu postaci w projekcie: prędkości 3, skoku 3 i dwóch skoków przy standardowej grawitacji. Duże różnice wysokości obsługują windy. Przy dolnym podejściu najpierw należy wejść na `LowerHallFerry`, a następnie przeskoczyć na drewniane pomosty. Trasa zawiera powroty, boczne skrzynie i dwa wejścia na arenę. Do pełnego wyważenia potrzebne jest przejście mapy docelową postacią.

## Warstwy i edycja

`Environment/Grid` zawiera `Ground`, `Platforms`, `Background`, `Decorations` i `Foreground`. Ziemia ma trawiastą powierzchnię i skalne wypełnienie, a pomosty osobne drewniane grafiki. Ziemię i pomosty można malować istniejącą paletą `IslandTerrainPalette` w `Window > 2D > Tile Palette`. Reguły automatycznie dobierają krawędzie.

`Ground` i `Platforms` mają Tilemap Collider 2D, Composite Collider 2D, Static Rigidbody2D oraz tag Ground. Pozostałe warstwy służą grafice. Jedna komórka odpowiada 0,32 jednostki świata; skala jest ustawiona na korzeniach `Environment` i `DynamicObstacles`.

## Przeszkody

- Osiem instancji `MovingPlatform` zapewnia przejazd i windy. `Local Travel` podaje trasę w lokalnych komórkach, `Speed` prędkość w jednostkach świata na sekundę. Łańcuchy pokazują pionowe trasy.
- Cztery `CollapsingPlatform` są umieszczone na prawym podejściu i nad jamą z kolcami. Istniejący skrypt reaguje na wejście gracza od góry.
- Cztery `ShooterTotem` pilnują środkowej i górnej galerii.
- Dwa `SpikeStrip` wyznaczają obszary obrażeń. Używają istniejącego `ContactDamage2D`.
- Nowy prefab `Assets/Prefabs/Level03/BarrelRampTrap.prefab` łączy pochyły drewniany pomost, istniejącą beczkę i `BarrelSpawner`. Pierwsza beczka rusza wraz ze sceną. Następne pojawiają się co 4 sekundy, gdy żywy gracz jest w zasięgu 4 jednostek świata.

Grafiki, materiały i pozostałe prefaby są współdzielone z poprzednimi mapami. Generator Level03 ich nie przebudowuje. Nie dodano nowych ani wygenerowanych grafik.

## Integracja rozgrywki

Tak jak w poprzednich mapach, scena zawiera mapę i statyczną kamerę, bez gracza ani kamery śledzącej. `PlayerSpawn`, `RangedTutorial`, `EnemySpawnMarkers`, `BossEncounter`, `BarricadeLower`, `BarricadeUpper`, `SecretReward` i `LevelExit` są punktami do podłączenia systemów gry. Skrzynie pełnią funkcję dekoracyjną. Nie dodano przeciwników, bossa, tutorialu strzelania, nagród ani blokad przejścia.

W dostępnych materiałach nie ma grafiki portalu. Jego położenie wskazują `ExitPortal` oraz istniejąca flaga `PortalLocationFlag` na prawym końcu areny. Portal i przejście do kolejnej sceny wymagają osobnego podłączenia.

## Generator i sprawdzanie

`Tools > Maps > Rebuild Level 03` odtwarza mapę z `Level03Layout`. Przed nadpisaniem zapisuje kopię sceny i prefabów Level03 w `Library/MapBackups`. Przebudowa zastępuje ręczne zmiany tej sceny; nie jest uruchamiana przy otwarciu Unity. Poziomy 01 i 02 pozostają bez zmian.

`Tools > Maps > Validate Level 03` sprawdza warstwy, kolizje i komplet przeszkód. `Level03Bootstrapper.BuildAndVerify` uruchamiana w batchmode bez `-quit` tworzy scenę i wykonuje kontrole w Play Mode: krawędzie terenu, wolne korytarze, podłogę areny, prześwity tras wind, wyzwalacze kolców, przewożenie ciał, toczenie beczki oraz lądowanie i spadanie. Zapisuje podglądy `Level03-*` w `Library/MapPreviews` i zamyka proces Unity. Te kontrole nie zastępują przejścia całej mapy postacią ani testów obrażeń i walki.
