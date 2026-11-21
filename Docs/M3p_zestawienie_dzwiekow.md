# M3p zestawienie potrzebnych dźwięków

<!-- ACCEPTED-SFX-INTEGRATION-2026-11-20 -->
## Wdrożenie 20.11.2026

**Aktualnie w grze jest 108 klipów z zaakceptowanych kolekcji.** W ostatnim wdrożeniu dodano 3 kolejne pliki, zachowując wszystkie dotychczasowe 105 klipów, ich GUID-y i pięć wcześniejszych korekt tempa lub fragmentu. Unity potwierdziło komplet 108 referencji oraz podłączenie biblioteki do GameConfig; biblioteka ma 109 zdarzeń i 68 wpisów umiejętności. [Raport ostatniego wdrożenia](AudioGeneration/2026-11-20/approved-update-03/unity_integration_report.json) · [Niezależna weryfikacja](AudioGeneration/2026-11-20/approved-update-03/unity_merge_verification.json).

Pierwsza partia obejmowała **83 zaakceptowane klipy**: 41 zdarzeń, 39 akcentów umiejętności i 3 warstwy klas. Zachowano pięć zapisanych korekt tempa i fragmentu. Ówczesna weryfikacja potwierdziła 83 referencje, 96 zdarzeń i 60 wpisów umiejętności, w tym wpisy samych warstw.

[Szczegóły wdrożenia](AudioGeneration/2026-11-20/accepted-set-01/UnityIntegration.md) · [Raport Unity](AudioGeneration/2026-11-20/accepted-set-01/unity_integration_report.json). Dalsze tabele są historycznym planem z 27.10.2026.

Pozostałe **27 pozycji** otrzymało nowe, krótkie prompty i próbki v3 o długości 1–2 s, w tym nowe koncepcje siedmiu wskazanych efektów. Nowe propozycje czekają na ocenę w [panelu porównawczym](AudioGeneration/2026-11-20/remaining-variants-01/review.html); [lista promptów i długości](AudioGeneration/2026-11-20/remaining-variants-01/Prompty.md). Wszystkie 83 zaakceptowane pliki zachowały identyczne SHA-256.

W kolejnym eksporcie z tego zestawu oceniono **9 jako „Pasuje”, 5 jako „Do poprawy”, 13 pozostało bez oceny**. Wybrane fragmenty obejmują pełne nagrania w tempie 1×. Dziewięć zaakceptowanych plików zebrano osobno w [accepted-set-02](AudioGeneration/2026-11-20/accepted-set-02/collection_manifest.json), co daje łącznie 92 zaakceptowane efekty.

Zgodnie z nową instrukcją wygenerowano także warianty v3 z promptami opisującymi konkretne zdarzenia w grze. [Nowy panel](AudioGeneration/2026-11-20/event-prompts-01/review.html) domyślnie pokazuje **18 pozostałych pozycji**, zachowując ocenę dziewięciu poprzednich wariantów; [opisy zdarzeń i prompty](AudioGeneration/2026-11-20/event-prompts-01/Prompty.md).

Po kolejnym eksporcie i uwagach użytkownika klip „Pojawienie się nagród i odłamków po walce” przypisano do **„Premia kafelka: punkt akcji”** (`TileBonusAction`, ID 56). Unity potwierdziło nowe przypisanie i zachowanie dotychczasowych 83 plików; biblioteka zawiera obecnie 84 klipy z zaakceptowanej kolekcji. Samo `battle_rewards_reveal` pozostaje **„Do poprawy”** i bez nowego przypisania w grze. [Raport przypisania](AudioGeneration/2026-11-20/review-adjustments-01/unity_mapping_report.json).

Nowy `lick_wounds` wygenerowano z promptu **„A wounded beast licking its fur with soft wet smacking sounds.”**. Zestaw po połączeniu ocen i zmiany przypisania obejmuje 21 zaakceptowanych propozycji z ostatnich 27 pozycji oraz 6 pozostałych: 5 do poprawy i nowy `lick_wounds` do oceny. Wraz z pierwszą kolekcją daje to 104 zaakceptowane efekty. [Aktualny panel po uwagach](AudioGeneration/2026-11-20/review-adjustments-01/review.html).

Dla pięciu pozycji „Do poprawy” wygenerowano kolejne próbki v3: `shard_collect`, `battle_rewards_reveal`, `skill_unequip`, `enemy_death` i `defensive_stance`. Nowe krótkie prompty opisują konkretne zdarzenia w grze, a próbki trwają 1–1,5 s. Wszystkie nowe warianty czekają na ocenę w [panelu pięciu poprawek](AudioGeneration/2026-11-20/event-revisions-02/review.html), z [listą promptów](AudioGeneration/2026-11-20/event-revisions-02/Prompty.md). Zachowano przypisanie dotychczasowego klipu nagród do punktu akcji oraz wszystkie zaakceptowane wersje.

Najnowszy eksport ocen zestawu 02 zawiera 1 „Pasuje”, 3 „Do poprawy” i 1 bez oceny. Zaakceptowano nowy `battle_rewards_reveal` i wdrożono go wraz z wcześniej zaakceptowanymi plikami; `tile_bonus_action` nadal używa swojego osobnego, wcześniej wybranego klipu. Dla `shard_collect`, `enemy_death` i `defensive_stance` wygenerowano kolejne prompty i próbki v3. [Aktualny panel pięciu pozostałych pozycji](AudioGeneration/2026-11-20/event-revisions-03/review.html) zawiera **3 nowe warianty oraz 2 wcześniejsze próbki bez oceny** (`skill_unequip`, `lick_wounds`). [Prompty i długości](AudioGeneration/2026-11-20/event-revisions-03/Prompty.md).

