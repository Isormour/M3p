# Brakujące dźwięki M3p

**Stan na 25.09.2026, po imporcie ocen z godz. 20:43:37: 80 pozycji bez zaakceptowanej próbki z 153 nazw SFX.**

73 pozycje mają co najmniej jedną próbkę ocenioną „Pasuje”. Na tej liście jest 77 pozycji bez zaakceptowanego kandydata z pierwszego doboru oraz 3 pozycje, których propozycję odrzucono: `map_token_step`, `tile_slot_select`, `forge_open`.

„Brak dźwięku” oznacza tutaj brak zaakceptowanego doboru. Wdrożenie to osobny etap: biblioteka gry nadal jest pusta. Zaakceptowane próbki bez wyboru pełne źródło/wycinek nie są ponownie liczone jako braki — wymagają decyzji montażowej w [tabeli ocen](M3p_dobor_dzwiekow_v1.md).

Każdy identyfikator poniżej jest osobną pozycją do dobrania. Podobne zdarzenia mogą korzystać z jednego nagrania. Czasy to propozycje z dokumentu, do dopasowania do obrazu. Paleta: subtelne fantasy, papier, kamień i organiczna magia, bez metalicznych dzwonków.

**Priorytety:** A — podstawowa czytelność walki; B — informacja zwrotna; C — dopracowanie. Umiejętności nie mają osobnych priorytetów A/B/C w dokumencie źródłowym.

| Obszar | Brakujące pozycje |
| --- | ---: |
| Menu i mapa | 7 |
| Karty i przebieg tury | 5 |
| Plansza i zasoby | 15 |
| Ataki umiejętności i statusy | 12 |
| Wynik walki i rozwój postaci | 16 |
| Umiejętności — Podstawowe | 3 |
| Umiejętności — Wrogowie | 1 |
| Umiejętności — Mag | 4 |
| Umiejętności — Wojownik | 7 |
| Umiejętności — Cień | 10 |

W pierwszej kolejności warto uzupełnić: sygnały wejścia i początku walki, zakończenie fal i kaskad, trafienie w tarczę, śmierć postaci oraz zwycięstwo/porażkę.

## Menu i mapa

### 1. Ruch pionka po mapie: kroki dopasowane do animacji, nie do każdej klatki — `map_token_step`

**Stan:** poprzednia propozycja odrzucona — potrzebny zamiennik. **Priorytet: C.**

**Gdzie i kiedy:** na każdym progu 1,1 jednostki ruchu.

**Co ma zagrać:** Lekki krok pionka na kamiennej mapie; przy prędkości 6 jednostek/s kroki wypadają nominalnie co ok. 0,18 s, odtwarzacz ogranicza je do 0,22 s.

**Docelowy czas / synchronizacja:** 0,10–0,16 s na krok; droga ma zmienny czas.

**Miejsce w projekcie:** [Map/MapPlayerToken.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapPlayerToken.cs>).

### 2. Wejście do zwykłej walki — `encounter_normal`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Na mapie, gdy wybrany węzeł uruchamia zwykłe spotkanie.

**Co ma zagrać:** Krótki ciemny impuls energii zapowiadający walkę; wyraźny początek bez długiej fanfary.

**Docelowy czas / synchronizacja:** 0,30–0,50 s

**Miejsce w projekcie:** [Map/MapManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapManager.cs>).

### 3. Wejście do walki z elitą — `encounter_elite`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Na mapie, gdy węzeł uruchamia spotkanie z elitą.

**Co ma zagrać:** Ten sam motyw co zwykłe spotkanie, lecz cięższy i głębszy, sygnalizujący większe zagrożenie.

**Docelowy czas / synchronizacja:** 0,40–0,65 s

**Miejsce w projekcie:** [Map/MapManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapManager.cs>).

### 4. Wejście do walki z bossem — `encounter_boss`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Na mapie, gdy węzeł uruchamia spotkanie z bossem.

**Co ma zagrać:** Najgłębszy i najmocniejszy wariant motywu spotkania; zwięzły akcent zagrożenia bez długiej fanfary podczas ładowania.

**Docelowy czas / synchronizacja:** 0,55–0,90 s

**Miejsce w projekcie:** [Map/MapManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapManager.cs>).

### 5. Otwarcie kuźni kafelków: krótki akcent żaru i runy — `forge_open`

**Stan:** poprzednia propozycja odrzucona — potrzebny zamiennik. **Priorytet: B.**

**Gdzie i kiedy:** w chwili otwarcia kuźni.

**Co ma zagrać:** Głęboki oddech żaru, kamienny nacisk i krótka iskra runy; bez metalicznego dzwonienia.

**Docelowy czas / synchronizacja:** 0,25–0,45 s, do potwierdzenia z animacją panelu.

