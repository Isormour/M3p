# Miejsca na dźwięk w projekcie M3p

Lista wskazuje momenty, w których efekt dźwiękowy potwierdzi akcję gracza lub wyjaśni wynik zdarzenia. Obejmuje obecną pętlę menu, mapy, walki i rozwoju postaci. Jeden plik audio może obsłużyć kilka podobnych zdarzeń.

Ścieżki skryptów w punktach poniżej są względne wobec `Assets/_Project/Scripts`, a ścieżki SFX wobec katalogu głównego projektu. **A** oznacza podstawową czytelność walki, **B** pełne sprzężenie zwrotne, a **C** końcowe dopracowanie interfejsu i ruchu. Przy każdym zdarzeniu podano moment startu, proponowany czas słyszalnego SFX i opis brzmienia.

**Status:** dotychczasowe próbki SFX usunięto z gry z powodu niskiej jakości. `SfxLibrary.asset` zawiera klipy zdarzeń z oceną „Pasuje”; pozostałe wywołania SFX nadal nie mają pliku. Ścieżki wdrożonych klipów wskazują pliki WAV. Przy imporcie kolejnych źródeł należy zaktualizować ścieżki i format w tym dokumencie oraz w `SfxLibraryBuilder.cs`.

**Wdrożenie 25.09.2026:** Do gry weszły zdarzenia z oceną „Pasuje” i przypisanym źródłem. Wybrana wersja jest używana tam, gdzie ją wskazano. Bez wybranej wersji gra pełne źródło po wyrównaniu szczytu do −6 dBFS, bez skracania do niewybranego wycinka. Gdy kilka próbek pasuje do jednego zdarzenia, pierwszeństwo ma próbka z wybraną wersją, potem próbka o nazwie zdarzenia. Pominięto odrzucone `map_token_step`, `forge_open` oraz zależne `tile_slot_select`. `card_deselect` i `shield_fade` używają odwróconego wycinka, bo pełne źródło jest tym samym nagraniem co dobranie karty i pojawienie tarczy. Zdarzenie z checkboxem pozostaje otwarte, jeśli choć jeden jego plik nie ma jeszcze dobranego dźwięku.

**Dobór po ocenie użytkownika:** 37 pasuje, 2 odrzucone, 0 do poprawy, 0 bez oceny. [Tabela ocen i wybranych wersji](M3p_dobor_dzwiekow_v1.md), [eksport 25.09.2026 20:43:37](AudioSelection/review_decisions.json) oraz [panel odsłuchu](AudioSelection/index.html). Oceny wpisano przy zdarzeniach poniżej. Checkbox oznacza, że każdy plik SFX tego zdarzenia jest już w bibliotece. Zaznaczone pozycje są wdrożone.

**Do dalszego doboru:** [80 brakujących dźwięków — miejsca występowania, moment startu i opis brzmienia](M3p_brakujace_dzwieki.md).

## Czas i synchronizacja dźwięku

**Czas SFX należy projektować od zdarzenia widocznego na ekranie.** Dla każdej próbki warto ustalić: początek (np. klik, wystrzał, pierwsza iskra), moment największego akcentu (np. trafienie) oraz koniec słyszalnego wybrzmienia. Czas życia cząstki, czas emisji całego systemu i czas utrzymywania się statusu w turach to trzy różne wartości. Długi ogon VFX może być już cichy; nie musi przez cały czas grać równie głośny efekt.

Poniższe **czasy docelowe SFX są propozycją montażową**, a nie pomiarem długości dawnych próbek. Wartości w kolumnie „obraz/akcja” pochodzą z obecnego kodu lub prefabów; ostateczny koniec słyszalnego dźwięku trzeba potwierdzić w Play Mode, patrząc na rzeczywiste wygaszenie efektu.

| Zdarzenie | Czas obrazu / akcji w projekcie | Docelowy SFX i punkt synchronizacji |
| --- | --- | --- |
| Kliknięcia, wybory kart i proste potwierdzenia UI | Zdarzenie chwilowe; brak wspólnego czasu animacji | 0,08–0,22 s, akcent od razu po przyjęciu akcji. Dłuższy ogon tylko przy widocznej animacji panelu/nagrody. |
| Zamiana dwóch kafelków | `swapDuration = 0,12 s` w `Match3/Match3Board.cs` | Około 0,15–0,20 s od rozpoczęcia ruchu, z głównym akcentem przed jego końcem. |
| Obrót grupy / przesunięcie rzędu | Ruch kafelków używa `swapDuration = 0,12 s` | Około 0,15–0,22 s; nie przedłużać tylko dlatego, że porusza się więcej kafelków. |
| Spadanie i pojawianie się kafelków | `fallDuration = 0,15 s` dla ruchu; kaskada składa się z wielu kroków | Pojedynczy miękki akcent 0,12–0,22 s. Przy kaskadzie powtarzać/warstwować na zdarzeniach, a nie odtwarzać jedną długą próbkę przez całą sekwencję. |
| Rozbicie kafelka i zebranie jego VFX | `UI/VFXManager.cs` usuwa instancję efektu po 2 s; to limit sprzątania, nie potwierdzony czas widoczności | Ciche pęknięcie pojedynczego kafelka 0,08–0,18 s; główny akcent raz na falę. Osobny ruch zbierania tylko wtedy, gdy widać go w Play Mode. Nie rozciągać trzasku do 2 s. |
| Krok pionka na mapie | Próg 1,1 jednostki przy prędkości 6 jednostek/s, nominalnie około 0,18 s między krokami (`Map/MapPlayerToken.cs`) | Każdy krok około 0,10–0,16 s; długość całej podróży jest zmienna. Obecny `SfxPlayer` ogranicza powtórki tego cue do 0,22 s. |
| Zamach ataku podstawowego | `_attackWindup = 0,18 s` w `Battle/BattleManager.cs` | Sygnał zamachu około 0,15–0,22 s, zakończony przed wypuszczeniem pocisku. |
| Lot pocisku i trafienie | Lot trwa `max(0,12 s, odległość / 32)` w `VFX/ProjectileVFX.cs` | Krótki start przy wystrzale, ewentualna cicha warstwa lotu tylko do `onArrived`; osobny akcent trafienia około 0,20–0,45 s dokładnie na dolocie. Długość lotu jest zmienna. |
| Trafienie w tarczę; pojawienie się i zniknięcie tarczy | Puls trafienia 0,22 s; fade tarczy 0,25 s (`Battle/BattleCharacterShield.cs`) | Trafienie około 0,20–0,30 s od kontaktu; pojawienie/zniknięcie około 0,20–0,30 s, tak by kończyć wraz z fade. |
| **Burn: obrażenie na początku tury** | Status `Burn` trwa 2 tury (`Configs/Status/Burn.asset`), ale `SmallFlame` na postaci/wrogu jest jednorazowy: emisja 1 s, życie cząstki 0,5–1 s. U gracza ostatnie cząstki mogą zniknąć około 2 s od startu. U wroga emisja jest ograniczona do burstu, z losowym opóźnieniem 0–0,25 s. | **Jeden cue na każde obrażenie od podpalenia**, nie ciągły dźwięk przez 2 tury. Krótki zapłon/uderzenie 0,3–0,5 s od ticku obrażeń, potem cichy, gasnący ogon; całkowity cel około 0,8–1,2 s. Jeżeli dźwięk ma podążać za całym płomieniem, potrzebna jest osobna warstwa sterowana życiem VFX, kończąca się wraz z nim. Dla wroga dostroić początek warstwy płomienia do opóźnienia cząsteczek. |
| Zbieranie odłamków | Cząstka `BasicShard` żyje 0,8 s; `VFXManager` usuwa instancję po 2 s | Akcent przy pojawieniu i/lub dolocie do licznika. Nie wydłużać samego dźwięku do 2 s tylko z powodu czasu sprzątania instancji. |
| Napełnianie doświadczenia po walce | Jeden odcinek paska trwa `max(0,15 s; 0,9 s × max(0,05; przyrost))`; między poziomami jest pauza 0,15 s (`UI/UIEndBattlePanel.cs`) | Warstwa napełniania powinna kończyć się na końcu każdego odcinka, a awans ma osobny akcent. Jeśli przyrost jest zmienny, dopasować/sterować czasem odtwarzania zamiast używać stałego, długiego one-shotu. |

W szczegółowej liście poniżej **czas oznacza długość słyszalnego pliku wraz z ogonem**, a „Start” punkt wyzwolenia. Liczby bez wskazanego czasu animacji w kodzie są propozycjami do pomiaru w Play Mode, nie potwierdzonymi długościami VFX. Paleta brzmieniowa: subtelne magiczne fantasy, czytelny kontakt fizyczny i organiczna energia, bez metalicznych dzwonków. Przy dużych efektach rozdziel cast, wystrzał, trafienie i dogasanie, jeśli następują w różnych chwilach.

**Ograniczenie obecnej implementacji:** `Audio/SfxPlayer.cs` używa `AudioSource.PlayOneShot`, więc klip gra do końca niezależnie od zakończenia animacji lub VFX. Dla efektów o zmiennej długości (lot, podróż, napełnianie paska, ewentualny ciągły płomień) trzeba użyć krótkich zdarzeń punktowych albo dodać warstwę audio z kontrolowanym stop/fade. Samo skrócenie jednego pliku nie zapewni synchronizacji we wszystkich wariantach.

## Menu i mapa