Eksport ocen zestawu 03 z 20.11.2026, godz. 22:04, zawiera **3 „Pasuje” i 2 „Do poprawy”**. Podpięto `shard_collect`, `enemy_death` i `defensive_stance`, z zapisanymi pełnymi fragmentami i tempem 1×. Łącznie 108 spośród 110 przygotowywanych pozycji ma zaakceptowany wariant w grze. Pozostałe **`skill_unequip` i `lick_wounds` są odrzucone**; ich plików nie dodano do zaakceptowanej biblioteki. [Źródłowy import ocen](AudioGeneration/2026-11-20/event-revisions-03/review_import_report.json) · [Aktualna kolekcja 25 zaakceptowanych spośród ostatnich 27 pozycji](AudioGeneration/2026-11-20/approved-update-03/accepted-selection/collection_manifest.json).

Stan na **27.10.2026**. Pełny zakres obejmuje **183 pozycje SFX**: 109 zdarzeń ogólnych, 69 akcentów umiejętności i 5 wspólnych warstw klas. **73 pozycje mają podpięte pliki; 110 nie ma własnego klipu**. Są to pozycje do obsłużenia, a liczba potrzebnych unikatowych nagrań może być mniejsza dzięki współdzieleniu próbek.

Na 110 braków składa się **55 zdarzeń ogólnych, 51 akcentów umiejętności i 4 warstwy klas**. Umiejętności gracza bez własnego akcentu korzystają z ogólnego `player_skill_cast`. Zastępczy `enemy_skill_cast` nie ma klipu, dlatego 29 umiejętności wrogów bez własnego wpisu nie ma dźwięku rzucenia. Osobne efekty trafienia lub osłony mogą nadal zagrać.

## Stan według obszaru

| Obszar | Pozycje w planie | Podpięte | Bez własnego klipu |
| --- | ---: | ---: | ---: |
| Menu i mapa | 18 | 11 | 7 |
| Karty i tury | 18 | 13 | 5 |
| Plansza i zasoby | 24 | 9 | 15 |
| Walka i statusy | 24 | 12 | 12 |
| Nagrody i rozwój | 25 | 9 | 16 |
| Umiejętności podstawowe | 6 | 3 | 3 |
| Umiejętności wrogów | 33 | 4 | 29 |
| Mag | 11 | 7 | 4 |
| Wojownik | 11 | 4 | 7 |
| Cień | 13 | 1 | 12 |
| **Razem** | **183** | **73** | **110** |

Warstwy klas są ujęte w planie jako osobne pozycje. Ich brak nie blokuje odtwarzania istniejących akcentów. Stan „podpięte” oznacza przypisanie plików w bibliotece; końcowe dopasowanie głośności i synchronizacji wymaga odsłuchu w grze.

## Kolejność przygotowania

1. **Wspólny dźwięk umiejętności wroga:** `enemy_skill_cast`. Wypełni lukę dla wszystkich 29 obecnie nierozpoznawanych umiejętności wrogów.
2. **Czytelność wyniku walki:** `shield_hit`, `enemy_death`, `player_death`, `victory`, `defeat`.
3. **Dopasowania i wejście do walki:** `match_wave_complete`, `big_match`, `cascade_wave`, `super_match`, `battle_start` oraz trzy warianty `encounter_*`.
4. **Informacja o skutkach akcji:** leczenie, regeneracja, nakładanie i wygasanie statusów, przyrost zasobów oraz wybór celów.
5. **Charakter umiejętności i nagród:** dedykowane akcenty, postęp XP, rozwój postaci, crafting i mapa.
6. **Dopracowanie:** wspólne warstwy klas, kroki pionka, hover i drobne przejścia UI.

**A** — pierwsza kolejność; **B** — pełna informacja zwrotna i charakter umiejętności; **C** — dopracowanie. Czasy w tabelach są propozycjami długości słyszalnego efektu, do zgrania z animacją. Paleta: subtelne fantasy, papier, kamień, drewno i organiczna magia; bez metalicznych dzwonków.

## Brakujące zdarzenia ogólne

Każdy poniższy identyfikator istnieje już w systemie ogólnych SFX, ale nie ma przypisanego klipu. Nazwy odpowiadają plikom w `Assets/_Project/Audio/SFX`.

### Menu i mapa