**Miejsce w projekcie:** [Map/MapManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapManager.cs>).

### 6. Odebranie nagrody ze skrzyni: krótki dźwięk zdobyczy — `chest_reward`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** po faktycznym odebraniu zawartości.

**Co ma zagrać:** Krótki, ciepły impuls zdobyczy z lekką magią, bez jasnej kaskady dzwonków.

**Docelowy czas / synchronizacja:** 0,20–0,38 s.

**Miejsce w projekcie:** [Map/MapManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Map/MapManager.cs>).

### 7. Zamknięcie panelu — `panel_close`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: C.**

**Gdzie i kiedy:** Przy zamknięciu statystyk, talii, craftingu lub umiejętności.

**Co ma zagrać:** Miękki ruch pergaminu z delikatnym osadzeniem; kontur opadający, przeciwny do otwarcia. Zgrać z ruchem panelu.

**Docelowy czas / synchronizacja:** 0,14–0,28 s

**Miejsce w projekcie:** [UI/UIMapPlayerStats.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIMapPlayerStats.cs>), [UI/UIPanelClosable.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelClosable.cs>).

## Karty i przebieg tury

### 8. Początek walki: sygnał wejścia na planszę — `battle_start`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** gdy plansza walki staje się aktywna.

**Co ma zagrać:** Niski puls aktywacji i krótkie rozbudzenie planszy, bez muzycznej fanfary.

**Docelowy czas / synchronizacja:** 0,45–0,75 s; wizualny start sceny do pomiaru.

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>).

### 9. Początek tury gracza i odzyskanie kontroli: krótki znacznik tury — `player_turn_start`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** przy odzyskaniu kontroli przez gracza.

**Co ma zagrać:** Jasny, lecz miękki puls gotowości, różny od tury przeciwnika.

**Docelowy czas / synchronizacja:** 0,14–0,26 s.

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>).

### 10. Wskazanie ostatniego wymaganego celu: akcent domknięcia wyboru — `target_selection_complete`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na ostatnim wymaganym celu.

**Co ma zagrać:** Dwa blisko położone impulsy domykające wybór, bez długiego wybrzmienia.

**Docelowy czas / synchronizacja:** 0,12–0,22 s.

**Miejsce w projekcie:** [Cards/CardPlayController.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Cards/CardPlayController.cs>).

### 11. Otwarcie dodatkowego wyboru koloru lub kierunku grawitacji — `card_choice_open`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na pokazaniu wyboru koloru lub grawitacji.

**Co ma zagrać:** Delikatne rozchylenie magicznej warstwy UI.

**Docelowy czas / synchronizacja:** 0,18–0,30 s.

**Miejsce w projekcie:** [Cards/CardPlayController.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Cards/CardPlayController.cs>).

### 12. Koniec tury przeciwnika — `enemy_turn_end`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, gdy przeciwnik kończy turę.

**Co ma zagrać:** Spokojne rozładowanie napięcia sygnalizujące zakończenie działań wroga.

**Docelowy czas / synchronizacja:** 0,14–0,25 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>).

## Plansza i zasoby

### 13. Przemalowanie kafelka: zmiana barwy lub magiczne muśnięcie — `tile_recolor`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** w momencie zmiany koloru kafelka.

**Co ma zagrać:** Krótki, płynny przepływ energii między barwami z cichym końcowym rozbłyskiem.

**Docelowy czas / synchronizacja:** 0,15–0,28 s.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 14. Usunięcie blokady lub negatywnego kafelka przez Purge — `tile_purge`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na usunięciu blokady przez Purge.

**Co ma zagrać:** Krótki rozpad ciemnej powłoki i ciche oczyszczenie energii.

**Docelowy czas / synchronizacja:** 0,20–0,38 s.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 15. Zmiana kierunku grawitacji: niski akcent lub obrót przestrzeni — `gravity_shift`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** po ustawieniu nowego kierunku grawitacji.

**Co ma zagrać:** Niski, miękki obrót przestrzeni bez mechanicznego zgrzytu.

**Docelowy czas / synchronizacja:** 0,20–0,35 s; kolejne spadanie brzmi osobno.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 16. Przetasowanie planszy: wieloelementowy ruch — `board_shuffle`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na rozpoczęciu przetasowania.

**Co ma zagrać:** Kilka przesunięć kamienia z jednym domknięciem, bez długiego grzechotania.

**Docelowy czas / synchronizacja:** 0,18–0,30 s; widoczne ruchy używają 0,12 s.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 17. Pojawienie się nowych kafelków: delikatne uzupełnienie — `tile_spawn`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: C.**

**Gdzie i kiedy:** gdy pojawiają się nowe kafelki.

