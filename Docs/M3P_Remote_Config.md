# M3P — Remote Config i synchronizacja z edytorem Unity

**Status:** specyfikacja do implementacji. Dokument opisuje planowane działanie; narzędzie nie jest jeszcze wdrożone.  
**Data:** 24.10.2026.  
**Nazwa narzędzia:** M3P Remote Config Sync.

**Ustalony sposób obsługi:** użytkownik zaznacza w narzędziu konkretne ScriptableObjecty, które chce synchronizować. Narzędzie ocenia ich zgodność i samodzielnie przygotowuje eksport oraz połączenie z istniejącym projektem Unity Cloud. Ręczna konfiguracja w Dashboardzie, wpisywanie ID, tworzenie kluczy i przygotowanie JSON nie są częścią zakładanego procesu użytkownika.

## 1. Cel

Configi nadal edytujemy w Inspectorze Unity. W oknie narzędzia zaznaczamy konkretne assety `ScriptableObject`. Narzędzie sprawdza, które z nich można obsłużyć, wykrywa różnice między ich wartościami lokalnymi a Unity Remote Config, pokazuje popup z porównaniem i pozwala wysłać wybrane wartości do chmury.

Główna ścieżka pracy:

1. Otwieram narzędzie i zaznaczam kompatybilne ScriptableObjecty z listy lub przeciągam je do okna.
2. Narzędzie wykorzystuje powiązany projekt Unity Cloud i sesję zalogowanego edytora. Pokazuje rozpoznany projekt oraz docelowe środowisko.
3. Dla każdego assetu widzę status obsługi, zakres eksportu i różnice. Brak zdalnego wpisu oznacza „Nowy — zostanie utworzony”.
4. Klikam **Wyślij wartości z edytora**. Narzędzie tworzy brakującą konfigurację i wpisy, publikuje wartości oraz potwierdza wynik odczytem.
5. Późniejsza zmiana lokalnego parametru, np. dodatkowych ataków kaskady z `1` na `2`, wywołuje popup z porównaniem.
6. Zaznaczam zmiany i publikuję je tym samym przyciskiem.
7. Gra z wdrożoną integracją i katalogiem tych assetów otrzymuje wartości przy następnym pobraniu konfiguracji.

Wykrywanie różnic jest automatyczne. Publikacja następuje wyłącznie po kliknięciu przycisku wysyłania. Zamknięcie popupu, zapis assetu i wejście w Play Mode nie publikują zmian.

## 2. Czym jest Remote Config