| Priorytet | Identyfikator | Zdarzenie i brzmienie | Czas |
| --- | --- | --- | --- |
| C | `map_token_step` | Ruch pionka po mapie: kroki dopasowane do animacji, nie do każdej klatki. Lekki krok pionka na kamiennej mapie; przy prędkości 6 jednostek/s kroki wypadają nominalnie co ok. 0,18 s, odtwarzacz ogranicza je do 0,22 s. | 0,10–0,16 s na krok; droga ma zmienny czas. |
| A | `encounter_normal` | Wejście do zwykłej walki. Krótki ciemny impuls energii zapowiadający walkę; wyraźny początek bez długiej fanfary. | 0,30–0,50 s |
| A | `encounter_elite` | Wejście do walki z elitą. Ten sam motyw co zwykłe spotkanie, lecz cięższy i głębszy, sygnalizujący większe zagrożenie. | 0,40–0,65 s |
| A | `encounter_boss` | Wejście do walki z bossem. Najgłębszy i najmocniejszy wariant motywu spotkania; zwięzły akcent zagrożenia bez długiej fanfary podczas ładowania. | 0,55–0,90 s |
| B | `forge_open` | Otwarcie kuźni kafelków: krótki akcent żaru i runy. Głęboki oddech żaru, kamienny nacisk i krótka iskra runy; bez metalicznego dzwonienia. | 0,25–0,45 s, do potwierdzenia z animacją panelu. |
| B | `chest_reward` | Odebranie nagrody ze skrzyni: krótki dźwięk zdobyczy. Krótki, ciepły impuls zdobyczy z lekką magią, bez jasnej kaskady dzwonków. | 0,20–0,38 s. |
| C | `panel_close` | Zamknięcie panelu. Miękki ruch pergaminu z delikatnym osadzeniem; kontur opadający, przeciwny do otwarcia. Zgrać z ruchem panelu. | 0,14–0,28 s |

### Karty i przebieg tury

| Priorytet | Identyfikator | Zdarzenie i brzmienie | Czas |
| --- | --- | --- | --- |
| A | `battle_start` | Początek walki: sygnał wejścia na planszę. Niski puls aktywacji i krótkie rozbudzenie planszy, bez muzycznej fanfary. | 0,45–0,75 s; wizualny start sceny do pomiaru. |
| B | `player_turn_start` | Początek tury gracza i odzyskanie kontroli: krótki znacznik tury. Jasny, lecz miękki puls gotowości, różny od tury przeciwnika. | 0,14–0,26 s. |
| B | `target_selection_complete` | Wskazanie ostatniego wymaganego celu: akcent domknięcia wyboru. Dwa blisko położone impulsy domykające wybór, bez długiego wybrzmienia. | 0,12–0,22 s. |
| B | `card_choice_open` | Otwarcie dodatkowego wyboru koloru lub kierunku grawitacji. Delikatne rozchylenie magicznej warstwy UI. | 0,18–0,30 s. |
| B | `enemy_turn_end` | Koniec tury przeciwnika. Spokojne rozładowanie napięcia sygnalizujące zakończenie działań wroga. | 0,14–0,25 s |

### Plansza i zasoby

| Priorytet | Identyfikator | Zdarzenie i brzmienie | Czas |
| --- | --- | --- | --- |
| B | `tile_recolor` | Przemalowanie kafelka: zmiana barwy lub magiczne muśnięcie. Krótki, płynny przepływ energii między barwami z cichym końcowym rozbłyskiem. | 0,15–0,28 s. |
| B | `tile_purge` | Usunięcie blokady lub negatywnego kafelka przez Purge. Krótki rozpad ciemnej powłoki i ciche oczyszczenie energii. | 0,20–0,38 s. |
| B | `gravity_shift` | Zmiana kierunku grawitacji: niski akcent lub obrót przestrzeni. Niski, miękki obrót przestrzeni bez mechanicznego zgrzytu. | 0,20–0,35 s; kolejne spadanie brzmi osobno. |
| B | `board_shuffle` | Przetasowanie planszy: wieloelementowy ruch. Kilka przesunięć kamienia z jednym domknięciem, bez długiego grzechotania. | 0,18–0,30 s; widoczne ruchy używają 0,12 s. |
| C | `tile_spawn` | Pojawienie się nowych kafelków: delikatne uzupełnienie. Miękki impuls pojawienia się materii, krótszy niż dźwięk zbierania nagrody. | 0,10–0,18 s na grupę. |
| A | `match_wave_complete` | Zakończenie jednej fali dopasowania: jeden akcent na falę. Spójny magiczny akcent wyniku fali, mocniejszy od pęknięć pojedynczych kafelków. | 0,20–0,36 s; niezależnie od liczby kafelków. |
| A | `big_match` | Duże dopasowanie: mocniejszy wariant akcentu fali. Głębszy, szerszy wariant ukończenia fali; nie dubluje głośno Super Match. | 0,28–0,48 s. |
| A | `cascade_wave` | Kolejna fala kaskady. Krótki impuls narastającej energii; kolejne fale mogą zwiększać intensywność. Osobny akcent dla każdej fali. | 0,18–0,30 s na falę |
| A | `super_match` | Finał Super Match. Pojedynczy pełniejszy finał energii kaskady. Nie utrzymywać tonu przez cały czas wyświetlania napisu. | 0,35–0,55 s |
| B | `mana_gain` | Przyrost many po fali: krótki sygnał napełniania zasobu. Krótki płynny wzrost energii w zasobie, cichszy od głównego akcentu fali. | 0,14–0,28 s. |
| B | `shard_collect` | Pojawienie się odłamków i dotarcie ich do licznika: lekki zbiór. Kilka delikatnych ziaren energii w ruchu do licznika; bez dźwięku trwającego do czasu usunięcia obiektu. | 0,35–0,75 s; cząstki żyją 0,8 s, usunięcie obiektu po 2 s jest tylko sprzątaniem. |
| B | `tile_bonus_mana` | Premia kafelka: mana. Miękki, płynny impuls napełnienia zasobu; ta sama rodzina brzmień co pozostałe bonusy. | 0,12–0,28 s |
| B | `tile_bonus_heal` | Premia kafelka: leczenie. Mały ciepły puls odzyskania zdrowia; subtelniejszy od pełnego czaru leczącego. | 0,12–0,28 s |
| B | `tile_bonus_action` | Premia kafelka: punkt akcji. Krótki rytmiczny impuls gotowości, wyróżniający odzyskanie możliwości działania. | 0,12–0,28 s |
| B | `tile_bonus_shield` | Premia kafelka: tarcza. Miękkie zagęszczenie energii ochrony; niewielki akcent bez metalicznego dzwonienia. | 0,12–0,28 s |