**Co ma zagrać:** Miękki impuls pojawienia się materii, krótszy niż dźwięk zbierania nagrody.

**Docelowy czas / synchronizacja:** 0,10–0,18 s na grupę.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 18. Zakończenie jednej fali dopasowania: jeden akcent na falę — `match_wave_complete`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** raz po zakończeniu każdej fali.

**Co ma zagrać:** Spójny magiczny akcent wyniku fali, mocniejszy od pęknięć pojedynczych kafelków.

**Docelowy czas / synchronizacja:** 0,20–0,36 s; niezależnie od liczby kafelków.

**Miejsce w projekcie:** [Match3/Match3Board.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Match3/Match3Board.cs>).

### 19. Duże dopasowanie: mocniejszy wariant akcentu fali — `big_match`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** przy rozpoznaniu dużego dopasowania.

**Co ma zagrać:** Głębszy, szerszy wariant ukończenia fali; nie dubluje głośno Super Match.

**Docelowy czas / synchronizacja:** 0,28–0,48 s.

**Miejsce w projekcie:** [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>).

### 20. Kolejna fala kaskady — `cascade_wave`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Na planszy, przy każdej następnej fali dopasowania.

**Co ma zagrać:** Krótki impuls narastającej energii; kolejne fale mogą zwiększać intensywność. Osobny akcent dla każdej fali.

**Docelowy czas / synchronizacja:** 0,18–0,30 s na falę

**Miejsce w projekcie:** [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>), [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>).

### 21. Finał Super Match — `super_match`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Na planszy, w chwili rozpoznania Super Match.

**Co ma zagrać:** Pojedynczy pełniejszy finał energii kaskady. Nie utrzymywać tonu przez cały czas wyświetlania napisu.

**Docelowy czas / synchronizacja:** 0,35–0,55 s

**Miejsce w projekcie:** [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>), [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>).

### 22. Przyrost many po fali: krótki sygnał napełniania zasobu — `mana_gain`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na zmianie licznika many po fali.

**Co ma zagrać:** Krótki płynny wzrost energii w zasobie, cichszy od głównego akcentu fali.

**Docelowy czas / synchronizacja:** 0,14–0,28 s.

**Miejsce w projekcie:** [UI/UIPanelPlayerMana.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelPlayerMana.cs>).

### 23. Pojawienie się odłamków i dotarcie ich do licznika: lekki zbiór — `shard_collect`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** przy pojawieniu się odłamków; osobny finał na dolocie wymaga nowego wyzwalacza.

**Co ma zagrać:** Kilka delikatnych ziaren energii w ruchu do licznika; bez dźwięku trwającego do czasu usunięcia obiektu.

**Docelowy czas / synchronizacja:** 0,35–0,75 s; cząstki żyją 0,8 s, usunięcie obiektu po 2 s jest tylko sprzątaniem.

**Miejsce w projekcie:** [UI/VFXManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/VFXManager.cs>).

### 24. Premia kafelka: mana — `tile_bonus_mana`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Podczas rozliczania planszy, w chwili przyznania bonusowej many.

**Co ma zagrać:** Miękki, płynny impuls napełnienia zasobu; ta sama rodzina brzmień co pozostałe bonusy.

**Docelowy czas / synchronizacja:** 0,12–0,28 s

**Miejsce w projekcie:** [Tiles/TileUpgradeLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Tiles/TileUpgradeLogics.cs>).

### 25. Premia kafelka: leczenie — `tile_bonus_heal`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Podczas rozliczania planszy, przy przyznaniu bonusu HP.

**Co ma zagrać:** Mały ciepły puls odzyskania zdrowia; subtelniejszy od pełnego czaru leczącego.

**Docelowy czas / synchronizacja:** 0,12–0,28 s

**Miejsce w projekcie:** [Tiles/TileUpgradeLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Tiles/TileUpgradeLogics.cs>).

### 26. Premia kafelka: punkt akcji — `tile_bonus_action`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Podczas rozliczania planszy, przy przyznaniu punktu akcji.

**Co ma zagrać:** Krótki rytmiczny impuls gotowości, wyróżniający odzyskanie możliwości działania.

**Docelowy czas / synchronizacja:** 0,12–0,28 s

**Miejsce w projekcie:** [Tiles/TileUpgradeLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Tiles/TileUpgradeLogics.cs>).

### 27. Premia kafelka: tarcza — `tile_bonus_shield`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Podczas rozliczania planszy, przy przyznaniu bonusowej osłony.

**Co ma zagrać:** Miękkie zagęszczenie energii ochrony; niewielki akcent bez metalicznego dzwonienia.

**Docelowy czas / synchronizacja:** 0,12–0,28 s

**Miejsce w projekcie:** [Tiles/TileUpgradeLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Tiles/TileUpgradeLogics.cs>).