- [x] **B** Wybór Kontynuuj, Nowa gra, Mapa testowa lub Wyjdź: klik potwierdzenia. `UI/MainMenu.cs:47` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/menu_confirm.wav`
  - **Ocena użytkownika — `menu_confirm`:** [menu_confirm](M3p_dobor_dzwiekow_v1.md#sfx-menu_confirm) — **Pasuje**, wersja niewybrana; [menu_confirm_alt](M3p_dobor_dzwiekow_v1.md#sfx-menu_confirm_alt) — **Pasuje**, pełne źródło (0.74 s). <!-- USER-SFX-REVIEW -->
  - **Start:** po przyjęciu wyboru menu. **Czas SFX:** 0,10–0,18 s. **Brzmienie:** Miękki, suchy nacisk z krótką ciepłą odpowiedzią; bez dzwonka.
- [x] **B** Przejście menu -> mapa -> walka i powrót: krótki dźwięk przejścia. `Core/SceneFlow.cs:12` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/scene_transition.wav`
  - **Ocena użytkownika — `scene_transition`:** [scene_transition](M3p_dobor_dzwiekow_v1.md#sfx-scene_transition) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** przy rozpoczęciu zmiany sceny. **Czas SFX:** 0,25–0,50 s; czas ładowania jest zmienny. **Brzmienie:** Krótki oddech przestrzeni i łagodne domknięcie; akcent przy odsłonięciu nowej sceny wymaga osobnego wyzwalacza.
- [x] **B** Kliknięcie dostępnego węzła mapy: wybór punktu podróży. `Map/MapManager.cs:436` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_node_select.wav`
  - **Ocena użytkownika — `map_node_select`:** [map_node_select](M3p_dobor_dzwiekow_v1.md#sfx-map_node_select) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po kliknięciu dostępnego węzła. **Czas SFX:** 0,10–0,18 s. **Brzmienie:** Punktowy dotyk mapy z subtelną iskrą orientacji, bez nagrody za samo najechanie.
- [x] **B** Otwarcie podglądu i potwierdzenia drogi: miękki sygnał panelu. `Map/MapManager.cs:459` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/route_preview.wav`
  - **Ocena użytkownika — `route_preview`:** [panel_open](M3p_dobor_dzwiekow_v1.md#sfx-panel_open) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy podgląd trasy pojawia się na ekranie. **Czas SFX:** 0,18–0,32 s. **Brzmienie:** Cichy rozwijany pergamin i miękki przepływ magicznej linii.
- [x] **B** Potwierdzenie lub anulowanie drogi: dwa krótkie warianty. `UI/UIMapPanelWalkNodeConfirm.cs:99` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/route_confirm.wav`, `Assets/_Project/Audio/SFX/MenuMap/route_cancel.wav`
  - **Ocena użytkownika — `route_confirm`:** [menu_confirm](M3p_dobor_dzwiekow_v1.md#sfx-menu_confirm) — **Pasuje**, wersja niewybrana; [menu_confirm_alt](M3p_dobor_dzwiekow_v1.md#sfx-menu_confirm_alt) — **Pasuje**, pełne źródło (0.74 s). <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `route_cancel`:** [route_cancel](M3p_dobor_dzwiekow_v1.md#sfx-route_cancel) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po potwierdzeniu lub anulowaniu. **Czas SFX:** potwierdzenie 0,12–0,22 s; anulowanie 0,10–0,18 s. **Brzmienie:** Potwierdzenie: osadzony, ciepły puls; anulowanie: krótki gest cofnięcia bez negatywnego alarmu.
- [ ] **C** Ruch pionka po mapie: kroki dopasowane do animacji, nie do każdej klatki. `Map/MapPlayerToken.cs:63` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_token_step.mp3`
  - **Ocena użytkownika — `map_token_step`:** [map_token_step](M3p_dobor_dzwiekow_v1.md#sfx-map_token_step) — **Odrzucone**. <!-- USER-SFX-REVIEW -->
  - **Start:** na każdym progu 1,1 jednostki ruchu. **Czas SFX:** 0,10–0,16 s na krok; droga ma zmienny czas. **Brzmienie:** Lekki krok pionka na kamiennej mapie; przy prędkości 6 jednostek/s kroki wypadają nominalnie co ok. 0,18 s, odtwarzacz ogranicza je do 0,22 s.
- [x] **B** Dotarcie do węzła: akcent zakończenia podróży. `Map/MapManager.cs:514` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_arrival.wav`
  - **Ocena użytkownika — `map_arrival`:** [map_arrival](M3p_dobor_dzwiekow_v1.md#sfx-map_arrival) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po zatrzymaniu pionka na węźle. **Czas SFX:** 0,18–0,30 s. **Brzmienie:** Miękkie osadzenie punktu podróży z krótkim rozjaśnieniem.
- [ ] **A** Wejście do walki zwykłej, z elitą lub bossem: warianty narastającego sygnału. `Map/MapManager.cs:582` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/encounter_normal.mp3`, `Assets/_Project/Audio/SFX/MenuMap/encounter_elite.mp3`, `Assets/_Project/Audio/SFX/MenuMap/encounter_boss.mp3`
  - **Start:** gdy wybrany węzeł uruchamia spotkanie. **Czas SFX:** zwykłe 0,30–0,50 s; elita 0,40–0,65 s; boss 0,55–0,90 s. **Brzmienie:** Ten sam ciemny motyw energii o rosnącej wadze i głębi; wyraźny początek bez długiej fanfary podczas ładowania sceny.
- [x] **B** Otwarcie sklepu z kartami: dźwięk wejścia do usługi. `Map/MapManager.cs:596` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/card_shop_open.wav`
  - **Ocena użytkownika — `card_shop_open`:** [hand_draw](M3p_dobor_dzwiekow_v1.md#sfx-hand_draw) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili otwarcia sklepu. **Czas SFX:** 0,22–0,40 s, do potwierdzenia z animacją panelu. **Brzmienie:** Ciepły szelest kart i spokojne magiczne zaproszenie, bez monetowego brzęku.
- [ ] **B** Otwarcie kuźni kafelków: krótki akcent żaru i runy. `Map/MapManager.cs:609` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/forge_open.mp3`
  - **Ocena użytkownika — `forge_open`:** [forge_open](M3p_dobor_dzwiekow_v1.md#sfx-forge_open) — **Odrzucone**. <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili otwarcia kuźni. **Czas SFX:** 0,25–0,45 s, do potwierdzenia z animacją panelu. **Brzmienie:** Głęboki oddech żaru, kamienny nacisk i krótka iskra runy; bez metalicznego dzwonienia.
- [x] **B** Otwarcie skrzyni: zatrzask i pokrywa. `Map/MapManager.cs:648` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_open.wav`
  - **Ocena użytkownika — `chest_open`:** [chest_open](M3p_dobor_dzwiekow_v1.md#sfx-chest_open) — **Pasuje**, wersja niewybrana; [chest_open_alt](M3p_dobor_dzwiekow_v1.md#sfx-chest_open_alt) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** przy zwolnieniu zatrzasku, z drugim akcentem na uniesieniu wieka. **Czas SFX:** 0,35–0,70 s; czas wieka wymaga pomiaru w Play Mode. **Brzmienie:** Drewno, ciężar i miękki ruch zawiasu; nagroda ma osobny dźwięk.
- [ ] **B** Odebranie nagrody ze skrzyni: krótki dźwięk zdobyczy. `Map/MapManager.cs:670` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_reward.mp3`
  - **Start:** po faktycznym odebraniu zawartości. **Czas SFX:** 0,20–0,38 s. **Brzmienie:** Krótki, ciepły impuls zdobyczy z lekką magią, bez jasnej kaskady dzwonków.
- [x] **C** Pusta skrzynia i potwierdzenie okna zdarzenia: neutralny klik. `Map/MapManager.cs:573; Map/MapEventPresenter.cs:117` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_empty.wav`
  - **Ocena użytkownika — `chest_empty`:** [route_cancel](M3p_dobor_dzwiekow_v1.md#sfx-route_cancel) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy gra potwierdza pustą skrzynię lub zamknięcie okna. **Czas SFX:** 0,08–0,16 s. **Brzmienie:** Neutralny, przytłumiony dotyk drewna lub UI; nie sugeruje zdobytej nagrody.
- [ ] **C** Otwieranie i zamykanie paneli statystyk, talii, craftingu i umiejętności. `UI/UIMapPlayerStats.cs:117; UI/UIPanelClosable.cs:74` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/panel_open.wav`, `Assets/_Project/Audio/SFX/MenuMap/panel_close.mp3`
  - **Ocena użytkownika — `panel_open`:** [panel_open](M3p_dobor_dzwiekow_v1.md#sfx-panel_open) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** przy otwarciu i zamknięciu panelu. **Czas SFX:** każdy 0,14–0,28 s; zgrać z widocznym ruchem panelu. **Brzmienie:** Miękki ruch pergaminu i delikatne osadzenie; zamknięcie ma odwrócony kontur.

## Karty i przebieg tury

- [ ] **A** Początek walki: sygnał wejścia na planszę. `Battle/BattleManager.cs:305` **SFX:** `Assets/_Project/Audio/SFX/Cards/battle_start.mp3`
  - **Start:** gdy plansza walki staje się aktywna. **Czas SFX:** 0,45–0,75 s; wizualny start sceny do pomiaru. **Brzmienie:** Niski puls aktywacji i krótkie rozbudzenie planszy, bez muzycznej fanfary.
- [ ] **B** Początek tury gracza i odzyskanie kontroli: krótki znacznik tury. `Battle/BattleManager.cs:735` **SFX:** `Assets/_Project/Audio/SFX/Cards/player_turn_start.mp3`
  - **Start:** przy odzyskaniu kontroli przez gracza. **Czas SFX:** 0,14–0,26 s. **Brzmienie:** Jasny, lecz miękki puls gotowości, różny od tury przeciwnika.
- [x] **B** Dobranie ręki na początku tury: szelest talii. `Cards/CardPlayController.cs:107` **SFX:** `Assets/_Project/Audio/SFX/Cards/hand_draw.wav`
  - **Ocena użytkownika — `hand_draw`:** [hand_draw](M3p_dobor_dzwiekow_v1.md#sfx-hand_draw) — **Pasuje**, wersja niewybrana; [hand_draw_alt](M3p_dobor_dzwiekow_v1.md#sfx-hand_draw_alt) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** przy pojawieniu się całej nowej ręki. **Czas SFX:** 0,25–0,45 s; liczba kart jest zmienna. **Brzmienie:** Kilka krótkich warstw papieru z jednym wspólnym finałem, bez osobnego głośnego cue dla każdej karty.
- [x] **B** Dobranie dodatkowej karty w środku tury: pojedyncze dobranie. `Cards/CardPlayController.cs:149` **SFX:** `Assets/_Project/Audio/SFX/Cards/single_card_draw.wav`
  - **Ocena użytkownika — `single_card_draw`:** [single_card_draw](M3p_dobor_dzwiekow_v1.md#sfx-single_card_draw) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** na pojawieniu się jednej dodatkowej karty. **Czas SFX:** 0,12–0,24 s. **Brzmienie:** Pojedynczy, suchy szelest karty z miękkim końcem.
- [x] **A** Wybranie karty z ręki: lekki klik lub szelest. `Cards/CardPlayController.cs:205` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_select.wav`
  - **Ocena użytkownika — `card_select`:** [card_select](M3p_dobor_dzwiekow_v1.md#sfx-card_select) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po skutecznym wybraniu karty. **Czas SFX:** 0,08–0,16 s. **Brzmienie:** Małe uniesienie papieru i precyzyjny, niemetaliczny kontakt.
- [x] **B** Odznaczenie karty albo anulowanie celowania: cofnięcie wyboru. `Cards/CardPlayController.cs:236` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_deselect.wav`
  - **Ocena użytkownika — `card_deselect`:** [card_deselect](M3p_dobor_dzwiekow_v1.md#sfx-card_deselect) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po odznaczeniu lub wyjściu z celowania. **Czas SFX:** 0,10–0,18 s. **Brzmienie:** Cichy powrót papieru, kontur opadający względem wyboru.
- [x] **A** Wskazanie poprawnego kafelka jako celu karty: krótki klik pola. `Cards/CardPlayController.cs:298` **SFX:** `Assets/_Project/Audio/SFX/Cards/target_tile_select.wav`
  - **Ocena użytkownika — `target_tile_select`:** [map_node_select](M3p_dobor_dzwiekow_v1.md#sfx-map_node_select) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po zaakceptowaniu poprawnego kafelka. **Czas SFX:** 0,07–0,14 s. **Brzmienie:** Krótki, miękki dotyk pola; niższy poziom niż dźwięk wykonania akcji.
- [ ] **B** Wskazanie ostatniego wymaganego celu: akcent domknięcia wyboru. `Cards/CardPlayController.cs:323` **SFX:** `Assets/_Project/Audio/SFX/Cards/target_selection_complete.mp3`
  - **Start:** na ostatnim wymaganym celu. **Czas SFX:** 0,12–0,22 s. **Brzmienie:** Dwa blisko położone impulsy domykające wybór, bez długiego wybrzmienia.
- [ ] **B** Otwarcie dodatkowego wyboru koloru lub kierunku grawitacji. `Cards/CardPlayController.cs:337` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_choice_open.mp3`
  - **Start:** na pokazaniu wyboru koloru lub grawitacji. **Czas SFX:** 0,18–0,30 s. **Brzmienie:** Delikatne rozchylenie magicznej warstwy UI.
- [x] **B** Potwierdzenie lub anulowanie dodatkowego wyboru. `UI/UICardChoiceOverlay.cs:155` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_choice_confirm.wav`, `Assets/_Project/Audio/SFX/Cards/card_choice_cancel.wav`
  - **Ocena użytkownika — `card_choice_confirm`:** [menu_confirm](M3p_dobor_dzwiekow_v1.md#sfx-menu_confirm) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `card_choice_cancel`:** [route_cancel](M3p_dobor_dzwiekow_v1.md#sfx-route_cancel) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po potwierdzeniu lub anulowaniu dodatkowego wyboru. **Czas SFX:** każdy 0,10–0,20 s. **Brzmienie:** Potwierdzenie: osadzenie energii; anulowanie: krótkie odwrócenie przepływu.
- [x] **A** Dodanie karty do kolejki: wyraźny klik zatwierdzenia akcji. `Cards/CardPlayController.cs:382` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_queue_add.wav`
  - **Ocena użytkownika — `card_queue_add`:** [card_queue_add](M3p_dobor_dzwiekow_v1.md#sfx-card_queue_add) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy karta trafia do kolejki. **Czas SFX:** 0,10–0,20 s. **Brzmienie:** Suchy kontakt karty z miejscem w kolejce i mały puls gotowości.
- [x] **A** Cofnięcie ostatniej karty z kolejki: krótki dźwięk powrotu. `Cards/CardPlayController.cs:256` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_queue_undo.wav`
  - **Ocena użytkownika — `card_queue_undo`:** [card_deselect](M3p_dobor_dzwiekow_v1.md#sfx-card_deselect) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy ostatnia karta wraca z kolejki. **Czas SFX:** 0,10–0,20 s. **Brzmienie:** Krótki odwrócony szelest papieru, bez sygnału błędu.
- [x] **A** Naciśnięcie Resolve i start odtwarzania kolejki: sygnał uruchomienia. `UI/UIPanelCardHand.cs:152; Cards/CardPlayController.cs:277` **SFX:** `Assets/_Project/Audio/SFX/Cards/resolve_start.wav`
  - **Ocena użytkownika — `resolve_start`:** [resolve_start](M3p_dobor_dzwiekow_v1.md#sfx-resolve_start) — **Pasuje**, pełne źródło (0.88 s). <!-- USER-SFX-REVIEW -->
  - **Start:** na zaakceptowanym Resolve, przed pierwszą akcją kolejki. **Czas SFX:** 0,18–0,32 s; dalsze akcje mają własne SFX. **Brzmienie:** Krótki rozruch skupionej energii, nie dźwięk całej sekwencji.
- [x] **C** Odrzucenie zagranych kart po Resolve: szelest stosu odrzuconych. `Cards/CardPlayController.cs:285` **SFX:** `Assets/_Project/Audio/SFX/Cards/cards_discard.wav`
  - **Ocena użytkownika — `cards_discard`:** [cards_discard](M3p_dobor_dzwiekow_v1.md#sfx-cards_discard) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy zagrane karty są odkładane. **Czas SFX:** 0,18–0,32 s; dopasować do liczby kart. **Brzmienie:** Jedno miękkie zsunięcie stosu papieru, nie seria ostrych kliknięć.
- [x] **B** Naciśnięcie Koniec tury i odrzucenie ręki: zamknięcie tury. `UI/UIPanelCardHand.cs:162; Cards/CardPlayController.cs:116` **SFX:** `Assets/_Project/Audio/SFX/Cards/player_turn_end.wav`
  - **Ocena użytkownika — `player_turn_end`:** [cards_discard](M3p_dobor_dzwiekow_v1.md#sfx-cards_discard) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po przyjęciu końca tury. **Czas SFX:** 0,20–0,36 s. **Brzmienie:** Zamknięcie ręki: krótki szelest i niższy puls przekazania kontroli.
- [ ] **B** Początek i zakończenie tury przeciwnika: dyskretny sygnał zmiany strony. `Battle/BattleManager.cs:759; Battle/BattleManager.cs:785` **SFX:** `Assets/_Project/Audio/SFX/Cards/enemy_turn_start.wav`, `Assets/_Project/Audio/SFX/Cards/enemy_turn_end.mp3`
  - **Ocena użytkownika — `enemy_turn_start`:** [enemy_intent](M3p_dobor_dzwiekow_v1.md#sfx-enemy_intent) — **Pasuje**, pełne źródło (0.99 s). <!-- USER-SFX-REVIEW -->
  - **Start:** na początku i końcu tury przeciwnika. **Czas SFX:** po 0,14–0,25 s; myślenie wroga 0,35 s jest osobną pauzą. **Brzmienie:** Początek: ciemniejszy puls ostrzegawczy; koniec: spokojne rozładowanie napięcia.

## Plansza i zasoby

- [x] **A** Zamiana dwóch kafelków: krótki ruch przestrzenny. `Match3/Match3Board.cs:526` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_swap_timed.wav`
  - **Ocena użytkownika — `tile_swap_timed`:** [tile_swap](M3p_dobor_dzwiekow_v1.md#sfx-tile_swap) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili rozpoczęcia zamiany. **Czas SFX:** 0,17 s w aktywnym WAV przy ruchu 0,12 s. **Brzmienie:** Dwa miękkie ruchy kamienia i subtelna energia; drugi kontakt przy osadzeniu kafelków, bez metalicznego ogona.
- [x] **B** Obrót grupy kafelków lub przesunięcie rzędu: ruch wielu kafelków. `Match3/Match3Board.cs:595` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_rotate.wav`, `Assets/_Project/Audio/SFX/Board/row_shift.wav`
  - **Ocena użytkownika — `tile_rotate`:** [tile_swap](M3p_dobor_dzwiekow_v1.md#sfx-tile_swap) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `row_shift`:** [tile_swap](M3p_dobor_dzwiekow_v1.md#sfx-tile_swap) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** na starcie obrotu lub przesunięcia. **Czas SFX:** 0,15–0,22 s; oba ruchy kafelków trwają 0,12 s. **Brzmienie:** Warstwowy miękki ślizg kamienia z jednym wspólnym osadzeniem, bez wydłużania go proporcjonalnie do liczby kafelków.
- [ ] **B** Przemalowanie kafelka: zmiana barwy lub magiczne muśnięcie. `Match3/Match3Board.cs:680` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_recolor.mp3`
  - **Start:** w momencie zmiany koloru kafelka. **Czas SFX:** 0,15–0,28 s. **Brzmienie:** Krótki, płynny przepływ energii między barwami z cichym końcowym rozbłyskiem.
- [x] **B** Oznaczenie kafelka do rozbicia: krótki trzask pęknięcia. `Match3/Match3Board.cs:431` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_crack_mark.wav`
  - **Ocena użytkownika — `tile_crack_mark`:** [tile_break](M3p_dobor_dzwiekow_v1.md#sfx-tile_break) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy kafelek dostaje znacznik pęknięcia. **Czas SFX:** 0,07–0,14 s. **Brzmienie:** Cichy suchy trzask rysy; bez pełnego rozbicia.
- [x] **A** Faktyczne rozbicie kafelków: krótkie pęknięcia pojedynczych kafelków. `Match3/Match3Board.cs:800` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_break.wav`
  - **Ocena użytkownika — `tile_break`:** [tile_break](M3p_dobor_dzwiekow_v1.md#sfx-tile_break) — **Pasuje**, wersja niewybrana; [tile_break_alt](M3p_dobor_dzwiekow_v1.md#sfx-tile_break_alt) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** na każdym faktycznie niszczonym kafelku. **Czas SFX:** 0,08–0,18 s na kafelek; limitować nakładanie. **Brzmienie:** Małe kruche pęknięcie pojedynczego kamienia. Główny akcent całej fali jest osobną pozycją „Zakończenie jednej fali”.
- [ ] **B** Usunięcie blokady lub negatywnego kafelka przez Purge. `Match3/Match3Board.cs:576` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_purge.mp3`
  - **Start:** na usunięciu blokady przez Purge. **Czas SFX:** 0,20–0,38 s. **Brzmienie:** Krótki rozpad ciemnej powłoki i ciche oczyszczenie energii.
- [ ] **B** Zmiana kierunku grawitacji: niski akcent lub obrót przestrzeni. `Match3/Match3Board.cs:439` **SFX:** `Assets/_Project/Audio/SFX/Board/gravity_shift.mp3`
  - **Start:** po ustawieniu nowego kierunku grawitacji. **Czas SFX:** 0,20–0,35 s; kolejne spadanie brzmi osobno. **Brzmienie:** Niski, miękki obrót przestrzeni bez mechanicznego zgrzytu.
- [ ] **B** Przetasowanie planszy: wieloelementowy ruch. `Match3/Match3Board.cs:638` **SFX:** `Assets/_Project/Audio/SFX/Board/board_shuffle.mp3`
  - **Start:** na rozpoczęciu przetasowania. **Czas SFX:** 0,18–0,30 s; widoczne ruchy używają 0,12 s. **Brzmienie:** Kilka przesunięć kamienia z jednym domknięciem, bez długiego grzechotania.
- [x] **C** Opadnięcie kafelków w puste miejsca: lekki dźwięk osiadania. `Match3/Match3Board.cs:886` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_fall.wav`
  - **Ocena użytkownika — `tile_fall`:** [tile_fall](M3p_dobor_dzwiekow_v1.md#sfx-tile_fall) — **Pasuje**, wycinek (0.20 s). <!-- USER-SFX-REVIEW -->
  - **Start:** gdy rusza opadanie kafelków. **Czas SFX:** 0,12–0,22 s na grupę; animacja opadania 0,15 s. **Brzmienie:** Lekkie, nierówne osadzenie kamienia; cicho, bo kaskady mogą się powtarzać.
- [ ] **C** Pojawienie się nowych kafelków: delikatne uzupełnienie. `Match3/Match3Board.cs:911` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_spawn.mp3`
  - **Start:** gdy pojawiają się nowe kafelki. **Czas SFX:** 0,10–0,18 s na grupę. **Brzmienie:** Miękki impuls pojawienia się materii, krótszy niż dźwięk zbierania nagrody.
- [ ] **A** Zakończenie jednej fali dopasowania: jeden akcent na falę. `Match3/Match3Board.cs:496` **SFX:** `Assets/_Project/Audio/SFX/Board/match_wave_complete.mp3`
  - **Start:** raz po zakończeniu każdej fali. **Czas SFX:** 0,20–0,36 s; niezależnie od liczby kafelków. **Brzmienie:** Spójny magiczny akcent wyniku fali, mocniejszy od pęknięć pojedynczych kafelków.
- [ ] **A** Duże dopasowanie: mocniejszy wariant akcentu fali. `UI/VFXManager.cs:447` **SFX:** `Assets/_Project/Audio/SFX/Board/big_match.mp3`
  - **Start:** przy rozpoznaniu dużego dopasowania. **Czas SFX:** 0,28–0,48 s. **Brzmienie:** Głębszy, szerszy wariant ukończenia fali; nie dubluje głośno Super Match.
- [ ] **A** Kolejna fala kaskady i Super Match: narastająca energia oraz osobny finał. `UI/VFXManager.cs:412; UI/VFXManager.cs:447` **SFX:** `Assets/_Project/Audio/SFX/Board/cascade_wave.mp3`, `Assets/_Project/Audio/SFX/Board/super_match.mp3`
  - **Start:** na każdej następnej fali oraz osobno na Super Match. **Czas SFX:** kaskada 0,18–0,30 s na falę; finał 0,35–0,55 s. **Brzmienie:** Narastająca energia fal, potem pojedynczy finał. Wskaźnik kaskady/Super Match jest widoczny ok. 1 s, ale nie wymaga ciągłego tonu.
- [ ] **B** Przyrost many po fali: krótki sygnał napełniania zasobu. `UI/UIPanelPlayerMana.cs:149` **SFX:** `Assets/_Project/Audio/SFX/Board/mana_gain.mp3`
  - **Start:** na zmianie licznika many po fali. **Czas SFX:** 0,14–0,28 s. **Brzmienie:** Krótki płynny wzrost energii w zasobie, cichszy od głównego akcentu fali.
- [ ] **B** Pojawienie się odłamków i dotarcie ich do licznika: lekki zbiór. `UI/VFXManager.cs:517` **SFX:** `Assets/_Project/Audio/SFX/Board/shard_collect.mp3`
  - **Start:** przy pojawieniu się odłamków; osobny finał na dolocie wymaga nowego wyzwalacza. **Czas SFX:** 0,35–0,75 s; cząstki żyją 0,8 s, usunięcie obiektu po 2 s jest tylko sprzątaniem. **Brzmienie:** Kilka delikatnych ziaren energii w ruchu do licznika; bez dźwięku trwającego do czasu usunięcia obiektu.
- [ ] **B** Premia z kafelka: mana, HP, punkty akcji, tarcza lub dodatkowa karta. `Tiles/TileUpgradeLogics.cs:9` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_bonus_mana.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_heal.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_action.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_shield.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_card.wav`
  - **Ocena użytkownika — `tile_bonus_card`:** [single_card_draw](M3p_dobor_dzwiekow_v1.md#sfx-single_card_draw) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili przyznania danej premii. **Czas SFX:** mana/HP/AP/tarcza/karta po 0,12–0,28 s. **Brzmienie:** Wspólne miękkie źródło; mana płynie, HP ogrzewa, AP daje rytmiczny impuls, tarcza zagęszcza, karta dodaje papierowy szelest.
- [x] **B** Premia z kafelka: podpalenie przeciwnika lub rozbicie sąsiada. `Tiles/TileUpgradeLogics.cs:83` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_bonus_ignite.wav`, `Assets/_Project/Audio/SFX/Board/tile_bonus_neighbor_break.wav`
  - **Ocena użytkownika — `tile_bonus_ignite`:** [ignite](M3p_dobor_dzwiekow_v1.md#sfx-ignite) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `tile_bonus_neighbor_break`:** [tile_break](M3p_dobor_dzwiekow_v1.md#sfx-tile_break) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** przy uruchomieniu premii. **Czas SFX:** podpalenie 0,18–0,34 s; sąsiad 0,08–0,18 s. **Brzmienie:** Podpalenie: mały płomienny zapłon, nie pełny Burn tick; sąsiad: suchy kontakt i pęknięcie drugiego kafelka.

## Ataki umiejętności i statusy

- [x] **A** Zapowiedź planowanej akcji wroga: ostrzegawczy sygnał zamiaru. `Battle/EnemyBattleCharacter.cs:25` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_intent.wav`
  - **Ocena użytkownika — `enemy_intent`:** [enemy_intent](M3p_dobor_dzwiekow_v1.md#sfx-enemy_intent) — **Pasuje**, pełne źródło (0.99 s). <!-- USER-SFX-REVIEW -->
  - **Start:** gdy wróg ujawnia wybraną akcję. **Czas SFX:** 0,18–0,32 s; późniejsza pauza myślenia 0,35 s nie wyznacza długości cue. **Brzmienie:** Zwięzły napięty puls zamiaru, rozpoznawalny bez metalicznego alarmu.
- [x] **A** Zamach przed podstawowym atakiem gracza. `Battle/BattleManager.cs:620` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_attack_windup.wav`
  - **Ocena użytkownika — `player_attack_windup`:** [player_attack_windup](M3p_dobor_dzwiekow_v1.md#sfx-player_attack_windup) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** na początku zamachu. **Czas SFX:** 0,15–0,22 s; windup trwa 0,18 s. **Brzmienie:** Krótki ruch ciała i skupionej energii, zakończony przed wystrzałem.
- [x] **A** Wypuszczenie pocisku podstawowego ataku. `Battle/BattleManager.cs:630; UI/VFXManager.cs:426` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_projectile_launch.wav`
  - **Ocena użytkownika — `player_projectile_launch`:** [player_projectile_launch](M3p_dobor_dzwiekow_v1.md#sfx-player_projectile_launch) — **Pasuje**, pełne źródło (0.28 s). <!-- USER-SFX-REVIEW -->
  - **Start:** dokładnie przy wypuszczeniu pocisku. **Czas SFX:** 0,12–0,20 s; lot trwa max(0,12 s; odległość/32). **Brzmienie:** Zwięzły wydech energii z miejscem na oddzielny dźwięk trafienia; nie przykrywać całego lotu.
- [x] **A** Dolot pocisku i trafienie przeciwnika. `UI/VFXManager.cs:434` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_projectile_hit.wav`
  - **Ocena użytkownika — `enemy_projectile_hit`:** [enemy_projectile_hit](M3p_dobor_dzwiekow_v1.md#sfx-enemy_projectile_hit) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** w callbacku dolotu pocisku. **Czas SFX:** 0,20–0,40 s; punkt kontaktu zależy od odległości/32. **Brzmienie:** Zwarty fizyczny impuls z krótką magiczną reakcją celu, bez echa do następnego ataku.
- [x] **A** Trafienie gracza przez umiejętność wroga. `UI/VFXManager.cs:507` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_hit.wav`
  - **Ocena użytkownika — `player_hit`:** [player_hit](M3p_dobor_dzwiekow_v1.md#sfx-player_hit) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy pocisk/umiejętność dociera do gracza. **Czas SFX:** 0,20–0,40 s. **Brzmienie:** Cięższy, przytłumiony kontakt ciała i energii niż przy trafieniu wroga.
- [ ] **A** Trafienie w tarczę: osobna, twardsza warstwa uderzenia. `UI/BattleWorld.cs:136` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_hit.mp3`
  - **Start:** na kontakcie z aktywną tarczą. **Czas SFX:** 0,20–0,30 s; wizualny puls tarczy 0,22 s. **Brzmienie:** Gęsty, sprężysty impuls bariery bez stalowego brzęku.
- [x] **B** Pojawienie się i zniknięcie tarczy. `Battle/BattleCharacterShield.cs:94` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_appear.wav`, `Assets/_Project/Audio/SFX/Combat/shield_fade.wav`
  - **Ocena użytkownika — `shield_appear`:** [shield_appear](M3p_dobor_dzwiekow_v1.md#sfx-shield_appear) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `shield_fade`:** [shield_fade](M3p_dobor_dzwiekow_v1.md#sfx-shield_fade) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** gdy tarcza zaczyna się pojawiać lub zanikać. **Czas SFX:** po 0,20–0,30 s; fade wizualny 0,25 s. **Brzmienie:** Pojawienie: łagodne narastanie osłony; zniknięcie: miękkie opadnięcie energii.
- [ ] **A** Śmierć przeciwnika lub gracza: różne akcenty finału. `UI/BattleWorld.cs:136` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_death.mp3`, `Assets/_Project/Audio/SFX/Combat/player_death.mp3`
  - **Start:** przy animacji śmierci, po ostatnim trafieniu. **Czas SFX:** wróg 0,50–0,85 s; gracz 0,65–1,00 s; długości animacji do pomiaru. **Brzmienie:** Wróg: zanik ciężaru i energii; gracz: cięższy, bardziej osobisty finał. Nie dublować uderzenia pocisku.
- [ ] **A** Rzucenie umiejętności gracza i wroga: brzmienie przypisane do typu umiejętności. `Battle/BattleManager.cs:177; UI/BattleWorld.cs:120` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_skill_cast.wav`, `Assets/_Project/Audio/SFX/Combat/enemy_skill_cast.mp3`
  - **Ocena użytkownika — `player_skill_cast`:** [resolve_start](M3p_dobor_dzwiekow_v1.md#sfx-resolve_start) — **Pasuje**, pełne źródło (0.88 s). <!-- USER-SFX-REVIEW -->
  - **Start:** przy użyciu umiejętności, przed ewentualnym dolotem. **Czas SFX:** 0,18–0,38 s na wspólny cast; dalsze trafienie osobno. **Brzmienie:** Sygnatura magii lub ruchu klasy z odrębnym akcentem konkretnej umiejętności; bez metalicznych dzwonków.
- [x] **B** Kolejne trafienia umiejętności wielokrotnego ataku. `BattleEffect/SkillEffectLogics.cs:314` **SFX:** `Assets/_Project/Audio/SFX/Combat/multi_hit_tick.wav`
  - **Ocena użytkownika — `multi_hit_tick`:** [multi_hit_tick](M3p_dobor_dzwiekow_v1.md#sfx-multi_hit_tick) — **Pasuje**, wycinek (0.11 s). <!-- USER-SFX-REVIEW -->
  - **Start:** na kolejnych trafieniach, jeśli są wizualnie rozdzielone. **Czas SFX:** 0,07–0,15 s na trafienie; obecna logika nalicza je synchronicznie. **Brzmienie:** Małe różniące się teksturą uderzenia. Bez osobnego rytmu, dopóki animacja nie rozdziela trafień w czasie.
- [ ] **B** Leczenie oraz regeneracja przy turze: odrębne akcenty. `BattleEffect/HealLogic.cs:25; Battle/BattleCharacter.cs:313` **SFX:** `Assets/_Project/Audio/SFX/Combat/healing.mp3`, `Assets/_Project/Audio/SFX/Combat/regeneration_tick.mp3`
  - **Start:** na rzeczywistym wzroście HP. **Czas SFX:** leczenie 0,30–0,55 s; tick regeneracji 0,16–0,30 s. **Brzmienie:** Leczenie: ciepły przepływ z miękkim domknięciem; regeneracja: pojedynczy mniejszy puls, nie dźwięk przez wszystkie tury statusu.
- [x] **B** Nałożenie tarczy lub efektu obronnego. `BattleEffect/AddShieldLogic.cs:25` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_apply.wav`
  - **Ocena użytkownika — `shield_apply`:** [shield_appear](M3p_dobor_dzwiekow_v1.md#sfx-shield_appear) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili przyznania ochrony. **Czas SFX:** 0,22–0,38 s; osobny SFX pojawienia tarczy trwa ok. 0,25 s. **Brzmienie:** Gęstniejąca warstwa ochrony, z miejscem na krótką reakcję widocznej tarczy.
- [ ] **B** Nałożenie, wzmocnienie i wygaśnięcie statusu. `Battle/BattleCharacter.cs:224; Battle/BattleCharacter.cs:282` **SFX:** `Assets/_Project/Audio/SFX/Combat/status_apply.mp3`, `Assets/_Project/Audio/SFX/Combat/status_strengthen.mp3`, `Assets/_Project/Audio/SFX/Combat/status_expire.mp3`
  - **Start:** przy zmianie stanu statusu. **Czas SFX:** nałożenie 0,14–0,26 s; wzmocnienie 0,18–0,30 s; wygaśnięcie 0,14–0,26 s. **Brzmienie:** Jedna rodzina miękkich run: znak, pełniejszy puls i ciche rozpuszczenie; długość w turach nie określa długości klipu.
- [x] **B** Obrażenia od podpalenia na początku tury. `Battle/BattleCharacter.cs:313; UI/BattleWorld.cs:183` **SFX:** `Assets/_Project/Audio/SFX/Combat/burn_tick_timed.wav`
  - **Ocena użytkownika — `burn_tick_timed`:** [burn_tick](M3p_dobor_dzwiekow_v1.md#sfx-burn_tick) — **Pasuje**, wersja niewybrana; [fire_alt](M3p_dobor_dzwiekow_v1.md#sfx-fire_alt) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** na ticku obrażeń i ponownym uruchomieniu płomienia. **Czas SFX:** 0,90 s w aktywnym WAV; cząstki żyją 0,5–1 s, emisja gracza trwa 1 s. **Brzmienie:** Szybki ciepły zapłon i organiczne trzaski, potem cichy ogon; status trwa 2 tury, lecz cue gra tylko raz na tick.
- [ ] **B** Przemiana many, poświęcenie HP, odzyskanie karty i zebranie dusz. `BattleEffect/SkillEffectLogics.cs:375` **SFX:** `Assets/_Project/Audio/SFX/Combat/mana_transmute.mp3`, `Assets/_Project/Audio/SFX/Combat/hp_sacrifice.wav`, `Assets/_Project/Audio/SFX/Combat/card_recover.mp3`, `Assets/_Project/Audio/SFX/Combat/soul_collect.mp3`
  - **Ocena użytkownika — `hp_sacrifice`:** [enemy_intent](M3p_dobor_dzwiekow_v1.md#sfx-enemy_intent) — **Pasuje**, pełne źródło (0.99 s). <!-- USER-SFX-REVIEW -->
  - **Start:** na potwierdzonym skutku każdej z czterech akcji. **Czas SFX:** transmutacja 0,30–0,50 s; ofiara HP 0,18–0,34 s; odzysk karty 0,18–0,32 s; dusze 0,30–0,55 s. **Brzmienie:** Transmutacja: przepływ między barwami; HP: niski puls utraty; karta: odwrócony szelest; dusze: ciche zbliżające się oddechy energii.

## Wynik walki i rozwój postaci

- [ ] **A** Zwycięstwo lub porażka: dwa wyraźne sygnały końca walki. `Battle/BattleManager.cs:823; UI/UIEndBattlePanel.cs:92` **SFX:** `Assets/_Project/Audio/SFX/Rewards/victory.mp3`, `Assets/_Project/Audio/SFX/Rewards/defeat.mp3`
  - **Start:** po ustaleniu wyniku walki. **Czas SFX:** zwycięstwo 0,70–1,15 s; porażka 0,60–1,00 s; animację panelu zmierzyć. **Brzmienie:** Zwycięstwo: ciepłe rozjaśnienie i mocny finał; porażka: opadnięcie energii bez przesadnej dramatycznej fanfary.
- [ ] **B** Pojawienie się nagród i odłamków po walce. `UI/UIEndBattlePanel.cs:112` **SFX:** `Assets/_Project/Audio/SFX/Rewards/battle_rewards_reveal.mp3`
  - **Start:** gdy panel odsłania nagrody. **Czas SFX:** 0,35–0,65 s; liczba nagród jest zmienna. **Brzmienie:** Kilka delikatnych pojawień i jedno wspólne osadzenie, bez odtwarzania dźwięku przez cały czas oglądania panelu.
- [ ] **B** Napełnianie paska doświadczenia. `UI/UIEndBattlePanel.cs:289` **SFX:** `Assets/_Project/Audio/SFX/Rewards/experience_fill.mp3`
  - **Start:** na początku każdego odcinka animacji paska. **Czas SFX:** dynamicznie 0,15–0,90 s na odcinek; pauza przy poziomie 0,15 s. **Brzmienie:** Cicha płynąca energia, która ustaje dokładnie z paskiem; poziom ma osobny akcent, więc nie przedłużać stałego one-shotu.
- [x] **B** Awans poziomu i pokazanie punktów statystyk. `UI/UIEndBattlePanel.cs:315` **SFX:** `Assets/_Project/Audio/SFX/Rewards/level_up.wav`
  - **Ocena użytkownika — `level_up`:** [craft_magic](M3p_dobor_dzwiekow_v1.md#sfx-craft_magic) — **Pasuje**, pełne źródło (3.19 s). <!-- USER-SFX-REVIEW -->
  - **Start:** w chwili osiągnięcia nowego poziomu. **Czas SFX:** 0,45–0,80 s; początek po zakończeniu odcinka paska. **Brzmienie:** Jedno wyraźne, ciepłe rozbłysnięcie i krótkie ukazanie nowych punktów, bez metalicznych dzwonków.
- [x] **B** Zamknięcie wyniku walki oraz powrót na mapę. `UI/UIEndBattlePanel.cs:214; Battle/BattleManager.cs:855` **SFX:** `Assets/_Project/Audio/SFX/Rewards/battle_result_close.wav`
  - **Ocena użytkownika — `battle_result_close`:** [scene_transition](M3p_dobor_dzwiekow_v1.md#sfx-scene_transition) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po zamknięciu wyniku, przy rozpoczęciu powrotu. **Czas SFX:** 0,20–0,38 s; czas ładowania mapy jest zmienny. **Brzmienie:** Miękki gest zamknięcia panelu i krótki oddech przejścia; nowa scena może mieć własny akcent.
- [ ] **B** Pokazanie nagrody po bossie i wybór nowej umiejętności. `Battle/BattleManager.cs:891; UI/UIPanelGainSkill.cs:134` **SFX:** `Assets/_Project/Audio/SFX/Rewards/boss_reward_reveal.mp3`, `Assets/_Project/Audio/SFX/Rewards/new_skill_choose.mp3`
  - **Start:** przy odsłonięciu nagrody oraz po wyborze umiejętności. **Czas SFX:** nagroda 0,50–0,90 s; wybór 0,14–0,28 s. **Brzmienie:** Nagroda: głębszy magiczny rozkwit; wybór: zwięzłe potwierdzenie bez ponownej fanfary.
- [ ] **B** Dodanie lub usunięcie umiejętności z wyposażenia. `UI/UIPanelChooseSkill.cs:165` **SFX:** `Assets/_Project/Audio/SFX/Rewards/skill_equip.mp3`, `Assets/_Project/Audio/SFX/Rewards/skill_unequip.mp3`
  - **Start:** po zatwierdzeniu wyposażenia lub zdjęcia. **Czas SFX:** każdy 0,11–0,22 s. **Brzmienie:** Miękkie wsunięcie energii w slot; usunięcie odwraca jego kontur.
- [x] **B** Dodanie lub usunięcie karty z talii. `UI/UIPanelPlayerCards.cs:304` **SFX:** `Assets/_Project/Audio/SFX/Rewards/deck_card_add.wav`, `Assets/_Project/Audio/SFX/Rewards/deck_card_remove.wav`
  - **Ocena użytkownika — `deck_card_add`:** [single_card_draw](M3p_dobor_dzwiekow_v1.md#sfx-single_card_draw) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `deck_card_remove`:** [card_deselect](M3p_dobor_dzwiekow_v1.md#sfx-card_deselect) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po dodaniu lub usunięciu karty z talii. **Czas SFX:** każdy 0,11–0,22 s. **Brzmienie:** Pojedynczy ruch grubego papieru w stosie; usunięcie ma ruch przeciwny.
- [ ] **C** Zmiana filtra i strony listy kart. `UI/UIPanelPlayerCards.cs:79` **SFX:** `Assets/_Project/Audio/SFX/Rewards/card_filter_change.mp3`, `Assets/_Project/Audio/SFX/Rewards/card_page_change.wav`
  - **Ocena użytkownika — `card_page_change`:** [panel_open](M3p_dobor_dzwiekow_v1.md#sfx-panel_open) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Start:** po zmianie filtra lub strony. **Czas SFX:** filtr 0,07–0,14 s; strona 0,15–0,28 s. **Brzmienie:** Filtr: dyskretny impuls UI; strona: krótki szelest przewróconego arkusza.
- [ ] **B** Dodanie, cofnięcie i zatwierdzenie punktów statystyki. `UI/UIPanelPlayerStats.cs:151` **SFX:** `Assets/_Project/Audio/SFX/Rewards/stat_point_add.mp3`, `Assets/_Project/Audio/SFX/Rewards/stat_point_undo.mp3`, `Assets/_Project/Audio/SFX/Rewards/stat_points_confirm.mp3`
  - **Start:** po dodaniu, cofnięciu lub zatwierdzeniu punktów. **Czas SFX:** dodanie/cofnięcie 0,08–0,16 s; zatwierdzenie 0,18–0,30 s. **Brzmienie:** Małe miękkie impulsy o przeciwnym kierunku; końcowe zatwierdzenie pełniejsze, ale krótkie.
- [x] **B** Wybór projektu karty i udane stworzenie karty. `UI/UIPanelCardCrafting.cs:177; UI/UIPanelCardCrafting.cs:295` **SFX:** `Assets/_Project/Audio/SFX/Rewards/card_recipe_select.wav`, `Assets/_Project/Audio/SFX/Rewards/card_craft_success.wav`
  - **Ocena użytkownika — `card_recipe_select`:** [card_select](M3p_dobor_dzwiekow_v1.md#sfx-card_select) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `card_craft_success`:** [craft_magic](M3p_dobor_dzwiekow_v1.md#sfx-craft_magic) — **Pasuje**, pełne źródło (3.19 s). <!-- USER-SFX-REVIEW -->
  - **Start:** po wyborze projektu i po udanym craftingu. **Czas SFX:** wybór 0,10–0,20 s; sukces 0,38–0,65 s. **Brzmienie:** Wybór: dotyk papierowego projektu; sukces: szelest składania i mały magiczny finał.
- [ ] **B** Wybór typu kafelka, ulepszenia i slotu oraz usunięcie ulepszenia. `UI/UIPanelTileCrafting.cs:221` **SFX:** `Assets/_Project/Audio/SFX/Rewards/tile_type_select.wav`, `Assets/_Project/Audio/SFX/Rewards/tile_upgrade_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/tile_slot_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/tile_upgrade_remove.mp3`
  - **Ocena użytkownika — `tile_type_select`:** [map_node_select](M3p_dobor_dzwiekow_v1.md#sfx-map_node_select) — **Pasuje**, wersja niewybrana. <!-- USER-SFX-REVIEW -->
  - **Ocena użytkownika — `tile_slot_select`:** [map_token_step](M3p_dobor_dzwiekow_v1.md#sfx-map_token_step) — **Odrzucone**. <!-- USER-SFX-REVIEW -->
  - **Start:** po zaakceptowaniu każdej zmiany w craftingu kafelka. **Czas SFX:** wybory 0,08–0,18 s; usunięcie 0,12–0,24 s. **Brzmienie:** Typ: kamienny dotyk; ulepszenie: miękka runa; slot: osadzenie; usunięcie: cichy odpływ energii.
- [x] **B** Udane stworzenie kafelka. `UI/UIPanelTileCrafting.cs:416` **SFX:** `Assets/_Project/Audio/SFX/Rewards/tile_craft_success.wav`
  - **Ocena użytkownika — `tile_craft_success`:** [craft_magic](M3p_dobor_dzwiekow_v1.md#sfx-craft_magic) — **Pasuje**, pełne źródło (3.19 s). <!-- USER-SFX-REVIEW -->
  - **Start:** gdy gotowy kafelek pojawia się po stworzeniu. **Czas SFX:** 0,38–0,65 s; animację craftingu zmierzyć. **Brzmienie:** Kamienny kontakt, nasycenie runy i krótkie ciepłe zakończenie bez uderzenia w metal.
- [ ] **C** Najechanie na kartę, umiejętność lub element z opisem: bardzo subtelny sygnał UI. `UI/UIBoardActionCard.cs:106; UI/UISkillButton.cs:104` **SFX:** `Assets/_Project/Audio/SFX/Rewards/ui_hover.mp3`
  - **Start:** po rozpoczęciu hover, tylko gdy podpowiedź rzeczywiście pomaga. **Czas SFX:** 0,05–0,10 s; odtwarzacz ogranicza cue do odstępów 0,08 s. **Brzmienie:** Prawie niesłyszalny miękki dotyk UI, bez tonu utrzymującego się podczas hover.

## Umiejętności wymagające palety brzmień

Po dodaniu nowych klipów aktywacja umiejętności może korzystać ze wspólnej warstwy klasy i akcentu umiejętności odtwarzanych równocześnie przez `SfxPlayer.PlaySkill`. Teraz biblioteka jest pusta i te wywołania pozostają nieme. Definicje działań znajdują się w `Assets/_Project/Configs/Skills`. Czasy poniżej dotyczą akcentu przy użyciu umiejętności; osobne SFX wystrzału, trafienia, statusu i regeneracji opisano wyżej. W obecnym `BattleManager.ExecuteSkill` efekt logiczny jest liczony przed uruchomieniem animacji, dlatego późniejsze dźwięki kontaktu należy wiązać z callbackiem VFX, a nie z chwilą naliczenia obrażeń.

<!-- GENERATED-SKILL-SFX-START -->
| Grupa | Umiejętność / warstwa | SFX | Start i czas SFX | Opis brzmienia |
| --- | --- | --- | --- | --- |
| Podstawowe | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Basic/basic_layer.mp3` | Przy użyciu, 0,12–0,20 s | Delikatny wspólny impuls energii; zostawia miejsce na akcent konkretnej umiejętności. |
| Podstawowe | BasicAttackSkill | `Assets/_Project/Audio/SFX/Skills/Basic/basic_attack_skill.wav` | Przy użyciu, 0,15–0,25 s | Krótki ruch fizycznego ataku; kontakt celu pozostaje osobnym SFX. |
| Podstawowe | BasicHeal | `Assets/_Project/Audio/SFX/Skills/Basic/basic_heal.mp3` | Przy leczeniu, 0,25–0,45 s | Ciepłe skupienie energii i miękkie rozjaśnienie HP. |
| Podstawowe | GuardUp | `Assets/_Project/Audio/SFX/Skills/Basic/guard_up.wav` | Przy podniesieniu gardy, 0,20–0,35 s | Gęstnienie ochrony i spokojne osadzenie postawy, bez stalowego brzęku. |
| Podstawowe | Ignite | `Assets/_Project/Audio/SFX/Skills/Basic/ignite.wav` | Przy nałożeniu ognia, 0,18–0,32 s | Mały zapłon na celu; późniejsze ticki Burn mają własny dźwięk. |
| Podstawowe | ShieldBash | `Assets/_Project/Audio/SFX/Skills/Basic/shield_bash.mp3` | Przy zamachu, 0,18–0,32 s | Krótki cięższy pchnięty ruch tarczy; trafienie oddzielić na kontakt celu. |
| Wrogowie | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_layer.wav` | Przy użyciu, 0,12–0,22 s | Ciemniejszy, organiczny impuls gotowości wroga; niższy od właściwego ataku. |
| Wrogowie | EnemyEmber | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_ember.wav` | Przy rzuceniu iskry, 0,18–0,32 s | Suchy wyrzut żaru; kontakt ognistego pocisku oddzielnie przy dolocie. |
| Wrogowie | EnemyGuard | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_guard.wav` | Przy podniesieniu obrony, 0,20–0,35 s | Ciężka, matowa warstwa ochrony bez metalicznego rezonansu. |
| Wrogowie | EnemySlam | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_slam.mp3` | Przy zamachu, 0,22–0,40 s | Niski ruch ciężaru do uderzenia; szczyt uderzenia należy do SFX trafienia. |
| Wrogowie | EnemyStrike | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_strike.wav` | Przy zamachu, 0,14–0,26 s | Szybki, szorstki ruch broni; bez długiego ogona po kontakcie. |
| Mag | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Mage/mage_layer.mp3` | Przy użyciu, 0,14–0,24 s | Cichy oddech skupianej many, bez jasnych dzwonków. |
| Mag | ChromaticBolt | `Assets/_Project/Audio/SFX/Skills/Mage/chromatic_bolt.wav` | Przy wystrzale, 0,18–0,30 s | Zwarty wielobarwny impuls energii; obrażenia skalują się kolorami, ale nie wymagają osobnego dźwięku na kolor. |
| Mag | Convergence | `Assets/_Project/Audio/SFX/Skills/Mage/convergence.mp3` | Przy użyciu, 0,38–0,62 s | Trzy subtelne strumienie zbiegają się w jeden puls obrażeń, leczenia i osłony. |
| Mag | FireSpark | `Assets/_Project/Audio/SFX/Skills/Mage/fire_spark.wav` | Przy rzuceniu, 0,16–0,30 s | Mała sucha iskra ognia; status Iskry działa przez 4 tury bez ciągłego dźwięku. |
| Mag | IceWard | `Assets/_Project/Audio/SFX/Skills/Mage/ice_ward.wav` | Przy utworzeniu ochrony, 0,28–0,48 s | Miękkie krystalizowanie chłodnej osłony i krótki niski koniec, bez szklanego dzwonka. |
| Mag | IgniteBurst | `Assets/_Project/Audio/SFX/Skills/Mage/ignite_burst.wav` | Przy zużyciu stacków Burn, 0,32–0,55 s | Skupione płomienie zbierają się i szybko pękają; siłę można skalować liczbą stacków. |
| Mag | Kindle | `Assets/_Project/Audio/SFX/Skills/Mage/kindle.wav` | Przy nałożeniu Burn, 0,18–0,34 s | Dwie małe fale żaru bez uderzenia obrażeń, bo akcja tylko nakłada status. |
| Mag | LightningChain | `Assets/_Project/Audio/SFX/Skills/Mage/lightning_chain.wav` | Przy wyzwoleniu, 0,22–0,42 s | Jedno zwarte wyładowanie z subtelnymi dodatkowymi impulsami zależnymi od poprzedniej kaskady; bez udawania wielu celów. |
| Mag | Renew | `Assets/_Project/Audio/SFX/Skills/Mage/renew.mp3` | Przy nałożeniu regeneracji, 0,22–0,38 s | Ciepły puls odnowy; cztery późniejsze ticki leczenia mają oddzielny krótki sygnał. |
| Mag | TransmuteMana | `Assets/_Project/Audio/SFX/Skills/Mage/transmute_mana.mp3` | Przy zatwierdzonej zamianie koloru, 0,24–0,42 s | Płynne przelanie do trzech punktów many między dwiema barwami. |
| Mag | Wildfire | `Assets/_Project/Audio/SFX/Skills/Mage/wildfire.wav` | Przy rzuceniu, 0,28–0,48 s | Szeroki, lecz krótki podmuch ognia; natychmiastowy tick Burn brzmi osobno i nie powinien maskować rzutu. |
| Wojownik | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Warrior/warrior_layer.mp3` | Przy użyciu, 0,12–0,22 s | Matowy ruch skóry, ciężaru i oddechu postawy; bez metalicznych dzwonków. |
| Wojownik | BattleFrenzy | `Assets/_Project/Audio/SFX/Skills/Warrior/battle_frenzy.mp3` | Przy użyciu, 0,18–0,34 s | Narastający rytm oddechu i impetu; wcześniejsze Resolve zwiększają siłę, nie długość cue. |
| Wojownik | Execute | `Assets/_Project/Audio/SFX/Skills/Warrior/execute.wav` | Przy zamachu, 0,20–0,36 s | Decydujący ciężki ruch; mocniejszy wariant przy niskim HP wroga, sam kontakt oddzielnie. |
| Wojownik | Fortress | `Assets/_Project/Audio/SFX/Skills/Warrior/fortress.mp3` | Przy nałożeniu ochrony, 0,24–0,42 s | Warstwy masywnej osłony domykają się w jednym gęstym pulsie; liczba grup zwiększa siłę tarczy, nie czas. |
| Wojownik | GuardStance | `Assets/_Project/Audio/SFX/Skills/Warrior/guard_stance.mp3` | Przy ustawieniu gardy, 0,18–0,34 s | Stabilny krok i miękkie zaryglowanie osłony bez ostrego rezonansu. |
| Wojownik | HeavySlash | `Assets/_Project/Audio/SFX/Skills/Warrior/heavy_slash.wav` | Przy zamachu, 0,18–0,30 s | Szeroki ciężki ruch ostrza przez powietrze; osobny SFX trafienia po dolocie VFX. |
| Wojownik | PerfectStrike | `Assets/_Project/Audio/SFX/Skills/Warrior/perfect_strike.mp3` | Przy precyzyjnym zamachu, 0,14–0,26 s | Bardzo skupiony, szybki ruch; udane pojedyncze dopasowanie może wzmocnić akcent trafienia. |
| Wojownik | Riposte | `Assets/_Project/Audio/SFX/Skills/Warrior/riposte.mp3` | Przy przygotowaniu kontry, 0,18–0,32 s | Cicha napięta garda teraz, a kontratak dopiero na następnym wrogim trafieniu. |
| Wojownik | ShieldSlam | `Assets/_Project/Audio/SFX/Skills/Warrior/shield_slam.wav` | Przy pchnięciu tarczą, 0,20–0,36 s | Krótki ciężki ruch zużywający osłonę; wielkość tarczy może sterować masą, nie długością. |
| Wojownik | Unyielding | `Assets/_Project/Audio/SFX/Skills/Warrior/unyielding.mp3` | Przy wzmocnieniu obrony, 0,22–0,40 s | Niski, spokojny puls wytrzymałości; późniejszy wzmocniony atak ma własne SFX. |
| Wojownik | WideSwing | `Assets/_Project/Audio/SFX/Skills/Warrior/wide_swing.wav` | Przy szerokim zamachu, 0,20–0,38 s | Jeden łuk ruchu; liczba grup z ostatniego Resolve wyznacza trafienia, których nie należy wciskać w jeden cast. |
| Cień | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Shadow/shadow_layer.mp3` | Przy użyciu, 0,10–0,20 s | Cichy ruch powietrza i przytłumionej energii, bez szklistych tonów. |
| Cień | Backstab | `Assets/_Project/Audio/SFX/Skills/Shadow/backstab.wav` | Przy nagłym ataku, 0,12–0,24 s | Krótki suchy gest sztyletu; gdy wróg zapowiada ofensywę, kontakt może być mocniejszy. |
| Cień | BladeDance | `Assets/_Project/Audio/SFX/Skills/Shadow/blade_dance.mp3` | Przy użyciu, 0,18–0,36 s | Zwinny wir kilku lekkich ruchów; liczba kart skaluje obrażenia, nie narzuca serii osobnych trafień. |
| Cień | BloodPact | `Assets/_Project/Audio/SFX/Skills/Shadow/blood_pact.mp3` | Przy utracie HP i zyskaniu AP, 0,22–0,38 s | Niski organiczny puls ofiary przechodzący w energię działania; bez głosu lub bólu postaci. |
| Cień | ExposeWeakness | `Assets/_Project/Audio/SFX/Skills/Shadow/expose_weakness.mp3` | Przy oznaczeniu celu, 0,18–0,32 s | Cienka szczelina w ochronie i cichy mroczny znak; trzy przyszłe trafienia nie brzmią teraz. |
| Cień | KillingSequence | `Assets/_Project/Audio/SFX/Skills/Shadow/killing_sequence.mp3` | Przy użyciu, 0,22–0,40 s | Skupiona seria gestów kończąca się jednym akcentem; liczba kart skaluje obrażenia, nie tworzy automatycznie osobnych uderzeń. |
| Cień | Momentum | `Assets/_Project/Audio/SFX/Skills/Shadow/momentum.mp3` | Przy dobraniu kart, 0,16–0,30 s | Szybki przepływ energii w ruch karty; jeśli dobrane są dwie, drugi lekki szelest kart osobno. |
| Cień | QuickCut | `Assets/_Project/Audio/SFX/Skills/Shadow/quick_cut.mp3` | Przy użyciu, 0,14–0,28 s | Dwa krótkie cięcia z bliskim odstępem tylko gdy animacja rozdziela trafienia; kod liczy je synchronicznie. |
| Cień | Recycle | `Assets/_Project/Audio/SFX/Skills/Shadow/recycle.mp3` | Przy odzyskaniu karty, 0,18–0,32 s | Odwrócony ruch papieru przez cień; oszczędnie, by nie dublować osobnego CardRecover. |
| Cień | ShadowHarvest | `Assets/_Project/Audio/SFX/Skills/Shadow/shadow_harvest.mp3` | Przy zebraniu dusz, 0,22–0,42 s | Ciche smugi energii zbiegają się w jeden zbiór; liczba zniszczonych kafelków reguluje intensywność. |
| Cień | SoulBlade | `Assets/_Project/Audio/SFX/Skills/Shadow/soul_blade.mp3` | Przy aktywacji, 0,22–0,42 s | Matowa energia dusz nasyca ostrze; do czterech trafień wymaga osobnych, krótkich akcentów tylko po rozdzieleniu ich w czasie. |
<!-- GENERATED-SKILL-SFX-END -->
<!-- USER-SFX-REVIEWS-START -->

### Oceny propozycji umiejętności

| Nazwa SFX | Ocena i wybrana wersja |
| --- | --- |
| `basic_attack_skill` | [player_attack_windup](M3p_dobor_dzwiekow_v1.md#sfx-player_attack_windup) — **Pasuje**, wersja niewybrana |
| `guard_up` | [shield_appear](M3p_dobor_dzwiekow_v1.md#sfx-shield_appear) — **Pasuje**, wersja niewybrana |
| `ignite` | [ignite](M3p_dobor_dzwiekow_v1.md#sfx-ignite) — **Pasuje**, wersja niewybrana |
| `enemy_layer` | [enemy_intent](M3p_dobor_dzwiekow_v1.md#sfx-enemy_intent) — **Pasuje**, pełne źródło (0.99 s) |
| `enemy_ember` | [ignite](M3p_dobor_dzwiekow_v1.md#sfx-ignite) — **Pasuje**, wersja niewybrana |
| `enemy_guard` | [shield_appear](M3p_dobor_dzwiekow_v1.md#sfx-shield_appear) — **Pasuje**, wersja niewybrana |
| `enemy_strike` | [player_attack_windup](M3p_dobor_dzwiekow_v1.md#sfx-player_attack_windup) — **Pasuje**, wersja niewybrana |
| `chromatic_bolt` | [resolve_start](M3p_dobor_dzwiekow_v1.md#sfx-resolve_start) — **Pasuje**, pełne źródło (0.88 s) |
| `fire_spark` | [ignite](M3p_dobor_dzwiekow_v1.md#sfx-ignite) — **Pasuje**, wersja niewybrana |
| `ice_ward` | [ice_ward](M3p_dobor_dzwiekow_v1.md#sfx-ice_ward) — **Pasuje**, wersja niewybrana |
| `ignite_burst` | [wildfire](M3p_dobor_dzwiekow_v1.md#sfx-wildfire) — **Pasuje**, wersja niewybrana |
| `kindle` | [ignite](M3p_dobor_dzwiekow_v1.md#sfx-ignite) — **Pasuje**, wersja niewybrana |
| `lightning_chain` | [lightning_chain](M3p_dobor_dzwiekow_v1.md#sfx-lightning_chain) — **Pasuje**, wersja niewybrana |
| `wildfire` | [wildfire](M3p_dobor_dzwiekow_v1.md#sfx-wildfire) — **Pasuje**, wersja niewybrana |
| `execute` | [heavy_slash](M3p_dobor_dzwiekow_v1.md#sfx-heavy_slash) — **Pasuje**, wersja niewybrana |
| `heavy_slash` | [heavy_slash](M3p_dobor_dzwiekow_v1.md#sfx-heavy_slash) — **Pasuje**, wersja niewybrana |
| `shield_slam` | [heavy_slash](M3p_dobor_dzwiekow_v1.md#sfx-heavy_slash) — **Pasuje**, wersja niewybrana |
| `wide_swing` | [heavy_slash](M3p_dobor_dzwiekow_v1.md#sfx-heavy_slash) — **Pasuje**, wersja niewybrana |
| `backstab` | [player_attack_windup](M3p_dobor_dzwiekow_v1.md#sfx-player_attack_windup) — **Pasuje**, wersja niewybrana |

<!-- USER-SFX-REVIEWS-END -->

## Uwagi do wdrożenia

- **Dopasowania:** `TileDestroyed` uruchamia się osobno dla każdego kafelka, a `MatchWaveCompleted` raz na falę. Oba SFX są już wyzwalane; pojedyncze pęknięcia muszą pozostać krótkie i ciche wobec akcentu fali.
- **Trafienia:** obrażenia są liczone przed końcem animacji pocisku. Obecny dźwięk uderzenia jest wyzwalany w callbacku `onArrived` w `VFXManager`, razem z reakcją postaci; przy wymianie próbki należy zachować ten punkt synchronizacji.
- **Interfejs:** klik zatwierdzenia odtwarzaj po przyjęciu akcji. Nieudany wybór może mieć osobny, cichy sygnał tylko wtedy, gdy ekran pokazuje graczowi powód odmowy.
- **Karta Rewind:** logika tej karty ma `IsAvailable = false` w `Cards/BoardActionsSpecial.cs:216`. Nie wymaga jeszcze SFX w działającej pętli gry.