### Ataki umiejętności i statusy

| Priorytet | Identyfikator | Zdarzenie i brzmienie | Czas |
| --- | --- | --- | --- |
| A | `shield_hit` | Trafienie w tarczę: osobna, twardsza warstwa uderzenia. Gęsty, sprężysty impuls bariery bez stalowego brzęku. | 0,20–0,30 s; wizualny puls tarczy 0,22 s. |
| A | `enemy_death` | Śmierć przeciwnika. Zanik ciężaru i energii przeciwnika. Nie powtarzać akcentu uderzenia pocisku. | 0,50–0,85 s |
| A | `player_death` | Śmierć gracza. Cięższy, bardziej osobisty finał utraty energii. Oddzielić od samego trafienia i sygnału porażki. | 0,65–1,00 s |
| A | `enemy_skill_cast` | Rzucenie umiejętności wroga. Wspólna sygnatura magii lub ruchu wroga, z miejscem na akcent konkretnej umiejętności. Kontakt z celem brzmi później, osobno. | 0,18–0,38 s |
| B | `healing` | Leczenie. Ciepły przepływ energii z miękkim domknięciem, czytelnie sygnalizujący odzyskanie zdrowia. | 0,30–0,55 s |
| B | `regeneration_tick` | Tick regeneracji. Pojedynczy mały ciepły puls; nie ciągły dźwięk przez wszystkie tury statusu. | 0,16–0,30 s |
| B | `status_apply` | Nałożenie statusu. Miękki akcent pojawienia się runicznego znaku, z krótkim końcem. | 0,14–0,26 s |
| B | `status_strengthen` | Wzmocnienie statusu. Pełniejszy puls tej samej runicznej rodziny, wyraźniejszy niż samo nałożenie. | 0,18–0,30 s |
| B | `status_expire` | Wygaśnięcie statusu. Ciche rozpuszczenie lub odpływ energii znaku. Czas trwania statusu w turach nie określa długości dźwięku. | 0,14–0,26 s |
| B | `mana_transmute` | Przemiana many — skutek akcji. Płynny przepływ energii między barwami; wspólny efekt skutku, odrębny od akcentu użycia TransmuteMana. | 0,30–0,50 s |
| B | `card_recover` | Odzyskanie karty — skutek akcji. Krótki odwrócony szelest papieru. Nie dublować głośno akcentu umiejętności Recycle. | 0,18–0,32 s |
| B | `soul_collect` | Zebranie dusz — skutek akcji. Ciche zbliżające się oddechy energii zbiegające się w punkt zbioru. | 0,30–0,55 s |

### Wynik walki i rozwój postaci

| Priorytet | Identyfikator | Zdarzenie i brzmienie | Czas |
| --- | --- | --- | --- |
| A | `victory` | Zwycięstwo. Ciepłe rozjaśnienie energii i mocny krótki finał, bez metalicznych dzwonków. | 0,70–1,15 s |
| A | `defeat` | Porażka. Opadnięcie energii i zamknięcie napięcia, bez przesadnie dramatycznej fanfary. | 0,60–1,00 s |
| B | `battle_rewards_reveal` | Pojawienie się nagród i odłamków po walce. Kilka delikatnych pojawień i jedno wspólne osadzenie, bez odtwarzania dźwięku przez cały czas oglądania panelu. | 0,35–0,65 s; liczba nagród jest zmienna. |
| B | `experience_fill` | Napełnianie paska doświadczenia. Cicha płynąca energia, która ustaje dokładnie z paskiem; poziom ma osobny akcent, więc nie przedłużać stałego one-shotu. | dynamicznie 0,15–0,90 s na odcinek; pauza przy poziomie 0,15 s. |
| B | `boss_reward_reveal` | Odsłonięcie nagrody po bossie. Głębszy magiczny rozkwit wyróżniający ważniejszą nagrodę. | 0,50–0,90 s |
| B | `new_skill_choose` | Wybór nowej umiejętności. Zwięzłe potwierdzenie wyboru, bez powtarzania fanfary odsłonięcia nagrody. | 0,14–0,28 s |
| B | `skill_equip` | Wyposażenie umiejętności. Miękkie wsunięcie energii w slot, kończące się dyskretnym osadzeniem. | 0,11–0,22 s |
| B | `skill_unequip` | Zdjęcie umiejętności. Odwrotny kontur wyposażenia: krótkie wysunięcie lub odpływ energii ze slotu. | 0,11–0,22 s |
| C | `card_filter_change` | Zmiana filtra kart. Dyskretny impuls UI potwierdzający zmianę; nie szelest przewracanej strony. | 0,07–0,14 s |
| B | `stat_point_add` | Dodanie punktu statystyki. Mały miękki impuls wzrostu, bez pełnego akcentu zatwierdzenia wszystkich zmian. | 0,08–0,16 s |
| B | `stat_point_undo` | Cofnięcie punktu statystyki. Miękki impuls o przeciwnym kierunku niż dodanie punktu; neutralny, bez alarmu błędu. | 0,08–0,16 s |
| B | `stat_points_confirm` | Zatwierdzenie punktów statystyki. Pełniejszy, lecz krótki puls domykający konfigurację. | 0,18–0,30 s |
| B | `tile_upgrade_select` | Wybór ulepszenia kafelka. Miękki impuls runy, wyróżniony od fizycznego wyboru typu kafelka. | 0,08–0,18 s |
| B | `tile_slot_select` | Wybór slotu kafelka. Krótki, miękki kontakt osadzający wybór w slocie. Poprzednia próbka kamiennego kontaktu została odrzucona. | 0,08–0,18 s |
| B | `tile_upgrade_remove` | Usunięcie ulepszenia kafelka. Cichy odpływ energii z runy/slotu, odróżniony od dodawania ulepszenia. | 0,12–0,24 s |
| C | `ui_hover` | Najechanie na kartę, umiejętność lub element z opisem: bardzo subtelny sygnał UI. Prawie niesłyszalny miękki dotyk UI, bez tonu utrzymującego się podczas hover. | 0,05–0,10 s; odtwarzacz ogranicza cue do odstępów 0,08 s. |