## Ataki umiejętności i statusy

### 28. Trafienie w tarczę: osobna, twardsza warstwa uderzenia — `shield_hit`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** na kontakcie z aktywną tarczą.

**Co ma zagrać:** Gęsty, sprężysty impuls bariery bez stalowego brzęku.

**Docelowy czas / synchronizacja:** 0,20–0,30 s; wizualny puls tarczy 0,22 s.

**Miejsce w projekcie:** [UI/BattleWorld.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/BattleWorld.cs>).

### 29. Śmierć przeciwnika — `enemy_death`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** W walce, przy animacji śmierci po ostatnim trafieniu.

**Co ma zagrać:** Zanik ciężaru i energii przeciwnika. Nie powtarzać akcentu uderzenia pocisku.

**Docelowy czas / synchronizacja:** 0,50–0,85 s

**Miejsce w projekcie:** [UI/BattleWorld.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/BattleWorld.cs>).

### 30. Śmierć gracza — `player_death`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** W walce, przy animacji śmierci gracza po ostatnim trafieniu.

**Co ma zagrać:** Cięższy, bardziej osobisty finał utraty energii. Oddzielić od samego trafienia i sygnału porażki.

**Docelowy czas / synchronizacja:** 0,65–1,00 s

**Miejsce w projekcie:** [UI/BattleWorld.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/BattleWorld.cs>).

### 31. Rzucenie umiejętności wroga — `enemy_skill_cast`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** W walce, przy użyciu umiejętności przez wroga, przed dolotem ewentualnego pocisku.

**Co ma zagrać:** Wspólna sygnatura magii lub ruchu wroga, z miejscem na akcent konkretnej umiejętności. Kontakt z celem brzmi później, osobno.

**Docelowy czas / synchronizacja:** 0,18–0,38 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [UI/BattleWorld.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/BattleWorld.cs>).

### 32. Leczenie — `healing`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, na rzeczywistym wzroście HP wskutek leczenia.

**Co ma zagrać:** Ciepły przepływ energii z miękkim domknięciem, czytelnie sygnalizujący odzyskanie zdrowia.

**Docelowy czas / synchronizacja:** 0,30–0,55 s

**Miejsce w projekcie:** [BattleEffect/HealLogic.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/BattleEffect/HealLogic.cs>), [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>).

### 33. Tick regeneracji — `regeneration_tick`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, przy każdym rzeczywistym wzroście HP od regeneracji na turze.

**Co ma zagrać:** Pojedynczy mały ciepły puls; nie ciągły dźwięk przez wszystkie tury statusu.

**Docelowy czas / synchronizacja:** 0,16–0,30 s

**Miejsce w projekcie:** [BattleEffect/HealLogic.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/BattleEffect/HealLogic.cs>), [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>).

### 34. Nałożenie statusu — `status_apply`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, gdy postać otrzymuje nowy status.

**Co ma zagrać:** Miękki akcent pojawienia się runicznego znaku, z krótkim końcem.

**Docelowy czas / synchronizacja:** 0,14–0,26 s

**Miejsce w projekcie:** [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>), [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>).

### 35. Wzmocnienie statusu — `status_strengthen`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, przy zwiększeniu siły istniejącego statusu.

**Co ma zagrać:** Pełniejszy puls tej samej runicznej rodziny, wyraźniejszy niż samo nałożenie.

**Docelowy czas / synchronizacja:** 0,18–0,30 s

**Miejsce w projekcie:** [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>), [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>).

### 36. Wygaśnięcie statusu — `status_expire`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, gdy status się kończy.

**Co ma zagrać:** Ciche rozpuszczenie lub odpływ energii znaku. Czas trwania statusu w turach nie określa długości dźwięku.

**Docelowy czas / synchronizacja:** 0,14–0,26 s

**Miejsce w projekcie:** [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>), [Battle/BattleCharacter.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleCharacter.cs>).

### 37. Przemiana many — skutek akcji — `mana_transmute`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, po potwierdzonej zamianie zasobu między kolorami.

**Co ma zagrać:** Płynny przepływ energii między barwami; wspólny efekt skutku, odrębny od akcentu użycia TransmuteMana.

**Docelowy czas / synchronizacja:** 0,30–0,50 s

**Miejsce w projekcie:** [BattleEffect/SkillEffectLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/BattleEffect/SkillEffectLogics.cs>).

### 38. Odzyskanie karty — skutek akcji — `card_recover`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, gdy karta zostaje faktycznie odzyskana.

**Co ma zagrać:** Krótki odwrócony szelest papieru. Nie dublować głośno akcentu umiejętności Recycle.

**Docelowy czas / synchronizacja:** 0,18–0,32 s

