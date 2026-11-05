# Wspólny gracz i kamera

Wszystkie sceny rozgrywki używają prefabu `Assets/Prefabs/Gameplay/GameplayRig.prefab`. Zawiera on istniejący `Assets/Prefab/Player.prefab`, kamerę główną z Cinemachine Brain, kamerę Cinemachine z Position Composer oraz istniejący `Assets/UI/Prefabs/HUDCanvas.prefab`. Konfiguracja postaci i kamery pochodzi z `Level_0` na gałęzi `krystian/rule-tile`, scalonej już do main. Gracz, animacje, ataki, zdrowie i materiały korzystają z istniejących assetów.

Wspólne ustawienia odpowiadają graczowi z tej sceny: prędkość 3, siła skoku 8, dwa skoki, dash o mnożniku 4 i czasie 0,1 sekundy. Kamera jest ortograficzna, ma rozmiar 5 i śledzi gracza przez Cinemachine Position Composer. Zmiany wspólnego prefabu obowiązują we wszystkich scenach; pozycji startowej i powiązań z lokalnym HUD nie trzeba ujednolicać.

| Czynność | Sterowanie |
| --- | --- |
| Ruch | A / D lub strzałki w lewo / w prawo |
| Skok i drugi skok | Spacja, W lub strzałka w górę |
| Dash | Dwukrotne naciśnięcie A / D lub odpowiedniej strzałki |
| Atak | Lewy przycisk myszy |

Sceny 01–03 zostały przeskalowane z 0,32 do 1 jednostki świata na komórkę, tak jak poziomy Krystiana. Układ kafelków, dekoracji i przeszkód jest zachowany. `Environment`, `DynamicObstacles` oraz znaczniki skalują się razem. Prefab gracza zachowuje tę samą wielkość i parametry na każdej mapie. Nowe sceny generowane narzędziami map również otrzymują wspólnego gracza i kamerę.

Po zmianie skali instalator przebudowuje `TilemapCollider2D` i `CompositeCollider2D`. Samo odświeżenie kafelków pozostawiało w zapisanych scenach ścieżki kolizji w starej skali, powodując przenikanie podłoża i niewidzialne ściany. Teren używa pełnych obszarów `Polygons`; puste komórki i przejścia zachowują otwory. Platformy z istniejącymi skryptami ruchu mają aktywny `Kinematic Rigidbody2D`. Instalator ponownie dopasowuje start gracza do rzeczywistego podłoża.

`Tools > Gameplay > Rebuild Level Collisions` naprawia zapisane kolizje wszystkich scen rozgrywki bez malowania map od nowa. Przed użyciem należy zapisać otwarte sceny i wyjść z Play Mode. Narzędzie zachowuje materiały, filtry warstw, ustawienia kontaktów i powiązania skryptów z colliderami. Usuwa też geometrię pozostałą w nieużywanych colliderach złożonych. Raport zgodności kafelków z kolizjami znajduje się w `Library/CollisionVerification/after.json`.

`PlayerSpawn` określa miejsce startu. Instalator ustawia postać nad najbliższym podłożem poniżej znacznika. W starych scenach wykorzystuje położenie poprzedniego gracza. Przypina wspólny HUD do postaci i zachowuje istniejący ekran śmierci. Dodaje światło globalne 2D tam, gdzie potrzebuje go materiał postaci.

`PlayerMovement` rozpoznaje podparcie po kierunku kontaktu pod stopami i uwzględnia pionowy ruch platformy. Kontakt z bokiem ściany lub sufitem nie odnawia skoków ani nie ustawia stanu stania. Kontakty są śledzone osobno, więc opuszczenie jednej powierzchni nie usuwa podparcia przez drugą. Skok czyści poprzednie podparcie. Ustawianie startu odrzuca trafienia promienia rozpoczynającego się wewnątrz ziemi i szuka jej rzeczywistej górnej powierzchni.

HUD używa istniejącego wyglądu paska zdrowia, ikony monety i tekstu licznika. `HUDManager` jest połączony z `PlayerHealth` wspólnej postaci oraz jedną aktywną `GameSession` w scenie. Pasek zmienia się przy obrażeniach, regeneracji i śmierci. Licznik reaguje na `StatsChanged`, emitowane przez istniejący system monet. Sesja zachowuje dotychczasową zasadę zerowania monet i wyniku na początku poziomu. Starsze paski gracza są wyłączone, a dotychczasowy ekran śmierci pozostaje podłączony. Powiązania są zapisane w scenach; `GameplayRig` potrafi też związać HUD i utworzyć sesję, gdy zostanie samodzielnie dodany do nowej sceny.

`Tools > Gameplay > Install Shared Player In All Levels` instaluje zestaw w scenach pod `Assets/Scenes` poza katalogiem UI oraz w dodatkowych scenach rozgrywki włączonych w Build Settings. Dlatego obejmuje także `Assets/_Recovery/Final.unity`, która obecnie znajduje się na tej liście. Sceny menu i końca gry pozostają ekranami UI. Przed zmianami instalator tworzy kopie scen w `Library/GameplayBackups`. Ponowne uruchomienie nie dodaje kolejnego gracza ani kolejnej kamery.

`GameplayVerification.VerifyAll`, uruchomiona w batchmode bez `-quit`, sprawdza zapisane sceny w Play Mode. Kontroluje jednego gracza, kamerę i AudioListener, parametry wspólnego prefabu, brak brakujących skryptów, lądowanie, ruch, podwójny skok, dash oraz śledzenie kamerą. Sprawdza też pojedynczy HUD i pasek gracza, spadek zdrowia po obrażeniach, regenerację, podniesienie rzeczywistego prefabu monety, odświeżenie HUD po ponownym włączeniu i wyzerowanie paska po śmierci. Próby ruchu wywołują funkcje istniejącego kontrolera z kontrolowanymi wartościami wejścia; nie symulują fizycznej klawiatury. Raport zapisuje do `Library/GameplayVerification/report.json`, a podglądy do `Library/MapPreviews/Gameplay-*.png` i `HUD-*.png`. Nie jest to automatyczne przejście całych map ani test walki z bossem.

Przed wejściem w Play Mode weryfikacja sprawdza również zgodność kolizji w całej mapie. `LevelCollisionVerification.VerifyAll`, uruchamiana w batchmode z `-quit`, dodatkowo bada górne powierzchnie podłoża i platform oraz symuluje lądowania ciał o rozmiarze, warstwie, materiale i ustawieniach fizyki wspólnego gracza. Pomija miejsca bez przestrzeni na postać i oddziela próby, aby nie stawały na sobie. Sprawdza zapisane sceny bez naprawiania ich w trakcie testu. Raport zapisuje do `Library/CollisionVerification/landings.json`.

`GameplayVerification.VerifyWithCollisions`, uruchomiona w batchmode bez `-quit`, wykonuje oba zestawy kontroli. Sprawdza też brak zagłębienia postaci w terenie przed uruchomieniem gry. Próba skoku mierzy miejsce nad głową; w niskich korytarzach oczekuje wzniosu ograniczonego wysokością sufitu. Podwójny skok nadal musi zużyć dokładnie dwa skoki, także po uderzeniu w sufit.