**Zamienniki po odrzuceniu wcześniejszych próbek:** `map_token_step`, `forge_open` oraz `tile_slot_select`, który korzystał z propozycji kroku pionka.

## Brakujące akcenty i warstwy z wcześniejszego planu

Poniżej 21 akcentów i 4 wspólne warstwy. Akcent umiejętności rozpoczyna się przy jej użyciu; trafienia, leczenie i inne skutki powinny mieć własny punkt synchronizacji. Wspólne warstwy klas mogą poczekać do końcowego miksu.

### Podstawowe

| Priorytet | Identyfikator | Brzmienie | Czas |
| --- | --- | --- | --- |
| C | `basic_layer` | Delikatny wspólny impuls energii; zostawia miejsce na akcent konkretnej umiejętności. | Przy użyciu, 0,12–0,20 s |
| B | `basic_heal` | Ciepłe skupienie energii i miękkie rozjaśnienie HP. | Przy leczeniu, 0,25–0,45 s |
| B | `shield_bash` | Krótki cięższy pchnięty ruch tarczy; trafienie oddzielić na kontakt celu. | Przy zamachu, 0,18–0,32 s |

### Wrogowie

| Priorytet | Identyfikator | Brzmienie | Czas |
| --- | --- | --- | --- |
| B | `enemy_slam` | Niski ruch ciężaru do uderzenia; szczyt uderzenia należy do SFX trafienia. | Przy zamachu, 0,22–0,40 s |

### Mag

| Priorytet | Identyfikator | Brzmienie | Czas |
| --- | --- | --- | --- |
| C | `mage_layer` | Cichy oddech skupianej many, bez jasnych dzwonków. | Przy użyciu, 0,14–0,24 s |
| B | `convergence` | Trzy subtelne strumienie zbiegają się w jeden puls obrażeń, leczenia i osłony. | Przy użyciu, 0,38–0,62 s |
| B | `renew` | Ciepły puls odnowy; cztery późniejsze ticki leczenia mają oddzielny krótki sygnał. | Przy nałożeniu regeneracji, 0,22–0,38 s |
| B | `transmute_mana` | Płynne przelanie do trzech punktów many między dwiema barwami. | Przy zatwierdzonej zamianie koloru, 0,24–0,42 s |

### Wojownik

| Priorytet | Identyfikator | Brzmienie | Czas |
| --- | --- | --- | --- |
| C | `warrior_layer` | Matowy ruch skóry, ciężaru i oddechu postawy; bez metalicznych dzwonków. | Przy użyciu, 0,12–0,22 s |
| B | `battle_frenzy` | Narastający rytm oddechu i impetu; wcześniejsze Resolve zwiększają siłę, nie długość cue. | Przy użyciu, 0,18–0,34 s |
| B | `fortress` | Warstwy masywnej osłony domykają się w jednym gęstym pulsie; liczba grup zwiększa siłę tarczy, nie czas. | Przy nałożeniu ochrony, 0,24–0,42 s |
| B | `guard_stance` | Stabilny krok i miękkie zaryglowanie osłony bez ostrego rezonansu. | Przy ustawieniu gardy, 0,18–0,34 s |
| B | `perfect_strike` | Bardzo skupiony, szybki ruch; udane pojedyncze dopasowanie może wzmocnić akcent trafienia. | Przy precyzyjnym zamachu, 0,14–0,26 s |
| B | `riposte` | Cicha napięta garda teraz, a kontratak dopiero na następnym wrogim trafieniu. | Przy przygotowaniu kontry, 0,18–0,32 s |
| B | `unyielding` | Niski, spokojny puls wytrzymałości; późniejszy wzmocniony atak ma własne SFX. | Przy wzmocnieniu obrony, 0,22–0,40 s |

### Cień