**Miejsce w projekcie:** [BattleEffect/SkillEffectLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/BattleEffect/SkillEffectLogics.cs>).

### 39. Zebranie dusz — skutek akcji — `soul_collect`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W walce, przy faktycznym zebraniu dusz.

**Co ma zagrać:** Ciche zbliżające się oddechy energii zbiegające się w punkt zbioru.

**Docelowy czas / synchronizacja:** 0,30–0,55 s

**Miejsce w projekcie:** [BattleEffect/SkillEffectLogics.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/BattleEffect/SkillEffectLogics.cs>).

## Wynik walki i rozwój postaci

### 40. Zwycięstwo — `victory`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Po ustaleniu zwycięskiego wyniku walki.

**Co ma zagrać:** Ciepłe rozjaśnienie energii i mocny krótki finał, bez metalicznych dzwonków.

**Docelowy czas / synchronizacja:** 0,70–1,15 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [UI/UIEndBattlePanel.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIEndBattlePanel.cs>).

### 41. Porażka — `defeat`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: A.**

**Gdzie i kiedy:** Po ustaleniu przegranego wyniku walki.

**Co ma zagrać:** Opadnięcie energii i zamknięcie napięcia, bez przesadnie dramatycznej fanfary.

**Docelowy czas / synchronizacja:** 0,60–1,00 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [UI/UIEndBattlePanel.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIEndBattlePanel.cs>).

### 42. Pojawienie się nagród i odłamków po walce — `battle_rewards_reveal`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** gdy panel odsłania nagrody.

**Co ma zagrać:** Kilka delikatnych pojawień i jedno wspólne osadzenie, bez odtwarzania dźwięku przez cały czas oglądania panelu.

**Docelowy czas / synchronizacja:** 0,35–0,65 s; liczba nagród jest zmienna.

**Miejsce w projekcie:** [UI/UIEndBattlePanel.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIEndBattlePanel.cs>).

### 43. Napełnianie paska doświadczenia — `experience_fill`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** na początku każdego odcinka animacji paska.

**Co ma zagrać:** Cicha płynąca energia, która ustaje dokładnie z paskiem; poziom ma osobny akcent, więc nie przedłużać stałego one-shotu.

**Docelowy czas / synchronizacja:** dynamicznie 0,15–0,90 s na odcinek; pauza przy poziomie 0,15 s.

**Miejsce w projekcie:** [UI/UIEndBattlePanel.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIEndBattlePanel.cs>).

### 44. Odsłonięcie nagrody po bossie — `boss_reward_reveal`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Na ekranie nagrody po walce z bossem, przy jej pokazaniu.

**Co ma zagrać:** Głębszy magiczny rozkwit wyróżniający ważniejszą nagrodę.

**Docelowy czas / synchronizacja:** 0,50–0,90 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [UI/UIPanelGainSkill.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelGainSkill.cs>).

### 45. Wybór nowej umiejętności — `new_skill_choose`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** Na ekranie nagrody, po zatwierdzeniu wybranej umiejętności.

**Co ma zagrać:** Zwięzłe potwierdzenie wyboru, bez powtarzania fanfary odsłonięcia nagrody.

**Docelowy czas / synchronizacja:** 0,14–0,28 s

**Miejsce w projekcie:** [Battle/BattleManager.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/Battle/BattleManager.cs>), [UI/UIPanelGainSkill.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelGainSkill.cs>).

### 46. Wyposażenie umiejętności — `skill_equip`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W panelu umiejętności, po zatwierdzeniu dodania do wyposażenia.

**Co ma zagrać:** Miękkie wsunięcie energii w slot, kończące się dyskretnym osadzeniem.

**Docelowy czas / synchronizacja:** 0,11–0,22 s

**Miejsce w projekcie:** [UI/UIPanelChooseSkill.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelChooseSkill.cs>).

### 47. Zdjęcie umiejętności — `skill_unequip`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W panelu umiejętności, po zatwierdzeniu usunięcia z wyposażenia.

**Co ma zagrać:** Odwrotny kontur wyposażenia: krótkie wysunięcie lub odpływ energii ze slotu.

**Docelowy czas / synchronizacja:** 0,11–0,22 s

**Miejsce w projekcie:** [UI/UIPanelChooseSkill.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelChooseSkill.cs>).

### 48. Zmiana filtra kart — `card_filter_change`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: C.**

**Gdzie i kiedy:** W panelu talii/listy kart, po zmianie filtra.

**Co ma zagrać:** Dyskretny impuls UI potwierdzający zmianę; nie szelest przewracanej strony.

**Docelowy czas / synchronizacja:** 0,07–0,14 s

**Miejsce w projekcie:** [UI/UIPanelPlayerCards.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelPlayerCards.cs>).