Unity Remote Config przechowuje ustawienia jako pary klucz–wartość, w tym wartości typu JSON. Po dodaniu obsługi w grze można zmieniać obsługiwane parametry balansu bez publikowania nowego builda. Osobne środowiska pozwalają utrzymywać różne ustawienia dla testów i produkcji. [Opis usługi Unity](https://docs.unity.com/en-us/remote-config), [typy ustawień i Game Overrides](https://docs.unity.com/en-us/remote-config/game-overrides-and-settings).

W M3P eksportujemy dane ze wskazanych `ScriptableObject` do własnego formatu JSON. Referencje do obiektów Unity wymagają jawnego mapowania na stabilne identyfikatory. Plik `.asset` nie jest formatem publikacji tej integracji.

Unity udostępnia również pliki `.rc` i publikację przez pakiet Deployment. Własne narzędzie M3P ma dodać powiązanie z naszymi configami, porównanie konkretnych pól oraz popup. [Pliki Remote Config](https://docs.unity.com/en-us/remote-config/remote-config-files), [integracja z edytorem](https://docs.unity.com/en-us/remote-config/write-configuration/unity-editor).

## 3. Obecny stan projektu

- Projekt korzysta z Unity `6000.3.5f2`.
- `GameConfig` zbiera referencje do configów walki, progresji, przeciwników, umiejętności, kart, kafelków i pozostałych systemów.
- `BattleConfig` oraz `LevelProgressionConfig` zawierają proste parametry, od których można rozpocząć integrację.
- Configi takie jak `EnemiesConfig`, `SkillConfig` i `CardConfig` są rejestrami. Właściwe parametry znajdują się również w powiązanych definicjach.
- `GameManager.Start()` przechodzi ze sceny Boot do Menu. Pobranie i przygotowanie konfiguracji trzeba włączyć przed pierwszym użyciem profilu i progresji.
- `ProfileManager` i `ProgressionService` zapamiętują przekazany `GameConfig`. Gotowa konfiguracja musi powstać przed utworzeniem tych usług.
- Rejestry buforują mapowania ID. Ich zmiana w runtime wymaga przebudowy odpowiednich mapowań.
- Inicjalizacja UGS istnieje już w `GameAnalytics`; integracja wymaga wspólnego punktu inicjalizacji i wyboru środowiska.
- W `Packages/manifest.json` nie ma obecnie pakietu Remote Config.
- Ustawienia projektu zawierają już identyfikator projektu Unity Cloud i organizacji. Narzędzie powinno je odczytać i zweryfikować dostęp, bez proszenia o ponowne utworzenie projektu.

### Automatyczne przygotowanie pierwszej publikacji

Wymaganiem narzędzia jest rozpoczęcie pracy od wyboru assetów, bez osobnego konfiguratora usług:

1. Odczytaj powiązanie projektu z ustawień Unity oraz stan logowania edytora.
2. Odczytaj dostępne środowiska. Użyj zapamiętanego środowiska narzędzia, następnie aktywnego środowiska edytora, a w razie jego braku środowiska domyślnego wskazanego przez usługę. Zawsze pokazuj cel przed publikacją; nie wybieraj przypadkowego pierwszego wpisu.
3. Odczytaj bazową konfigurację. Rozróżnij jej brak od błędu autoryzacji, sieci lub niejednoznacznej odpowiedzi.
4. Przygotuj lokalny manifest wyboru, identyfikatory assetów i dane eksportu automatycznie.
5. Dopiero po kliknięciu publikacji utwórz brakującą bazową konfigurację i własny klucz Remote Config, a następnie opublikuj wpisy zaznaczonych assetów. Operacja ma być powtarzalna bez tworzenia duplikatów.

Standardowy proces korzysta z istniejącego środowiska i nie wymaga tworzenia nowego. Jeśli projekt nie ma żadnego środowiska, narzędzie powinno umożliwić jego utworzenie we własnym oknie przez obsługiwany interfejs usług; utworzenie jest częścią jawnej publikacji i ma wskazaną nazwę celu. Po błędzie dostępu nie wolno samoczynnie przełączać projektu ani środowiska.

Pakiety i integracja są dostarczane w ramach wdrożenia narzędzia. Użytkownik nie musi tworzyć konta usługowego, wklejać tokenów, pisać JSON ani ręcznie zakładać kluczy na stronie. Poświadczenia pobieramy przez obsługiwany mechanizm sesji Unity Editor; nie odczytujemy plików z tokenami ani nie utrwalamy tokenów w projekcie. Unity dokumentuje tworzenie/powiązanie projektu oraz publikację konfiguracji bezpośrednio w edytorze. [Integracja z Unity Editor](https://docs.unity.com/en-us/remote-config/write-configuration/unity-editor).

**Warunek techniczny do potwierdzenia przy implementacji:** działające uwierzytelnienie odczytu środowisk nie dowodzi dostępu do każdego endpointu Remote Config. Pierwszy test integracji musi potwierdzić odczyt bazy, utworzenie brakującego klucza, publikację i weryfikację z użyciem sesji edytora, bez ręcznych kluczy administracyjnych. Taki przebieg jest wymaganiem tej specyfikacji, a nie potwierdzoną już funkcją projektu.

Zalogowanie do konta Unity i uprawnienia do projektu są konieczne. Jeżeli ich brakuje, narzędzie pokazuje konkretny stan i prowadzi do logowania w Unity lub uzyskania dostępu. Jeżeli usługa wymaga działania właściciela konta, zgody lub aktywacji niedostępnej przez obsługiwane API, należy ujawnić ten konkretny blokujący warunek; nie zastępować po cichu wymaganego procesu instrukcją ręcznej konfiguracji w Dashboardzie.

## 4. Zakres synchronizacji

Podstawą jest lista konkretnych assetów z checkboxami. Można wybrać wiele instancji tego samego typu, np. kilka configów przeciwników. `GameConfig` może pomóc znaleźć powiązane assety, ale nie jest obowiązkowym ani jedynym źródłem. Zakres synchronizacji wynika z wyboru kompatybilnych assetów przez użytkownika.

Lista powstaje z assetów `ScriptableObject` w projekcie i obsługuje wyszukiwanie, filtrowanie po typie, przeciąganie plików oraz dodawanie zaznaczenia z okna Project. Assety z pakietów, obiekty tymczasowe i subassety bez wdrożonej obsługi tożsamości mają czytelny status niedostępności. Użytkownik zaznacza tylko te obiekty, które chce objąć synchronizacją.

| Status assetu | Znaczenie i zachowanie |
| --- | --- |
| **Obsługiwany** | Wszystkie pola należące do kontraktu konfiguracji mają eksport, walidację i odczyt runtime; checkbox aktywny |
| **Częściowo obsługiwany** | Możliwa synchronizacja określonych pól danych; checkbox aktywny, zakres i pomijane pola widoczne przed wysłaniem |
| **Nieobsługiwany — powód** | Brak poprawnego eksportu lub zastosowania w grze; checkbox wysyłania nieaktywny |
| **Błąd danych** | Typ jest obsługiwany, ale bieżące wartości są niepoprawne; wysyłka assetu zablokowana do poprawy |

**Eksporter ogólny** obsługuje jawnie określony zestaw serializowanych danych: liczby, bool, string, enum, tablice/listy oraz zagnieżdżone serializowalne struktury bez referencji Unity i polimorfizmu. Poszczególne typy Unity, np. `Color` i wektory, wymagają zdefiniowanego kodowania. System pomija pola techniczne i nieserializowane oraz pokazuje rzeczywistą listę eksportowanych pól. Nie opiera kwalifikacji tylko na tym, że `JsonUtility` potrafi zwrócić tekst.

**Adapter typu** obsługuje specyficzne zależności, ograniczenia i walidację, np. osiem bonusów supermatcha albo odwołania do definicji po ID. Jest dostarczany z narzędziem; użytkownik wybierający asset nie musi go pisać. Dodanie nieznanej mechaniki, polimorficznych efektów, słowników lub grafów z cyklami może wymagać nowego adaptera.

Pola z referencjami do prefabów, ikon, audio i innych `UnityEngine.Object` pozostają lokalne, dopóki adapter nie zapewnia mapowania. Częściowy eksport jest dozwolony wyłącznie wtedy, gdy pominięcie tych pól zachowuje spójność danych i istnieje poprawny sposób zastosowania pozostałych pól. Referencje do innych ScriptableObjectów nie powodują samoczynnego eksportu wszystkich zależności. Akcja **Pokaż powiązane assety** pozwala zaznaczyć je osobno.

Przykłady kwalifikacji do sprawdzenia podczas implementacji:

| Config / dane | Planowana obsługa |
| --- | --- |
| `BattleConfig`, `LevelProgressionConfig` | Pełne dane liczbowe i tablice, z walidacją reguł M3P |
| `StatProgressionConfig` | Obsługiwane parametry statystyk; referencje i złożone drzewa wymagają adaptera |
| `MatchRewardRules`, `ChestConfig` | Pola nagród; odłamki zależą od obsługi identyfikatorów kafelków |
| `EnemyDefinition`, `SkillDefinition`, definicje kart i ulepszeń | Parametry liczbowe; zależne efekty i referencje według adaptera |
| `EnemiesConfig`, `SkillConfig`, `CardConfig`, `GameConfig` | Wskazanie zależności; sama obecność rejestru nie oznacza eksportu parametrów powiązanych definicji |
| Prefaby, tekstury, modele i audio | Nie są danymi eksportowanymi przez ten system |

Zaznaczenie jest zapamiętywane pomiędzy sesjami i zmienia zakres obserwacji. Odznaczenie zatrzymuje obserwowanie i przyszłe publikacje assetu; nie usuwa jego opublikowanych danych ani mapowań potrzebnych istniejącym buildom. Usuwanie zdalne jest osobną, przyszłą funkcją. Komunikat „Zgodne” zawsze wskazuje zakres zaznaczonych assetów i ich eksportowanych pól.

Parametry zapisane jako stałe w kodzie, np. część mnożników w `EnemyProgressionResolver`, wymagają wcześniejszego przeniesienia do danych. Eksport istniejących configów nie udostępni ich automatycznie w Remote Config.

## 5. Format danych

Narzędzie automatycznie tworzy ustawienie typu **JSON** o nazwie `m3p_configs_v1`. Zawiera ono wpisy assetów z identyfikatorami nadanymi przez narzędzie. Użytkownik nie zakłada osobnych kluczy ani pól w Dashboardzie. Jest to własny format M3P wewnątrz wartości ustawienia Remote Config.

Jeden wspólny klucz pozwala zweryfikować zależny zestaw przed zastosowaniem. Obowiązuje kontrola rozmiaru według limitów używanego API; przekroczenie blokuje publikację z informacją o rozmiarze. Ewentualny podział na kilka kluczy wymaga osobnego protokołu spójnego wydania.

Przykład formatu, z ilustracyjnymi wartościami:

```json
{
  "schemaVersion": 1,
  "revision": 1,
  "objects": {
    "39f6c8ce4b814acb9f32a5591058b3e7": {
      "typeId": "m3p.battle",
      "typeSchemaVersion": 1,
      "data": {
        "damagePerMatchedTile": 1,
        "damagePerSupermatchSize": [0, 0, 0, 0, 0, 0, 0, 0],
        "additionalAttackPerCascade": 2
      }
    },
    "7f37a563458a4fdbabfdb9ba3ca8dd93": {
      "typeId": "m3p.levelProgression",
      "typeSchemaVersion": 1,
      "data": {
        "levelCap": 20,
        "baseExperiencePerLevel": 100,
        "experienceIncreasePerLevel": 100,
        "statPointsPerLevel": 1
      }
    }
  }
}
```

- `schemaVersion` opisuje strukturę danych. Zmiana niezgodna ze starszym klientem wymaga nowej wersji kontraktu i planu publikacji dla starszych buildów.
- Kluczem każdego wpisu `objects` jest trwały `remoteId`, nadany assetowi automatycznie i zapisany w lokalnym manifeście. Zmiana nazwy lub przeniesienie assetu go nie zmienia; skopiowanie assetu tworzy nową tożsamość. Konflikt identyfikatorów blokuje publikację.
- `typeId` i `typeSchemaVersion` opisują kontrakt danych danego typu. Eksporter ogólny generuje kontrakt i mapowanie; adapter może dostarczyć własny. Zmiana schematu typu wymaga planu zgodności starszych buildów, np. zachowania starszego klucza transportowego do końca ich wsparcia.
- `revision` identyfikuje publikację M3P. Narzędzie nadaje kolejną wartość po odczycie bieżącej konfiguracji. Ten numer nie jest blokadą zapisu ani wersją transakcyjną API Unity.
- Diff i hash zawartości obejmują wersje kontraktu oraz dane; pomijają `revision` i ewentualne metadane publikacji. Sam czas eksportu nie może powodować różnicy.
- Obecność konkretnego configu wynika z wyboru użytkownika. Można opublikować tylko `BattleConfig` albo dowolną inną kompatybilną instancję. Wewnątrz nowego wpisu wymagane są wszystkie pola jego kontraktu; istniejący wpis można aktualizować częściowo przez scalenie.
- Wpisy niezaznaczonych obiektów pozostają zachowane. Wybranie nowego assetu dodaje wpis; nie przebudowuje zdalnej listy wyłącznie na podstawie bieżących checkboxów.

Eksporter tworzy deterministyczny wynik: stałe nazwy pól, typy i format liczb niezależny od ustawień regionalnych. Nie eksportuje Unity `instanceID`, `fileID`, ścieżek lokalnych ani referencji obiektowych jako identyfikatorów gry.

Manifest edytora wiąże `remoteId` z trwałą tożsamością assetu w `AssetDatabase`. Generator tworzy osobny katalog runtime z prawdziwymi referencjami do obiektów i obsługą ich kontraktów. `remoteId` nie zastępuje ID kart, skilli ani kafelków zapisanych w profilach. Nazwy i ścieżki służą tylko do prezentacji w edytorze.

Manifest tożsamości i kontrakty przechowujemy w plikach wersjonowanych razem z projektem, poza `Library` i `UserSettings`. Nowy checkout, przebudowa katalogu lub inny komputer muszą odtworzyć te same `remoteId`. Usunięcie cache nie nadaje assetom nowych identyfikatorów; konflikty przy scalaniu manifestu wymagają rozstrzygnięcia przed publikacją.

## 6. Okno narzędzia i automatyczne sprawdzanie

Proponowana pozycja menu: **M3P → Remote Config → Synchronizacja**.

Okno zawiera:

- automatycznie odczytany projekt UGS i środowisko docelowe; szczegóły techniczne dostępne w rozwijanym panelu;
- listę ScriptableObjectów z checkboxami, wyszukiwaniem, filtrem typu, nazwą, statusem zgodności i zakresem pól;
- akcje **Dodaj zaznaczone w Project**, przeciąganie assetów i **Pokaż powiązane assety**;
- czas ostatniego udanego odczytu i status aktualności;
- przycisk **Sprawdź teraz**;
- tabelę różnic z możliwością zaznaczenia pól;
- przycisk **Wyślij wartości z edytora**;
- opcję **Automatycznie sprawdzaj różnice**, domyślnie włączoną dla zaznaczonych assetów po automatycznym rozpoznaniu połączenia.

Przykładowy wybór assetów, z ilustracyjnymi statusami:

```text
M3P Remote Config                 Projekt: M3p | Środowisko: production

Szukaj: [                         ]  [Dodaj zaznaczone w Project]

[✓] BattleConfig.asset           Obsługiwany             Nowy
[✓] LevelProgressionConfig.asset Obsługiwany             1 różnica
[ ] EnemyDefinition.asset        Częściowo obsługiwany   Zobacz pola
[—] CustomGraph.asset            Nieobsługiwany          Graf z cyklami

[Sprawdź teraz] [Wyślij wartości z edytora]
```

Nazwy środowisk w przykładach są ilustracyjne. Środowisko `production` nie jest wybierane na sztywno: narzędzie odczytuje rzeczywisty cel według reguł automatycznego przygotowania. Zaznaczenie assetu i otwarcie okna nie powodują żadnej publikacji.

Proponowane wyzwalacze i wartości początkowe:

| Zdarzenie | Działanie |
| --- | --- |
| Otwarcie projektu lub przeładowanie domeny | Automatyczne rozpoznanie połączenia i jedno odroczone sprawdzenie zaznaczonych assetów po zakończeniu kompilacji/importu |
| Zaznaczenie lub dodanie assetu | Ocena zgodności, nadanie trwałego ID w razie potrzeby i porównanie bez publikacji |
| Edycja obserwowanego configu, Undo/Redo, import lub zapis | Ponowny eksport po 2 sekundach bez kolejnej zmiany |
| Powrót do okna Unity | Odświeżenie remote, jeśli ostatni odczyt jest starszy niż 60 sekund |
| Otwarte okno synchronizacji | Odświeżenie remote co 60 sekund |
| Kliknięcie „Sprawdź teraz” | Pełny, bieżący odczyt i porównanie |
| Kliknięcie wysyłania | Obowiązkowe ponowne sprawdzenie źródeł i remote |

Zmiany dokonane w Dashboardzie zostaną wykryte przy najbliższym odświeżeniu. Pierwsza wersja nie zakłada powiadomień push z serwera. Automatyczny odczyt sieciowy jest ograniczany do jednego na 60 sekund; lokalne zmiany można wcześniej porównać z ostatnią kopią, oznaczając czas tego odczytu.

Żądania są asynchroniczne. W danej chwili działa najwyżej jedno sprawdzanie lub wysyłanie. Dostęp do assetów i UI odbywa się na głównym wątku Unity. Popup czeka na zakończenie importu, kompilacji i Play Mode. Przy zmianie projektu lub środowiska wynik poprzedniego żądania jest unieważniany.

## 7. Popup z różnicami

Popup jest oknem z przewijaną tabelą, które pozwala przejrzeć i zaznaczyć różnice. Nie ograniczamy go do komunikatu „OK/Cancel”.

Przykładowy wygląd:

```text
Wykryto różnice w konfiguracji

Projekt: M3P       Środowisko: development
Kierunek: Edytor → Remote Config
Zakres: 2 configi, 2 zmienione pola

[✓] Config                  Parametr                     Edytor   Remote
[✓] BattleConfig            AdditionalAttackPerCascade   2        1
[✓] LevelProgressionConfig  BaseExperiencePerLevel        120      100

Wybrane wartości zastąpią ustawienia w środowisku development.

[Wyślij wartości z edytora] [Później]
```

Zmiany możliwe do wysłania są domyślnie zaznaczone. Wiersze błędne mają opis przyczyny i blokują publikację zależnego zestawu. Kliknięcie configu wskazuje jego lokalny asset w Project/Inspector.

**Później** zamyka popup bez zmiany danych. Ten sam zestaw różnic nie otwiera go ponownie w tej samej sesji edytora, także po przeładowaniu domeny. Nowa zawartość lokalna, nowa zawartość remote lub zmiana środowiska może wywołać nowy popup. Stan wyciszenia zależy od projektu, środowiska, źródeł i hashy porównywanych danych. Ręczne otwarcie narzędzia zawsze pokazuje aktualny diff.

Przy niezapisanych zmianach źródłowych okno pokazuje tę informację, a przycisk brzmi **Zapisz configi i wyślij wartości z edytora**. Kliknięcie zapisuje wyłącznie obserwowane assety źródłowe potrzebne do eksportu, po czym ponawia walidację. Samo porównanie nie zapisuje assetów. Błąd zapisu zatrzymuje publikację.

Środowisko docelowe jest widoczne przez cały czas. Przełączenie środowiska czyści zaznaczenia, pobiera nowy stan i wymaga ponownego wyboru zmian. Nie stosujemy automatycznego przełączania na środowisko domyślne po błędzie.

## 8. Reguły porównywania

Narzędzie porównuje bazowy **Settings Config** w konkretnym środowisku przez interfejs administracyjny. Wynik pobrania przez klienta gry może uwzględniać reguły i segmentację, dlatego nie jest źródłem bazowego porównania edytora. Unity rozdziela ustawienia bazowe i Game Overrides dla środowisk. [Game Overrides and Settings](https://docs.unity.com/en-us/remote-config/game-overrides-and-settings).

| Sytuacja | Wynik |
| --- | --- |
| Równe wartości i typy, inna kolejność pól JSON | Brak różnicy |
| Inna wartość obsługiwanego pola | Wiersz „Zmienione” z wartościami Edytor / Remote |
| Brak bazowej konfiguracji, klucza `m3p_configs_v1` lub wpisu wybranego assetu | „Nowy — zostanie utworzony”; przygotowanie zasobów automatycznie przy publikacji |
| Brak wymaganego pola, błędny typ lub uszkodzony JSON | Błąd walidacji; wysyłanie zablokowane do rozstrzygnięcia |
| Nieznany schemat transportowy lub kontrakt wybranego obiektu | „Nieobsługiwana konfiguracja”; bez automatycznego nadpisania |
| Wpis zdalnego obiektu poza bieżącym wyborem | Zachowanie wpisu bez zmian; brak porównania z niezaznaczonym assetem |
| Inne klucze Remote Config | Pomijane w diffie M3P i zachowywane podczas zapisu |
| Zmiana wyłącznie prefabów, ikon lub innych pomijanych pól | Brak różnicy w publikowanych danych |
| Brak sieci lub uprawnień | „Nie udało się sprawdzić”, z czasem ostatniego sukcesu; nigdy „Zgodne” |

Porównanie jest typowane. Wartość brakująca, `null` i zero mają odrębne znaczenia. Białe znaki JSON nie mają znaczenia; białe znaki wewnątrz tekstu są zachowywane. Liczby porównujemy po normalizacji do typu kontraktu, bez tolerancji ukrywającej małe zmiany balansu. `NaN` i nieskończoność są odrzucane.

Kolejność tablic jest zachowywana, jeśli wpływa na działanie gry. W `damagePerSupermatchSize` indeks odpowiada rozmiarowi matcha, więc przestawienie elementów jest zmianą. Dopiero kolekcje z jawnym, stabilnym ID można porównywać według ID zamiast po pozycji.

Dodawanie wpisów wybranych lokalnych assetów jest częścią podstawowego działania. Nie tworzy to nowych prefabów ani definicji w już wydanym buildzie. Usunięcie lokalnego assetu objętego manifestem zgłasza brak źródła; nie usuwa danych w chmurze. Odznaczenie istniejącego assetu ma wyłącznie znaczenie opisane w zakresie synchronizacji.

## 9. Przebieg publikacji

1. Zapamiętaj projekt, środowisko, zaznaczone assety, pola i zawartość, którą użytkownik widział w oknie lub popupie. Dla nowego zestawu pokaż także zasoby, które zostaną automatycznie utworzone.
2. Zapisz wymagane assety, jeśli użytkownik wybrał przycisk obejmujący zapis. Ponownie wyeksportuj wartości.
3. Pobierz świeży stan zdalny. Jeśli lokalne dane, zdalne dane lub cel zmieniły się względem zaakceptowanego porównania, odśwież popup i poproś o ponowne kliknięcie wysyłania. Nie publikuj nowego, nieobejrzanego zestawu.
4. Zbuduj docelowy zestaw na bazie bieżącego remote: zastąp zaznaczone pola wartościami z edytora, a niezaznaczone pola i wpisy zachowaj. Nowy wpis zawiera pełny kontrakt danego assetu. Brak konfiguracji lub klucza oznacza plan ich utworzenia, bez wymogu ręcznej pracy na stronie.
5. Zweryfikuj format transportowy całego dokumentu oraz pełne kontrakty zmienianych wpisów i ich wymaganych zależności: wersje, zakresy liczb i długości tablic. Pozostałe wpisy zachowaj bez interpretowania nieznanych kontraktów. Nieznana zależność wymagana przez zmieniany wpis blokuje jego publikację. Przykładowo tablica bonusów ma dokładnie osiem elementów, wymagane XP jest dodatnie, a obliczenia progresji muszą mieścić się w obsługiwanych zakresach.
6. Zapisz lokalny snapshot poprzedniej wartości oraz docelowy zestaw, cel publikacji i hashe. Błąd zapisu snapshotu zatrzymuje wysyłkę.
7. Przez adapter korzystający z sesji edytora przygotuj brakujące zasoby i opublikuj docelowy zestaw. Uwzględnij etap publikacji, jeśli API rozdziela zapis roboczy od wersji dostarczanej grze. Zablokuj powtórne kliknięcie na czas operacji.
8. Pobierz konfigurację po zapisie i porównaj z docelowym zestawem. Przy opóźnieniu odczytu wykonaj ograniczoną liczbę prób. Dopiero zgodność potwierdza sukces.
9. Odśwież diff. Jeśli wysłano część pól, pozostałe różnice nadal są widoczne. Komunikat „Wysłano wybrane zmiany” nie oznacza zgodności wszystkich configów.

Snapshoty zapisujemy poza `Assets`, np. w `Library/M3PRemoteConfig/History/<environmentId>/`. Zawierają dane konfiguracji, bez poświadczeń. Jest to lokalna historia pomocnicza, usuwalna razem z `Library`; okno powinno umożliwiać eksport snapshotu do trwałego pliku. Przywrócenie snapshotu jest nową, jawną publikacją z ponownym porównaniem.

Jeżeli wybrany endpoint aktualizuje cały dokument Settings Config, adapter musi zachować wszystkie pozostałe klucze i metadane wymagane przez API. Zakres edycji M3P nadal ogranicza się do jego klucza. Uprawnienia do odczytu i zapisu po stronie narzędzia wynikają z wybranego mechanizmu administracyjnego. [Remote Config Admin API](https://docs.unity.com/en-us/oas-remote-config-admin/1.0.0).

**Równoczesne publikacje:** odczyt przed wysyłką wykrywa wcześniejsze zmiany, ale sam nie gwarantuje atomowego zapisu warunkowego. Przy implementacji należy sprawdzić dostępność mechanizmu wersji/ETag w używanym API. Jeśli go nie ma, pierwsza wersja wymaga jednego publikującego na środowisko; pełna koordynacja wielu autorów wymaga wspólnego serwisu publikacji. Własne `revision` ani hash nie zastępują takiej blokady.

Przy timeoutcie po wysłaniu status brzmi **Wynik publikacji niepotwierdzony**. Narzędzie najpierw odczytuje remote, aby ustalić, czy zapis się wykonał. Nie ponawia zapisu w ciemno. Błędy sieci, uprawnień i limity żądań mają osobne komunikaty oraz ręczną opcję ponowienia.

Tak samo obsługujemy przerwanie pierwszej publikacji: po wznowieniu sprawdzamy, które zasoby już powstały, i kontynuujemy bez duplikatów. Powstanie pustej konfiguracji po błędzie wysyłania nie oznacza udanej publikacji. Weryfikacja obejmuje stan opublikowany, a test integracji potwierdza, że otrzymuje go również klient runtime.

Narzędzie nie zmienia Game Overrides. Aktywna reguła obejmująca ten sam klucz może spowodować, że gracz dostanie inny wariant niż ustawienia bazowe. Okno wskazuje wykryte reguły dotyczące klucza albo informuje, że nie zostały sprawdzone; status zgodności dotyczy wyłącznie bazy.

## 10. Odczyt konfiguracji przez grę

SDK runtime służy do pobierania ustawień i zastosowania ich w kodzie. Wymaga przygotowania usług UGS i uwierzytelnienia zgodnie z używaną wersją pakietu. [Integracja w kodzie](https://docs.unity.com/en-us/remote-config/code-integration).

Proponowany start gry:

1. Wczytaj automatycznie wygenerowany katalog `remoteId → obiekt + kontrakt`, przygotuj lokalne wartości domyślne oraz kopie obiektów, które będą zmieniane w runtime.
2. Zainicjalizuj UGS we wspólnym punkcie z ustalonym środowiskiem. Włącz w ten proces istniejącą inicjalizację `GameAnalytics`.
3. Pobierz remote z limitem czasu, proponowane 5 sekund łącznie na etap sieciowy.
4. Zweryfikuj schemat transportowy i kontrakty wpisów znanych katalogowi buildu. Nieznane `remoteId` pomiń; nie twórz zdalnie nowych obiektów. Brak wpisu znanego assetu oznacza jego lokalne wartości domyślne. Niepoprawny kontrakt znanego wpisu odrzuca cały stosowany zestaw.
5. Zastosuj poprawne dane znanych obiektów jako jeden zestaw. Przy błędzie użyj ostatniego poprawnego zestawu zgodnego z bieżącym katalogiem, a przy jego braku wartości z builda.
6. Udostępnij gotowy `GameConfig`, utwórz usługi profilu/progresji i dopiero wtedy przejdź do Menu.

SDK ma własny cache, ale M3P musi dodatkowo sprawdzać poprawność i zgodność danych, zanim uzna je za ostatnią poprawną konfigurację. Cache należy rozdzielić według projektu, środowiska, schematu i podpisu wygenerowanego katalogu. [Mechanizm cache Unity](https://docs.unity.com/en-us/remote-config/caching-mechanism).

Katalog i mapowania pól powstają automatycznie podczas przygotowania eksportu/buildu, bez ręcznego pisania loadera dla każdego assetu. Generator i adaptery muszą zapewnić działanie w docelowym buildzie, w tym IL2CPP, bez `UnityEditor` i `AssetDatabase`. Status „Obsługiwany” wymaga poprawnego eksportu oraz zastosowania danych w runtime. Dodanie do synchronizacji obiektu, którego nie ma w katalogu wydanego klienta, wymaga dostarczenia klienta z nowym katalogiem; zmiana wartości już obsługiwanego obiektu tego nie wymaga.

Po upływie limitu startu późna odpowiedź sieciowa nie podmienia konfiguracji aktywnej sesji. Może zostać zwalidowana i zapisana na następny start. Pierwsza wersja stosuje balans raz na uruchomienie; wznowienie zapisanego runu po ponownym uruchomieniu używa aktualnie zaakceptowanego balansu. Przypięcie całego runu do historycznej wersji wymaga osobnego mechanizmu.

Samo `Instantiate(GameConfig)` nie kopiuje wszystkich powiązanych `ScriptableObject`. Trzeba skopiować każdy obiekt podlegający zmianom i przepiąć wszystkie jego referencje na tę samą kopię. Dotyczy to także ownerów poza `GameConfig`, np. obiektów scen korzystających z wybranego assetu. Resolver runtime lub adapter właściciela musi zapewnić, że gra korzysta z kopii. Asset bez obsłużonej ścieżki zastosowania nie może otrzymać statusu „Obsługiwany” tylko dlatego, że jego dane można zapisać do JSON. Należy również odświeżyć zależne cache rejestrów.

Zachowujemy istniejące ID zapisywane w profilach. Nazwy typów kafelków są obecnie używane jako klucze zapisu, a indeksy jako identyfikatory runtime; pierwsza wersja nie zmienia ich nazw ani kolejności. Zmiana parametrów progresji wymaga sprawdzenia wpływu na istniejące profile, szczególnie poziomy, naliczanie nagród i przyznane punkty.

Play Mode domyślnie używa lokalnych wartości, żeby można było testować nieopublikowane zmiany. Osobny tryb **Testuj Remote Config** uruchamia ścieżkę runtime z jawnie wybranym środowiskiem. Żaden tryb nie nadpisuje assetów źródłowych wartościami z chmury.

## 11. Podział implementacji

Poniższe nazwy oznaczają planowane komponenty:

| Komponent | Odpowiedzialność |
| --- | --- |
| `RemoteConfigSyncWindow` | Wybór źródeł i środowiska, status, tabela, przyciski |
| `ScriptableObjectSelector` | Lista assetów, checkboxy, drag-and-drop, filtry i raport kwalifikacji |
| `EditorCloudBootstrap` | Sesja edytora, powiązany projekt, odczyt środowiska i przygotowanie brakujących zasobów przy publikacji |
| `RemoteConfigChangeWatcher` | Obserwacja assetów, odroczenie sprawdzania i ograniczanie częstotliwości popupów |
| `ConfigExportRegistry` + eksporter ogólny + adaptery | Kontrakty wspieranych pól i typów, eksport do DTO, przyczyny wykluczenia |
| `RemoteAssetManifest` + generator katalogu | Trwałe identyfikatory, zapamiętany wybór i mapowania używane w buildzie |
| `ConfigDiffService` | Normalizacja, hash i diff pól z powiązaniem do assetów |
| `RemoteConfigAdminClient` | Odczyt bazy i publikacja z zachowaniem innych kluczy |
| `RemoteConfigPublishService` | Kontrola aktualności, scalanie wyboru, snapshot, zapis i weryfikacja |
| `BalanceConfigValidator` | Kontrakt i walidacja wspólna dla edytora i runtime |
| `RuntimeConfigFactory` | Kopie configów i spójne przepinanie referencji |
| `RemoteBalanceBootstrap` | Pobieranie w grze, limit czasu, fallback i gotowość konfiguracji |

Kod administracyjny i UI trafiają do assembly edytora, a format danych i walidacja do części współdzielonej. Adapter korzysta z obsługiwanego uwierzytelnienia sesji Unity Editor. Wariant wymagający ręcznego zakładania konta usługowego nie spełnia przyjętego wymagania obsługi. Poświadczenia nie trafiają do assetów, repozytorium ani buildu. Klient gry ma wyłącznie ścieżkę odczytu runtime.

Konfiguracja narzędzia przechowuje niesekretne ustawienia: zaznaczone assety, manifest tożsamości, rozpoznany projekt i środowisko, klucz, kontrakty i częstotliwość sprawdzania. Użytkownik nie musi wypełniać tych ustawień ręcznie przed wyborem obiektów. W pierwszej wersji nie wdrażamy pobierania remote do źródłowych assetów, automatycznych usunięć ani edycji reguł A/B.

## 12. Kolejność wdrożenia i kryteria odbioru

1. Dodać wymagane pakiety i potwierdzić cały proces odczytu, pierwszej publikacji i weryfikacji z sesją Unity Editor. Test nie może wymagać ręcznego tworzenia kluczy ani konta usługowego.
2. Wdrożyć wybór ScriptableObjectów, ocenę zgodności, trwały manifest i eksporter ogólny wraz z potrzebnymi adapterami M3P. `BattleConfig` i `LevelProgressionConfig` są pierwszymi przypadkami testowymi, nie ograniczeniem interfejsu do tych dwóch typów.
3. Dodać automatyczny generator katalogu runtime, kontrakty, walidację i deterministyczny diff.
4. Dodać ręczne sprawdzanie, automatyczne wyzwalacze, popup i publikację zaznaczonych assetów/pól, w tym tworzenie brakujących zasobów.
5. Dodać snapshoty, potwierdzenie opublikowanego wyniku oraz wznowienie przerwanej pierwszej publikacji.
6. Włączyć bootstrap runtime i przetestować docelowy build, offline oraz zgodność zapisanych profili.

Pierwsza wersja jest gotowa, kiedy spełnia poniższe scenariusze:

- [ ] Po otwarciu narzędzia można zaznaczyć ScriptableObjecty bez kreatora konfiguracji i wejścia do Dashboardu.
- [ ] Projekt i sesja konta są pobrane z Unity; środowisko docelowe jest rozpoznane i pokazane przed wysyłką.
- [ ] Można wybrać pojedynczy asset oraz kilka instancji tego samego typu, także poza zależnościami `GameConfig`.
- [ ] Lista pokazuje poprawną obsługę pełną, częściową albo konkretną przyczynę niedostępności; zakres pomijanych pól jest widoczny.
- [ ] Pierwsza publikacja tworzy brakujące zasoby bez ręcznych kluczy, ID, konta usługowego i JSON; klient runtime odbiera opublikowany wynik.
- [ ] Brak logowania lub uprawnień daje konkretny komunikat, bez fałszywego potwierdzenia przygotowania usługi.
- [ ] Rename/przeniesienie assetu zachowuje jego `remoteId`, kopia dostaje nowe ID, a odznaczenie nie usuwa danych remote.
- [ ] Nowy checkout i wyczyszczenie `Library` zachowują identyczne `remoteId` dzięki wersjonowanemu manifestowi.
- [ ] Nowy wybrany asset dodaje wpis, zachowując wpisy wszystkich niezaznaczonych obiektów.
- [ ] Zmiana `additionalAttackPerCascade` w Inspectorze powoduje popup z prawidłowymi wartościami Edytor / Remote.
- [ ] Zmiana w Dashboardzie jest wykrywana przy kolejnym odświeżeniu, także bez lokalnej edycji.
- [ ] „Później” niczego nie publikuje, a ten sam diff nie powoduje serii popupów po rekompilacji.
- [ ] Wysłanie jednego z dwóch zmienionych pól zachowuje zdalną wartość drugiego i pokazuje pozostałą różnicę.
- [ ] Każdy nowy wpis zawiera pełny kontrakt wybranego obiektu; usunięty asset źródłowy blokuje jego publikację.
- [ ] Nieaktualne porównanie wymaga ponownego obejrzenia zmian przed wysyłką.
- [ ] Inne klucze i reguły Remote Config pozostają zachowane.
- [ ] Nieprawidłowy JSON, nieznana wersja i zmiana środowiska blokują publikację nieaktualnego zestawu.
- [ ] Brak sieci i brak uprawnień są odróżniane od braku różnic; timeout zapisu prowadzi do weryfikacji wyniku.
- [ ] Kolejność pól JSON nie wywołuje diffu, a zmiana kolejności bonusów w tablicy go wywołuje.
- [ ] Niezapisane zmiany, Undo/Redo i zapis źródeł działają zgodnie z treścią przycisku i popupu.
- [ ] Runtime odczytuje opublikowane wartości, działa offline i nie zmienia konfiguracji w trakcie aktywnej sesji.
- [ ] Play Mode i pobranie zdalnych danych nie modyfikują źródłowych `.asset`.
- [ ] Wygenerowany katalog i mapowania działają w docelowym buildzie, bez `AssetDatabase`; nieznane obiekty remote są pomijane.
- [ ] Parametry są stosowane na kopiach, z których rzeczywiście korzystają systemy gry, również dla assetów wybranych poza `GameConfig`.
- [ ] Zmiana balansu progresji nie duplikuje nagród ani przyznanych punktów w istniejących profilach.
- [ ] UI wskazuje ograniczony zakres obsługi; nieuwzględnione configi nie są prezentowane jako zsynchronizowane.

Przykładowy test odbiorczy: bez otwierania Dashboardu zaznaczyć `BattleConfig.asset`, opublikować go pierwszy raz, zmienić lokalnie ataki kaskady z `1` na `2`, zobaczyć popup i wysłać zmianę. Odczyt potwierdzający zwraca `2`. Gra z wygenerowanym katalogiem uruchomiona w trybie Remote Config korzysta z tego parametru w walce.