| Priorytet | Identyfikator | Brzmienie | Czas |
| --- | --- | --- | --- |
| C | `shadow_layer` | Cichy ruch powietrza i przytłumionej energii, bez szklistych tonów. | Przy użyciu, 0,10–0,20 s |
| B | `blade_dance` | Zwinny wir kilku lekkich ruchów; liczba kart skaluje obrażenia, nie narzuca serii osobnych trafień. | Przy użyciu, 0,18–0,36 s |
| B | `blood_pact` | Niski organiczny puls ofiary przechodzący w energię działania; bez głosu lub bólu postaci. | Przy utracie HP i zyskaniu AP, 0,22–0,38 s |
| B | `expose_weakness` | Cienka szczelina w ochronie i cichy mroczny znak; trzy przyszłe trafienia nie brzmią teraz. | Przy oznaczeniu celu, 0,18–0,32 s |
| B | `killing_sequence` | Skupiona seria gestów kończąca się jednym akcentem; liczba kart skaluje obrażenia, nie tworzy automatycznie osobnych uderzeń. | Przy użyciu, 0,22–0,40 s |
| B | `momentum` | Szybki przepływ energii w ruch karty; jeśli dobrane są dwie, drugi lekki szelest kart osobno. | Przy dobraniu kart, 0,16–0,30 s |
| B | `quick_cut` | Dwa krótkie cięcia z bliskim odstępem tylko gdy animacja rozdziela trafienia; kod liczy je synchronicznie. | Przy użyciu, 0,14–0,28 s |
| B | `recycle` | Odwrócony ruch papieru przez cień; oszczędnie, by nie dublować osobnego CardRecover. | Przy odzyskaniu karty, 0,18–0,32 s |
| B | `shadow_harvest` | Ciche smugi energii zbiegają się w jeden zbiór; liczba zniszczonych kafelków reguluje intensywność. | Przy zebraniu dusz, 0,22–0,42 s |
| B | `soul_blade` | Matowa energia dusz nasyca ostrze; do czterech trafień wymaga osobnych, krótkich akcentów tylko po rozdzieleniu ich w czasie. | Przy aktywacji, 0,22–0,42 s |

## Nowe umiejętności wymagające przypisania dźwięku

**30 dodatkowych akcentów:** 28 wrogów i 2 Cienia. Wszystkie 28 umiejętności wrogów występuje w konfiguracjach przeciwników; priorytet **B**, po uzupełnieniu ogólnego `enemy_skill_cast`. SoulMark i SoulTransmute są w katalogu gracza, ale mają wyłączone odblokowywanie, dlatego ich audio może poczekać do udostępnienia umiejętności. Podane nazwy plików są proponowanymi kluczami zgodnymi z obecną konwencją budowania biblioteki.