### 49. Dodanie punktu statystyki — `stat_point_add`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W panelu statystyk, po dodaniu punktu do wybranej cechy.

**Co ma zagrać:** Mały miękki impuls wzrostu, bez pełnego akcentu zatwierdzenia wszystkich zmian.

**Docelowy czas / synchronizacja:** 0,08–0,16 s

**Miejsce w projekcie:** [UI/UIPanelPlayerStats.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelPlayerStats.cs>).

### 50. Cofnięcie punktu statystyki — `stat_point_undo`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W panelu statystyk, po cofnięciu przydziału punktu.

**Co ma zagrać:** Miękki impuls o przeciwnym kierunku niż dodanie punktu; neutralny, bez alarmu błędu.

**Docelowy czas / synchronizacja:** 0,08–0,16 s

**Miejsce w projekcie:** [UI/UIPanelPlayerStats.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelPlayerStats.cs>).

### 51. Zatwierdzenie punktów statystyki — `stat_points_confirm`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W panelu statystyk, po zatwierdzeniu całego przydziału.

**Co ma zagrać:** Pełniejszy, lecz krótki puls domykający konfigurację.

**Docelowy czas / synchronizacja:** 0,18–0,30 s

**Miejsce w projekcie:** [UI/UIPanelPlayerStats.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelPlayerStats.cs>).

### 52. Wybór ulepszenia kafelka — `tile_upgrade_select`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W kuźni/craftingu kafelków, po wybraniu ulepszenia.

**Co ma zagrać:** Miękki impuls runy, wyróżniony od fizycznego wyboru typu kafelka.

**Docelowy czas / synchronizacja:** 0,08–0,18 s

**Miejsce w projekcie:** [UI/UIPanelTileCrafting.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelTileCrafting.cs>).

### 53. Wybór slotu kafelka — `tile_slot_select`

**Stan:** poprzednia propozycja odrzucona — potrzebny zamiennik. **Priorytet: B.**

**Gdzie i kiedy:** W kuźni/craftingu kafelków, po wskazaniu slotu na ulepszenie.

**Co ma zagrać:** Krótki, miękki kontakt osadzający wybór w slocie. Poprzednia próbka kamiennego kontaktu została odrzucona.

**Docelowy czas / synchronizacja:** 0,08–0,18 s

**Miejsce w projekcie:** [UI/UIPanelTileCrafting.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelTileCrafting.cs>).

### 54. Usunięcie ulepszenia kafelka — `tile_upgrade_remove`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: B.**

**Gdzie i kiedy:** W kuźni/craftingu kafelków, po usunięciu ulepszenia.

**Co ma zagrać:** Cichy odpływ energii z runy/slotu, odróżniony od dodawania ulepszenia.

**Docelowy czas / synchronizacja:** 0,12–0,24 s

**Miejsce w projekcie:** [UI/UIPanelTileCrafting.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIPanelTileCrafting.cs>).

### 55. Najechanie na kartę, umiejętność lub element z opisem: bardzo subtelny sygnał UI — `ui_hover`

**Stan:** brak zaakceptowanej propozycji. **Priorytet: C.**

**Gdzie i kiedy:** po rozpoczęciu hover, tylko gdy podpowiedź rzeczywiście pomaga.

**Co ma zagrać:** Prawie niesłyszalny miękki dotyk UI, bez tonu utrzymującego się podczas hover.

**Docelowy czas / synchronizacja:** 0,05–0,10 s; odtwarzacz ogranicza cue do odstępów 0,08 s.

**Miejsce w projekcie:** [UI/UIBoardActionCard.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UIBoardActionCard.cs>), [UI/UISkillButton.cs](<D:/Unity/projs/M3p/Assets/_Project/Scripts/UI/UISkillButton.cs>).

## Umiejętności — Podstawowe

### 56. Wspólna warstwa klasy — `basic_layer`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, jako wspólny podkład aktywacji umiejętności grupy „Podstawowe”, równolegle z jej własnym akcentem.

**Co ma zagrać:** Delikatny wspólny impuls energii; zostawia miejsce na akcent konkretnej umiejętności.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,12–0,20 s

### 57. BasicHeal — `basic_heal`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: BasicHeal (Podstawowe).

**Co ma zagrać:** Ciepłe skupienie energii i miękkie rozjaśnienie HP.

**Docelowy czas / synchronizacja:** Przy leczeniu, 0,25–0,45 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/BasicHeal.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/BasicHeal.asset>).

### 58. ShieldBash — `shield_bash`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: ShieldBash (Podstawowe).

**Co ma zagrać:** Krótki cięższy pchnięty ruch tarczy; trafienie oddzielić na kontakt celu.