| Umiejętność i klucz audio | Rodzina | Brzmienie | Proponowany czas |
| --- | --- | --- | --- |
| [BodySlam](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/BodySlam.asset) — `body_slam` | Ciężkie ataki | Niski ruch masy i matowy zamach; koszt HP osobno. | 0,20–0,40 s |
| [DexrafiSlam](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/DexrafiSlam.asset) — `dexrafi_slam` | Ciężkie ataki | Szeroki, głęboki zamach; kontakt celu osobno. | 0,20–0,40 s |
| [HeadButt](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/HeadButt.asset) — `head_butt` | Ciężkie ataki | Krótki gwałtowny wyrzut powietrza i ciężaru. | 0,15–0,30 s |
| [ArmorBash](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/EnemyShieldBash.asset) — `armor_bash` | Ciężkie ataki | Ściągnięcie osłony w jeden ciężki impuls. | 0,20–0,40 s |
| [MultiStrike](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/MultiStrike.asset) — `multi_strike` | Serie ataków | Krótka seria ruchów; akcenty trafień dopiero na kontakcie. | 0,18–0,35 s na gest |
| [KonarHit](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/KonarHit.asset) — `konar_hit` | Drewno i korzenie | Skrzypnięcie konaru i szorstki zamach; uderzenie osobno. | 0,18–0,35 s |
| [Entail](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Entail.asset) — `entail` | Drewno i korzenie | Krótki rozrost korzeni, tarcie drewna i ziemi. | 0,25–0,50 s |
| [EmberLight](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/EmberLight.asset) — `ember_light` | Ogień | Suchy mały zapłon przy nałożeniu podpalenia. | 0,18–0,32 s |
| [EmberStrike](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/EmberStrike.asset) — `ember_strike` | Ogień | Zamach z krótką smugą żaru. | 0,18–0,35 s |
| [EmberDetonation](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/EmberDetonation.asset) — `ember_detonation` | Ogień | Zassanie płomienia i zwarty niski wybuch; baza IgniteBurst. | 0,30–0,55 s |
| [DefensiveStance](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/DefensiveStance.asset) — `defensive_stance` | Obrona | Matowe osadzenie ciężaru i krótki oddech obrony. | 0,18–0,35 s |
| [DexrafiGuard](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/DexrafiGuard.asset) — `dexrafi_guard` | Obrona | Zagęszczenie ochrony; można użyć rodziny EnemyGuard. | 0,20–0,35 s |
| [IronWall](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/IronWall.asset) — `iron_wall` | Obrona | Ciężki, suchy impuls zamknięcia osłony. | 0,20–0,40 s |
| [ThickHide](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ThickHide.asset) — `thick_hide` | Obrona | Niski organiczny puls napięcia skóry. | 0,20–0,35 s |
| [LickWounds](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/LickWounds.asset) — `lick_wounds` | Odnowa | Krótki organiczny gest i ciepły puls odnowy. | 0,25–0,45 s |
| [Regeneracja](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Regeneracja.asset) — `regeneracja` | Odnowa | Łagodny rozkwit energii życia; późniejsze ticki osobno. | 0,25–0,45 s |
| [Devour](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Devour.asset) — `devour` | Odnowa | Organiczny chwyt i pochłonięcie z niskim zasysaniem. | 0,25–0,50 s |
| [ArcaneHunger](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ArcaneHunger.asset) — `arcane_hunger` | Drenaż i magia | Cichy pusty oddech i mroczny puls aktywacji. | 0,20–0,40 s |
| [ManaNibble](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ManaNibble.asset) — `mana_nibble` | Drenaż i magia | Krótki gest ugryzienia energii i opadający puls many. | 0,18–0,35 s |
| [ManaFeast](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ManaFeast.asset) — `mana_feast` | Drenaż i magia | Pełniejsze wciągnięcie energii zakończone ciepłym pulsem. | 0,25–0,50 s |
| [Laser](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Laser.asset) — `laser` | Drenaż i magia | Skupiony strumień magiczny; długość ciągłej warstwy zależna od VFX. | Start 0,20–0,35 s |
| [Corrosion](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Corrosion.asset) — `corrosion` | Drenaż i magia | Matowe rozpuszczenie powłoki i zasysanie energii. | 0,25–0,45 s |
| [Scream](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Scream.asset) — `scream` | Głos i wzmocnienie | Chropawy krzyk stworzenia z przygaszonym echem. | 0,35–0,70 s |
| [Rage](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Rage.asset) — `rage` | Głos i wzmocnienie | Gardłowy pomruk i narastający impuls napięcia. | 0,25–0,50 s |
| [SlimeCoat](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/SlimeCoat.asset) — `slime_coat` | Organiczne i toksyczne | Lepkie rozciągnięcie i mokre osadzenie powłoki. | 0,25–0,45 s |
| [ThrowBeeHive](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ThrowBeeHive.asset) — `throw_bee_hive` | Organiczne i toksyczne | Krótki rzut i zwarty szum roju; tick trucizny osobno. | 0,30–0,60 s |
| [ToxicBite](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/ToxicBite.asset) — `toxic_bite` | Organiczne i toksyczne | Suche kłapnięcie z krótką organiczną warstwą osłabienia. | 0,18–0,35 s |
| [Zakopanie](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/Zakopanie.asset) — `zakopanie` | Ziemia | Krótkie osunięcie ziemi przy zniknięciu pod powierzchnią. | 0,30–0,55 s |
| [SoulMark](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/SoulMark.asset) — `soul_mark` | Dusze | Cichy mroczny znak, oddech i impuls pieczęci. | 0,18–0,32 s |
| [SoulTransmute](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/SoulTransmute.asset) — `soul_transmute` | Dusze | Zbieganie smug dusz w ciepły impuls odnowy. | 0,25–0,45 s |

**Ważne mapowanie:** plik `EnemyShieldBash.asset` ma nazwę obiektu `ArmorBash`. Wpis biblioteki powinien użyć `ArmorBash`, a plik audio `armor_bash.wav`. Pozostałe nowe nazwy odpowiadają nazwom obiektów umiejętności.

**Późniejszy etap:** SoulMark, SoulTransmute oraz Recycle mają `ExcludedFromUnlocks = true`. Ich akcenty pozostają w pełnym planie, ale nie są pilnymi brakami puli odblokowywanych umiejętności.

## Co już jest podpięte

Poniższych 73 pozycji nie trzeba dobierać od zera. Podpięte akcenty mogą też posłużyć jako baza dla nowych umiejętności tej samej rodziny.

| Obszar | Istniejące identyfikatory |
| --- | --- |
| Menu i mapa | `card_shop_open`, `chest_empty`, `chest_open`, `map_arrival`, `map_node_select`, `menu_confirm`, `panel_open`, `route_cancel`, `route_confirm`, `route_preview`, `scene_transition` |
| Karty i tury | `card_choice_cancel`, `card_choice_confirm`, `card_deselect`, `card_queue_add`, `card_queue_undo`, `card_select`, `cards_discard`, `enemy_turn_start`, `hand_draw`, `player_turn_end`, `resolve_start`, `single_card_draw`, `target_tile_select` |
| Plansza i zasoby | `row_shift`, `tile_bonus_card`, `tile_bonus_ignite`, `tile_bonus_neighbor_break`, `tile_break`, `tile_crack_mark`, `tile_fall`, `tile_rotate`, `tile_swap_timed` |
| Walka i statusy | `burn_tick_timed`, `enemy_intent`, `enemy_projectile_hit`, `hp_sacrifice`, `multi_hit_tick`, `player_attack_windup`, `player_hit`, `player_projectile_launch`, `player_skill_cast`, `shield_appear`, `shield_apply`, `shield_fade` |
| Nagrody i rozwój | `battle_result_close`, `card_craft_success`, `card_page_change`, `card_recipe_select`, `deck_card_add`, `deck_card_remove`, `level_up`, `tile_craft_success`, `tile_type_select` |
| Umiejętności podstawowe | `basic_attack_skill`, `guard_up`, `ignite` |
| Umiejętności wrogów | `enemy_ember`, `enemy_guard`, `enemy_layer`, `enemy_strike` |
| Mag | `chromatic_bolt`, `fire_spark`, `ice_ward`, `ignite_burst`, `kindle`, `lightning_chain`, `wildfire` |
| Wojownik | `execute`, `heavy_slash`, `shield_slam`, `wide_swing` |
| Cień | `backstab` |

## Współdzielenie nagrań

- **Ogień:** wykorzystać rodziny `ignite` / `enemy_ember` i `ignite_burst` dla EmberLight, EmberStrike i EmberDetonation.
- **Obrona:** bazować na `shield_appear`, `shield_apply`, `enemy_guard`; rozróżnić organiczną skórę, energię tarczy i ciężar pancerza.
- **Ataki fizyczne:** warianty istniejących zamachów lekkich i ciężkich; osobno kontakt celu.
- **Leczenie:** wspólna baza dla Healing, BasicHeal, Renew, LickWounds i Regeneracja; tick powinien być krótszy i cichszy niż aktywacja.
- **Dusze i drenaż:** jedna rodzina zasysania energii z różnymi zakończeniami dla many, dusz i odzyskania HP.
- **UI i rozwój:** rodziny potwierdzenia, cofnięcia i wyboru mogą obsługiwać kilka ekranów.

## Dodatkowe zdarzenia do rozważenia

Te pięć propozycji **nie wchodzi do 183 pozycji**. Wymagają dodania osobnego zdarzenia audio albo świadomego wykorzystania istniejącego.

| Proponowany klucz | Moment odtworzenia | Potrzebne brzmienie |
| --- | --- | --- |
| `poison_tick` | Rzeczywiste obrażenie od trucizny | Krótki toksyczny, organiczny impuls odróżniony od Burn. |
| `action_points_drain` | Faktyczna utrata AP od dezorientacji | Ciche opadające wygaszenie energii działania. |
| `mana_drain` | Faktyczne odjęcie many przez wroga | Krótkie zassanie zasobu, przeciwne do ManaGain. |
| `tile_root_apply` | Zakorzenienie kafelków | Krótki trzask i zacisk korzeni, jeden akcent na grupę. |
| `attack_evaded` | Pominięcie bezpośredniego trafienia podczas zakopania | Krótki pusty zamach lub świst chybienia. |

Nakładanie i wygasanie zwykłych statusów może korzystać ze wspólnych `status_apply` i `status_expire`; nie każdy status potrzebuje osobnego nagrania.

## Muzyka i tło jako osobny zakres

Poza powyższą listą SFX można przygotować pięć pętli: spokojne tło menu i mapy, muzykę zwykłej walki, wariant walki z bossem, delikatną atmosferę mapy oraz żar kuźni. To dodatkowy zakres aranżacji i sterowania odtwarzaniem; nie jest wliczony do braków biblioteki SFX.

## Synchronizacja i wdrożenie

- Akcenty trafienia uruchamiać na kontakcie celu, a zamach lub rzut na początku akcji.
- Pęknięcia pojedynczych kafelków pozostawić ciche; główny akcent powinien wystąpić raz na falę dopasowania.
- Burn, trucizna i regeneracja powinny grać przy rzeczywistych tickach, a nie nieprzerwanie przez wszystkie tury statusu.
- XP, laser, lot pocisku i ewentualne ciągłe tła wymagają kontrolowanego końca. Obecny odtwarzacz SFX używa `PlayOneShot`, więc sam plik nie zatrzyma się wraz z końcem VFX.
- Po dodaniu klipów zaktualizować bibliotekę, sprawdzić zgodność nazw umiejętności i odsłuchać równoczesne warstwy w Play Mode.

## Źródła projektu

Stan przypisań: [SfxLibrary.asset](D:/Unity/projs/M3p/Assets/_Project/Audio/SfxLibrary.asset). Zdarzenia i reguły odtwarzania: [SfxId.cs](D:/Unity/projs/M3p/Assets/_Project/Scripts/Audio/SfxId.cs), [SfxPlayer.cs](D:/Unity/projs/M3p/Assets/_Project/Scripts/Audio/SfxPlayer.cs) oraz [SfxLibraryBuilder.cs](D:/Unity/projs/M3p/Assets/_Project/Scripts/Editor/SfxLibraryBuilder.cs). Konfiguracje: [umiejętności](D:/Unity/projs/M3p/Assets/_Project/Configs/Skills), [przeciwnicy](D:/Unity/projs/M3p/Assets/_Project/Configs/Enemies).

Wcześniejszy plan i opisy brzmienia: [Miejsca na dźwięk](D:/Unity/projs/M3p/Docs/M3p_miejsca_na_dzwiek.md). Historyczne oceny: [Dobór SFX](D:/Unity/projs/M3p/Docs/M3p_dobor_dzwiekow_v1.md) i [Lista 80 braków](D:/Unity/projs/M3p/Docs/M3p_brakujace_dzwieki.md). Wzmianka o pustej bibliotece w tych dwóch starszych dokumentach jest nieaktualna. Obecny spis rozszerza 153 wcześniejsze pozycje o 30 nowych akcentów.