**Docelowy czas / synchronizacja:** Przy zamachu, 0,18–0,32 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/ShieldBash.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/ShieldBash.asset>).

## Umiejętności — Wrogowie

### 59. EnemySlam — `enemy_slam`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: EnemySlam (Wrogowie).

**Co ma zagrać:** Niski ruch ciężaru do uderzenia; szczyt uderzenia należy do SFX trafienia.

**Docelowy czas / synchronizacja:** Przy zamachu, 0,22–0,40 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Enemies/EnemySlam.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Enemies/EnemySlam.asset>).

## Umiejętności — Mag

### 60. Wspólna warstwa klasy — `mage_layer`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, jako wspólny podkład aktywacji umiejętności grupy „Mag”, równolegle z jej własnym akcentem.

**Co ma zagrać:** Cichy oddech skupianej many, bez jasnych dzwonków.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,14–0,24 s

### 61. Convergence — `convergence`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Convergence (Mag).

**Co ma zagrać:** Trzy subtelne strumienie zbiegają się w jeden puls obrażeń, leczenia i osłony.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,38–0,62 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Mage/Convergence.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Mage/Convergence.asset>).

### 62. Renew — `renew`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Renew (Mag).

**Co ma zagrać:** Ciepły puls odnowy; cztery późniejsze ticki leczenia mają oddzielny krótki sygnał.

**Docelowy czas / synchronizacja:** Przy nałożeniu regeneracji, 0,22–0,38 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Mage/Renew.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Mage/Renew.asset>).

### 63. TransmuteMana — `transmute_mana`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: TransmuteMana (Mag).

**Co ma zagrać:** Płynne przelanie do trzech punktów many między dwiema barwami.

**Docelowy czas / synchronizacja:** Przy zatwierdzonej zamianie koloru, 0,24–0,42 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Mage/TransmuteMana.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Mage/TransmuteMana.asset>).

## Umiejętności — Wojownik

### 64. Wspólna warstwa klasy — `warrior_layer`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, jako wspólny podkład aktywacji umiejętności grupy „Wojownik”, równolegle z jej własnym akcentem.

**Co ma zagrać:** Matowy ruch skóry, ciężaru i oddechu postawy; bez metalicznych dzwonków.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,12–0,22 s

### 65. BattleFrenzy — `battle_frenzy`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: BattleFrenzy (Wojownik).

**Co ma zagrać:** Narastający rytm oddechu i impetu; wcześniejsze Resolve zwiększają siłę, nie długość cue.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,18–0,34 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/BattleFrenzy.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/BattleFrenzy.asset>).

### 66. Fortress — `fortress`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Fortress (Wojownik).

**Co ma zagrać:** Warstwy masywnej osłony domykają się w jednym gęstym pulsie; liczba grup zwiększa siłę tarczy, nie czas.

**Docelowy czas / synchronizacja:** Przy nałożeniu ochrony, 0,24–0,42 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/Fortress.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/Fortress.asset>).

### 67. GuardStance — `guard_stance`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: GuardStance (Wojownik).

**Co ma zagrać:** Stabilny krok i miękkie zaryglowanie osłony bez ostrego rezonansu.

**Docelowy czas / synchronizacja:** Przy ustawieniu gardy, 0,18–0,34 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/GuardStance.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/GuardStance.asset>).

### 68. PerfectStrike — `perfect_strike`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: PerfectStrike (Wojownik).

**Co ma zagrać:** Bardzo skupiony, szybki ruch; udane pojedyncze dopasowanie może wzmocnić akcent trafienia.

**Docelowy czas / synchronizacja:** Przy precyzyjnym zamachu, 0,14–0,26 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/PerfectStrike.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/PerfectStrike.asset>).

### 69. Riposte — `riposte`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Riposte (Wojownik).

**Co ma zagrać:** Cicha napięta garda teraz, a kontratak dopiero na następnym wrogim trafieniu.

**Docelowy czas / synchronizacja:** Przy przygotowaniu kontry, 0,18–0,32 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/Riposte.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/Riposte.asset>).

### 70. Unyielding — `unyielding`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Unyielding (Wojownik).

**Co ma zagrać:** Niski, spokojny puls wytrzymałości; późniejszy wzmocniony atak ma własne SFX.

**Docelowy czas / synchronizacja:** Przy wzmocnieniu obrony, 0,22–0,40 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Warrior/Unyielding.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Warrior/Unyielding.asset>).

## Umiejętności — Cień

### 71. Wspólna warstwa klasy — `shadow_layer`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, jako wspólny podkład aktywacji umiejętności grupy „Cień”, równolegle z jej własnym akcentem.

**Co ma zagrać:** Cichy ruch powietrza i przytłumionej energii, bez szklistych tonów.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,10–0,20 s

### 72. BladeDance — `blade_dance`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: BladeDance (Cień).

**Co ma zagrać:** Zwinny wir kilku lekkich ruchów; liczba kart skaluje obrażenia, nie narzuca serii osobnych trafień.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,18–0,36 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/BladeDance.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/BladeDance.asset>).

### 73. BloodPact — `blood_pact`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: BloodPact (Cień).

**Co ma zagrać:** Niski organiczny puls ofiary przechodzący w energię działania; bez głosu lub bólu postaci.

**Docelowy czas / synchronizacja:** Przy utracie HP i zyskaniu AP, 0,22–0,38 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/BloodPact.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/BloodPact.asset>).

### 74. ExposeWeakness — `expose_weakness`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: ExposeWeakness (Cień).

**Co ma zagrać:** Cienka szczelina w ochronie i cichy mroczny znak; trzy przyszłe trafienia nie brzmią teraz.

**Docelowy czas / synchronizacja:** Przy oznaczeniu celu, 0,18–0,32 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/ExposeWeakness.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/ExposeWeakness.asset>).

### 75. KillingSequence — `killing_sequence`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: KillingSequence (Cień).

**Co ma zagrać:** Skupiona seria gestów kończąca się jednym akcentem; liczba kart skaluje obrażenia, nie tworzy automatycznie osobnych uderzeń.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,22–0,40 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/KillingSequence.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/KillingSequence.asset>).

### 76. Momentum — `momentum`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Momentum (Cień).

**Co ma zagrać:** Szybki przepływ energii w ruch karty; jeśli dobrane są dwie, drugi lekki szelest kart osobno.

**Docelowy czas / synchronizacja:** Przy dobraniu kart, 0,16–0,30 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/Momentum.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/Momentum.asset>).

### 77. QuickCut — `quick_cut`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: QuickCut (Cień).

**Co ma zagrać:** Dwa krótkie cięcia z bliskim odstępem tylko gdy animacja rozdziela trafienia; kod liczy je synchronicznie.

**Docelowy czas / synchronizacja:** Przy użyciu, 0,14–0,28 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/QuickCut.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/QuickCut.asset>).

### 78. Recycle — `recycle`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: Recycle (Cień).

**Co ma zagrać:** Odwrócony ruch papieru przez cień; oszczędnie, by nie dublować osobnego CardRecover.

**Docelowy czas / synchronizacja:** Przy odzyskaniu karty, 0,18–0,32 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/Recycle.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/Recycle.asset>).

### 79. ShadowHarvest — `shadow_harvest`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: ShadowHarvest (Cień).

**Co ma zagrać:** Ciche smugi energii zbiegają się w jeden zbiór; liczba zniszczonych kafelków reguluje intensywność.

**Docelowy czas / synchronizacja:** Przy zebraniu dusz, 0,22–0,42 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/ShadowHarvest.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/ShadowHarvest.asset>).

### 80. SoulBlade — `soul_blade`

**Stan:** brak zaakceptowanej propozycji.

**Gdzie i kiedy:** W walce, przy aktywacji umiejętności: SoulBlade (Cień).

**Co ma zagrać:** Matowa energia dusz nasyca ostrze; do czterech trafień wymaga osobnych, krótkich akcentów tylko po rozdzieleniu ich w czasie.

**Docelowy czas / synchronizacja:** Przy aktywacji, 0,22–0,42 s

**Miejsce w projekcie:** [Assets/_Project/Configs/Skills/Player/Shadow/SoulBlade.asset](<D:/Unity/projs/M3p/Assets/_Project/Configs/Skills/Player/Shadow/SoulBlade.asset>).

## Zależności przy montażu

- `experience_fill`: dźwięk kończy się wraz z każdym odcinkiem paska XP; wymaga kontrolowanego stop/fade zamiast stałego długiego pliku.
- `shard_collect`: osobny akcent na dolocie odłamków wymaga dodatkowego wyzwalacza; czas usunięcia VFX nie jest czasem dźwięku.
- Wspólne warstwy klas i akcenty umiejętności mogą grać równolegle. Warstwy muszą być cichsze, aby nie podwajać siły efektu.
- Akcent użycia umiejętności i reakcja celu to różne zdarzenia. Np. `basic_heal` / `healing`, `transmute_mana` / `mana_transmute`, `recycle` / `card_recover` mogą korzystać ze wspólnej rodziny, ale trzeba uniknąć podwójnego głośnego odtworzenia.
- Karta Rewind jest wyłączona w źródłowej liście i nie została dodana do braków.

Źródła: [lista zdarzeń i opisów](M3p_miejsca_na_dzwiek.md), [oceny i przypisania](M3p_dobor_dzwiekow_v1.md), [zapisany eksport ocen](AudioSelection/review_decisions.json).
