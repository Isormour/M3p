# Miejsca na dźwięk w projekcie M3p

Lista wskazuje momenty, w których efekt dźwiękowy potwierdzi akcję gracza lub wyjaśni wynik zdarzenia. Obejmuje obecną pętlę menu, mapy, walki i rozwoju postaci. Jeden plik audio może obsłużyć kilka podobnych zdarzeń.

Ścieżki skryptów w punktach poniżej są względne wobec `Assets/_Project/Scripts`, a ścieżki SFX wobec katalogu głównego projektu. **A** oznacza podstawową czytelność walki, **B** pełne sprzężenie zwrotne, a **C** końcowe dopracowanie interfejsu i ruchu. Każda pozycja jest propozycją do wdrożenia.

W `Assets/_Project/Audio/SFX` znajdują się wygenerowane próbki. Ścieżki poniżej wskazują pliki audio; samo wyzwalanie SFX w skryptach gry pozostaje do wdrożenia.

## Menu i mapa

- [ ] **B** Wybór Kontynuuj, Nowa gra, Mapa testowa lub Wyjdź: klik potwierdzenia. `UI/MainMenu.cs:47` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/menu_confirm.mp3`
- [ ] **B** Przejście menu -> mapa -> walka i powrót: krótki dźwięk przejścia. `Core/SceneFlow.cs:12` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/scene_transition.mp3`
- [ ] **B** Kliknięcie dostępnego węzła mapy: wybór punktu podróży. `Map/MapManager.cs:436` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_node_select.mp3`
- [ ] **B** Otwarcie podglądu i potwierdzenia drogi: miękki sygnał panelu. `Map/MapManager.cs:459` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/route_preview.mp3`
- [ ] **B** Potwierdzenie lub anulowanie drogi: dwa krótkie warianty. `UI/UIMapPanelWalkNodeConfirm.cs:99` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/route_confirm.mp3`, `Assets/_Project/Audio/SFX/MenuMap/route_cancel.mp3`
- [ ] **C** Ruch pionka po mapie: kroki dopasowane do animacji, nie do każdej klatki. `Map/MapPlayerToken.cs:63` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_token_step.mp3`
- [ ] **B** Dotarcie do węzła: akcent zakończenia podróży. `Map/MapManager.cs:514` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/map_arrival.mp3`
- [ ] **A** Wejście do walki zwykłej, z elitą lub bossem: warianty narastającego sygnału. `Map/MapManager.cs:582` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/encounter_normal.mp3`, `Assets/_Project/Audio/SFX/MenuMap/encounter_elite.mp3`, `Assets/_Project/Audio/SFX/MenuMap/encounter_boss.mp3`
- [ ] **B** Otwarcie sklepu z kartami: dźwięk wejścia do usługi. `Map/MapManager.cs:596` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/card_shop_open.mp3`
- [ ] **B** Otwarcie kuźni kafelków: metaliczny akcent. `Map/MapManager.cs:609` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/forge_open.mp3`
- [ ] **B** Otwarcie skrzyni: zatrzask i pokrywa. `Map/MapManager.cs:648` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_open.mp3`
- [ ] **B** Odebranie nagrody ze skrzyni: krótki dźwięk zdobyczy. `Map/MapManager.cs:670` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_reward.mp3`
- [ ] **C** Pusta skrzynia i potwierdzenie okna zdarzenia: neutralny klik. `Map/MapManager.cs:573; Map/MapEventPresenter.cs:117` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/chest_empty.mp3`
- [ ] **C** Otwieranie i zamykanie paneli statystyk, talii, craftingu i umiejętności. `UI/UIMapPlayerStats.cs:117; UI/UIPanelClosable.cs:74` **SFX:** `Assets/_Project/Audio/SFX/MenuMap/panel_open.mp3`, `Assets/_Project/Audio/SFX/MenuMap/panel_close.mp3`

## Karty i przebieg tury

- [ ] **A** Początek walki: sygnał wejścia na planszę. `Battle/BattleManager.cs:305` **SFX:** `Assets/_Project/Audio/SFX/Cards/battle_start.mp3`
- [ ] **B** Początek tury gracza i odzyskanie kontroli: krótki znacznik tury. `Battle/BattleManager.cs:735` **SFX:** `Assets/_Project/Audio/SFX/Cards/player_turn_start.mp3`
- [ ] **B** Dobranie ręki na początku tury: szelest talii. `Cards/CardPlayController.cs:107` **SFX:** `Assets/_Project/Audio/SFX/Cards/hand_draw.mp3`
- [ ] **B** Dobranie dodatkowej karty w środku tury: pojedyncze dobranie. `Cards/CardPlayController.cs:149` **SFX:** `Assets/_Project/Audio/SFX/Cards/single_card_draw.mp3`
- [ ] **A** Wybranie karty z ręki: lekki klik lub szelest. `Cards/CardPlayController.cs:205` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_select.mp3`
- [ ] **B** Odznaczenie karty albo anulowanie celowania: cofnięcie wyboru. `Cards/CardPlayController.cs:236` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_deselect.mp3`
- [ ] **A** Wskazanie poprawnego kafelka jako celu karty: krótki klik pola. `Cards/CardPlayController.cs:298` **SFX:** `Assets/_Project/Audio/SFX/Cards/target_tile_select.mp3`
- [ ] **B** Wskazanie ostatniego wymaganego celu: akcent domknięcia wyboru. `Cards/CardPlayController.cs:323` **SFX:** `Assets/_Project/Audio/SFX/Cards/target_selection_complete.mp3`
- [ ] **B** Otwarcie dodatkowego wyboru koloru lub kierunku grawitacji. `Cards/CardPlayController.cs:337` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_choice_open.mp3`
- [ ] **B** Potwierdzenie lub anulowanie dodatkowego wyboru. `UI/UICardChoiceOverlay.cs:155` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_choice_confirm.mp3`, `Assets/_Project/Audio/SFX/Cards/card_choice_cancel.mp3`
- [ ] **A** Dodanie karty do kolejki: wyraźny klik zatwierdzenia akcji. `Cards/CardPlayController.cs:382` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_queue_add.mp3`
- [ ] **A** Cofnięcie ostatniej karty z kolejki: krótki dźwięk powrotu. `Cards/CardPlayController.cs:256` **SFX:** `Assets/_Project/Audio/SFX/Cards/card_queue_undo.mp3`
- [ ] **A** Naciśnięcie Resolve i start odtwarzania kolejki: sygnał uruchomienia. `UI/UIPanelCardHand.cs:152; Cards/CardPlayController.cs:277` **SFX:** `Assets/_Project/Audio/SFX/Cards/resolve_start.mp3`
- [ ] **C** Odrzucenie zagranych kart po Resolve: szelest stosu odrzuconych. `Cards/CardPlayController.cs:285` **SFX:** `Assets/_Project/Audio/SFX/Cards/cards_discard.mp3`
- [ ] **B** Naciśnięcie Koniec tury i odrzucenie ręki: zamknięcie tury. `UI/UIPanelCardHand.cs:162; Cards/CardPlayController.cs:116` **SFX:** `Assets/_Project/Audio/SFX/Cards/player_turn_end.mp3`
- [ ] **B** Początek i zakończenie tury przeciwnika: dyskretny sygnał zmiany strony. `Battle/BattleManager.cs:759; Battle/BattleManager.cs:785` **SFX:** `Assets/_Project/Audio/SFX/Cards/enemy_turn_start.mp3`, `Assets/_Project/Audio/SFX/Cards/enemy_turn_end.mp3`

## Plansza i zasoby

- [ ] **A** Zamiana dwóch kafelków: krótki ruch przestrzenny. `Match3/Match3Board.cs:526` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_swap.mp3`
- [ ] **B** Obrót grupy kafelków lub przesunięcie rzędu: dłuższy ruch. `Match3/Match3Board.cs:595` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_rotate.mp3`, `Assets/_Project/Audio/SFX/Board/row_shift.mp3`
- [ ] **B** Przemalowanie kafelka: zmiana barwy lub magiczne muśnięcie. `Match3/Match3Board.cs:680` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_recolor.mp3`
- [ ] **B** Oznaczenie kafelka do rozbicia: krótki trzask pęknięcia. `Match3/Match3Board.cs:431` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_crack_mark.mp3`
- [ ] **A** Faktyczne rozbicie kafelków: główny odgłos dopasowania. `Match3/Match3Board.cs:800` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_break.mp3`
- [ ] **B** Usunięcie blokady lub negatywnego kafelka przez Purge. `Match3/Match3Board.cs:576` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_purge.mp3`
- [ ] **B** Zmiana kierunku grawitacji: niski akcent lub obrót przestrzeni. `Match3/Match3Board.cs:439` **SFX:** `Assets/_Project/Audio/SFX/Board/gravity_shift.mp3`
- [ ] **B** Przetasowanie planszy: wieloelementowy ruch. `Match3/Match3Board.cs:638` **SFX:** `Assets/_Project/Audio/SFX/Board/board_shuffle.mp3`
- [ ] **C** Opadnięcie kafelków w puste miejsca: lekki dźwięk osiadania. `Match3/Match3Board.cs:886` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_fall.mp3`
- [ ] **C** Pojawienie się nowych kafelków: delikatne uzupełnienie. `Match3/Match3Board.cs:911` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_spawn.mp3`
- [ ] **A** Zakończenie jednej fali dopasowania: jeden akcent na falę. `Match3/Match3Board.cs:496` **SFX:** `Assets/_Project/Audio/SFX/Board/match_wave_complete.mp3`
- [ ] **A** Duże dopasowanie: mocniejszy wariant akcentu fali. `UI/VFXManager.cs:447` **SFX:** `Assets/_Project/Audio/SFX/Board/big_match.mp3`
- [ ] **A** Kolejna fala kaskady i Super Match: narastający ton oraz osobny finał. `UI/VFXManager.cs:412; UI/VFXManager.cs:447` **SFX:** `Assets/_Project/Audio/SFX/Board/cascade_wave.mp3`, `Assets/_Project/Audio/SFX/Board/super_match.mp3`
- [ ] **B** Przyrost many po fali: krótki sygnał napełniania zasobu. `UI/UIPanelPlayerMana.cs:149` **SFX:** `Assets/_Project/Audio/SFX/Board/mana_gain.mp3`
- [ ] **B** Pojawienie się odłamków i dotarcie ich do licznika: lekki zbiór. `UI/VFXManager.cs:517` **SFX:** `Assets/_Project/Audio/SFX/Board/shard_collect.mp3`
- [ ] **B** Premia z kafelka: mana, HP, punkty akcji, tarcza lub dodatkowa karta. `Tiles/TileUpgradeLogics.cs:9` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_bonus_mana.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_heal.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_action.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_shield.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_card.mp3`
- [ ] **B** Premia z kafelka: podpalenie przeciwnika lub rozbicie sąsiada. `Tiles/TileUpgradeLogics.cs:83` **SFX:** `Assets/_Project/Audio/SFX/Board/tile_bonus_ignite.mp3`, `Assets/_Project/Audio/SFX/Board/tile_bonus_neighbor_break.mp3`

## Ataki umiejętności i statusy

- [ ] **A** Zapowiedź planowanej akcji wroga: ostrzegawczy sygnał zamiaru. `Battle/EnemyBattleCharacter.cs:25` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_intent.mp3`
- [ ] **A** Zamach przed podstawowym atakiem gracza. `Battle/BattleManager.cs:620` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_attack_windup.mp3`
- [ ] **A** Wypuszczenie pocisku podstawowego ataku. `Battle/BattleManager.cs:630; UI/VFXManager.cs:426` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_projectile_launch.mp3`
- [ ] **A** Dolot pocisku i trafienie przeciwnika. `UI/VFXManager.cs:434` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_projectile_hit.mp3`
- [ ] **A** Trafienie gracza przez umiejętność wroga. `UI/VFXManager.cs:507` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_hit.mp3`
- [ ] **A** Trafienie w tarczę: osobna, twardsza warstwa uderzenia. `UI/BattleWorld.cs:136` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_hit.mp3`
- [ ] **B** Pojawienie się i zniknięcie tarczy. `Battle/BattleCharacterShield.cs:94` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_appear.mp3`, `Assets/_Project/Audio/SFX/Combat/shield_fade.mp3`
- [ ] **A** Śmierć przeciwnika lub gracza: różne akcenty finału. `UI/BattleWorld.cs:136` **SFX:** `Assets/_Project/Audio/SFX/Combat/enemy_death.mp3`, `Assets/_Project/Audio/SFX/Combat/player_death.mp3`
- [ ] **A** Rzucenie umiejętności gracza i wroga: brzmienie przypisane do typu umiejętności. `Battle/BattleManager.cs:177; UI/BattleWorld.cs:120` **SFX:** `Assets/_Project/Audio/SFX/Combat/player_skill_cast.mp3`, `Assets/_Project/Audio/SFX/Combat/enemy_skill_cast.mp3`
- [ ] **B** Kolejne trafienia umiejętności wielokrotnego ataku. `BattleEffect/SkillEffectLogics.cs:314` **SFX:** `Assets/_Project/Audio/SFX/Combat/multi_hit_tick.mp3`
- [ ] **B** Leczenie oraz regeneracja przy turze: odrębne akcenty. `BattleEffect/HealLogic.cs:25; Battle/BattleCharacter.cs:313` **SFX:** `Assets/_Project/Audio/SFX/Combat/healing.mp3`, `Assets/_Project/Audio/SFX/Combat/regeneration_tick.mp3`
- [ ] **B** Nałożenie tarczy lub efektu obronnego. `BattleEffect/AddShieldLogic.cs:25` **SFX:** `Assets/_Project/Audio/SFX/Combat/shield_apply.mp3`
- [ ] **B** Nałożenie, wzmocnienie i wygaśnięcie statusu. `Battle/BattleCharacter.cs:224; Battle/BattleCharacter.cs:282` **SFX:** `Assets/_Project/Audio/SFX/Combat/status_apply.mp3`, `Assets/_Project/Audio/SFX/Combat/status_strengthen.mp3`, `Assets/_Project/Audio/SFX/Combat/status_expire.mp3`
- [ ] **B** Obrażenia od podpalenia na początku tury. `Battle/BattleCharacter.cs:313; UI/BattleWorld.cs:183` **SFX:** `Assets/_Project/Audio/SFX/Combat/burn_tick.mp3`
- [ ] **B** Przemiana many, poświęcenie HP, odzyskanie karty i zebranie dusz. `BattleEffect/SkillEffectLogics.cs:375` **SFX:** `Assets/_Project/Audio/SFX/Combat/mana_transmute.mp3`, `Assets/_Project/Audio/SFX/Combat/hp_sacrifice.mp3`, `Assets/_Project/Audio/SFX/Combat/card_recover.mp3`, `Assets/_Project/Audio/SFX/Combat/soul_collect.mp3`

## Wynik walki i rozwój postaci

- [ ] **A** Zwycięstwo lub porażka: dwa wyraźne sygnały końca walki. `Battle/BattleManager.cs:823; UI/UIEndBattlePanel.cs:92` **SFX:** `Assets/_Project/Audio/SFX/Rewards/victory.mp3`, `Assets/_Project/Audio/SFX/Rewards/defeat.mp3`
- [ ] **B** Pojawienie się nagród i odłamków po walce. `UI/UIEndBattlePanel.cs:112` **SFX:** `Assets/_Project/Audio/SFX/Rewards/battle_rewards_reveal.mp3`
- [ ] **B** Napełnianie paska doświadczenia. `UI/UIEndBattlePanel.cs:289` **SFX:** `Assets/_Project/Audio/SFX/Rewards/experience_fill.mp3`
- [ ] **B** Awans poziomu i pokazanie punktów statystyk. `UI/UIEndBattlePanel.cs:315` **SFX:** `Assets/_Project/Audio/SFX/Rewards/level_up.mp3`
- [ ] **B** Zamknięcie wyniku walki oraz powrót na mapę. `UI/UIEndBattlePanel.cs:214; Battle/BattleManager.cs:855` **SFX:** `Assets/_Project/Audio/SFX/Rewards/battle_result_close.mp3`
- [ ] **B** Pokazanie nagrody po bossie i wybór nowej umiejętności. `Battle/BattleManager.cs:891; UI/UIPanelGainSkill.cs:134` **SFX:** `Assets/_Project/Audio/SFX/Rewards/boss_reward_reveal.mp3`, `Assets/_Project/Audio/SFX/Rewards/new_skill_choose.mp3`
- [ ] **B** Dodanie lub usunięcie umiejętności z wyposażenia. `UI/UIPanelChooseSkill.cs:165` **SFX:** `Assets/_Project/Audio/SFX/Rewards/skill_equip.mp3`, `Assets/_Project/Audio/SFX/Rewards/skill_unequip.mp3`
- [ ] **B** Dodanie lub usunięcie karty z talii. `UI/UIPanelPlayerCards.cs:304` **SFX:** `Assets/_Project/Audio/SFX/Rewards/deck_card_add.mp3`, `Assets/_Project/Audio/SFX/Rewards/deck_card_remove.mp3`
- [ ] **C** Zmiana filtra i strony listy kart. `UI/UIPanelPlayerCards.cs:79` **SFX:** `Assets/_Project/Audio/SFX/Rewards/card_filter_change.mp3`, `Assets/_Project/Audio/SFX/Rewards/card_page_change.mp3`
- [ ] **B** Dodanie, cofnięcie i zatwierdzenie punktów statystyki. `UI/UIPanelPlayerStats.cs:151` **SFX:** `Assets/_Project/Audio/SFX/Rewards/stat_point_add.mp3`, `Assets/_Project/Audio/SFX/Rewards/stat_point_undo.mp3`, `Assets/_Project/Audio/SFX/Rewards/stat_points_confirm.mp3`
- [ ] **B** Wybór projektu karty i udane stworzenie karty. `UI/UIPanelCardCrafting.cs:177; UI/UIPanelCardCrafting.cs:295` **SFX:** `Assets/_Project/Audio/SFX/Rewards/card_recipe_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/card_craft_success.mp3`
- [ ] **B** Wybór typu kafelka, ulepszenia i slotu oraz usunięcie ulepszenia. `UI/UIPanelTileCrafting.cs:221` **SFX:** `Assets/_Project/Audio/SFX/Rewards/tile_type_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/tile_upgrade_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/tile_slot_select.mp3`, `Assets/_Project/Audio/SFX/Rewards/tile_upgrade_remove.mp3`
- [ ] **B** Udane stworzenie kafelka. `UI/UIPanelTileCrafting.cs:416` **SFX:** `Assets/_Project/Audio/SFX/Rewards/tile_craft_success.mp3`
- [ ] **C** Najechanie na kartę, umiejętność lub element z opisem: bardzo subtelny sygnał UI. `UI/UIBoardActionCard.cs:106; UI/UISkillButton.cs:104` **SFX:** `Assets/_Project/Audio/SFX/Rewards/ui_hover.mp3`

## Umiejętności wymagające palety brzmień

Każda aktywacja umiejętności może korzystać ze wspólnej warstwy dla klasy oraz krótkiego charakterystycznego akcentu. Definicje znajdują się w `Assets/_Project/Configs/Skills`.

<!-- GENERATED-SKILL-SFX-START -->
| Grupa | Umiejętność / warstwa | SFX |
| --- | --- | --- |
| Podstawowe | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Basic/basic_layer.mp3` |
| Podstawowe | BasicAttackSkill | `Assets/_Project/Audio/SFX/Skills/Basic/basic_attack_skill.mp3` |
| Podstawowe | BasicHeal | `Assets/_Project/Audio/SFX/Skills/Basic/basic_heal.mp3` |
| Podstawowe | GuardUp | `Assets/_Project/Audio/SFX/Skills/Basic/guard_up.mp3` |
| Podstawowe | Ignite | `Assets/_Project/Audio/SFX/Skills/Basic/ignite.mp3` |
| Podstawowe | ShieldBash | `Assets/_Project/Audio/SFX/Skills/Basic/shield_bash.mp3` |
| Wrogowie | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_layer.mp3` |
| Wrogowie | EnemyEmber | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_ember.mp3` |
| Wrogowie | EnemyGuard | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_guard.mp3` |
| Wrogowie | EnemySlam | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_slam.mp3` |
| Wrogowie | EnemyStrike | `Assets/_Project/Audio/SFX/Skills/Enemy/enemy_strike.mp3` |
| Mag | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Mage/mage_layer.mp3` |
| Mag | ChromaticBolt | `Assets/_Project/Audio/SFX/Skills/Mage/chromatic_bolt.mp3` |
| Mag | Convergence | `Assets/_Project/Audio/SFX/Skills/Mage/convergence.mp3` |
| Mag | FireSpark | `Assets/_Project/Audio/SFX/Skills/Mage/fire_spark.mp3` |
| Mag | IceWard | `Assets/_Project/Audio/SFX/Skills/Mage/ice_ward.mp3` |
| Mag | IgniteBurst | `Assets/_Project/Audio/SFX/Skills/Mage/ignite_burst.mp3` |
| Mag | Kindle | `Assets/_Project/Audio/SFX/Skills/Mage/kindle.mp3` |
| Mag | LightningChain | `Assets/_Project/Audio/SFX/Skills/Mage/lightning_chain.mp3` |
| Mag | Renew | `Assets/_Project/Audio/SFX/Skills/Mage/renew.mp3` |
| Mag | TransmuteMana | `Assets/_Project/Audio/SFX/Skills/Mage/transmute_mana.mp3` |
| Mag | Wildfire | `Assets/_Project/Audio/SFX/Skills/Mage/wildfire.mp3` |
| Wojownik | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Warrior/warrior_layer.mp3` |
| Wojownik | BattleFrenzy | `Assets/_Project/Audio/SFX/Skills/Warrior/battle_frenzy.mp3` |
| Wojownik | Execute | `Assets/_Project/Audio/SFX/Skills/Warrior/execute.mp3` |
| Wojownik | Fortress | `Assets/_Project/Audio/SFX/Skills/Warrior/fortress.mp3` |
| Wojownik | GuardStance | `Assets/_Project/Audio/SFX/Skills/Warrior/guard_stance.mp3` |
| Wojownik | HeavySlash | `Assets/_Project/Audio/SFX/Skills/Warrior/heavy_slash.mp3` |
| Wojownik | PerfectStrike | `Assets/_Project/Audio/SFX/Skills/Warrior/perfect_strike.mp3` |
| Wojownik | Riposte | `Assets/_Project/Audio/SFX/Skills/Warrior/riposte.mp3` |
| Wojownik | ShieldSlam | `Assets/_Project/Audio/SFX/Skills/Warrior/shield_slam.mp3` |
| Wojownik | Unyielding | `Assets/_Project/Audio/SFX/Skills/Warrior/unyielding.mp3` |
| Wojownik | WideSwing | `Assets/_Project/Audio/SFX/Skills/Warrior/wide_swing.mp3` |
| Cień | Wspólna warstwa | `Assets/_Project/Audio/SFX/Skills/Shadow/shadow_layer.mp3` |
| Cień | Backstab | `Assets/_Project/Audio/SFX/Skills/Shadow/backstab.mp3` |
| Cień | BladeDance | `Assets/_Project/Audio/SFX/Skills/Shadow/blade_dance.mp3` |
| Cień | BloodPact | `Assets/_Project/Audio/SFX/Skills/Shadow/blood_pact.mp3` |
| Cień | ExposeWeakness | `Assets/_Project/Audio/SFX/Skills/Shadow/expose_weakness.mp3` |
| Cień | KillingSequence | `Assets/_Project/Audio/SFX/Skills/Shadow/killing_sequence.mp3` |
| Cień | Momentum | `Assets/_Project/Audio/SFX/Skills/Shadow/momentum.mp3` |
| Cień | QuickCut | `Assets/_Project/Audio/SFX/Skills/Shadow/quick_cut.mp3` |
| Cień | Recycle | `Assets/_Project/Audio/SFX/Skills/Shadow/recycle.mp3` |
| Cień | ShadowHarvest | `Assets/_Project/Audio/SFX/Skills/Shadow/shadow_harvest.mp3` |
| Cień | SoulBlade | `Assets/_Project/Audio/SFX/Skills/Shadow/soul_blade.mp3` |
<!-- GENERATED-SKILL-SFX-END -->

## Uwagi do wdrożenia

- **Dopasowania:** `TileDestroyed` uruchamia się osobno dla każdego kafelka. Główny SFX należy wyzwalać raz na falę przez `MatchWaveCompleted`; dźwięki pojedynczych kafelków powinny być ciche i limitowane.
- **Trafienia:** obrażenia są liczone przed końcem animacji pocisku. Dźwięk uderzenia powinien pojawić się w callbacku `onArrived` w `VFXManager`, razem z reakcją postaci.
- **Interfejs:** klik zatwierdzenia odtwarzaj po przyjęciu akcji. Nieudany wybór może mieć osobny, cichy sygnał tylko wtedy, gdy ekran pokazuje graczowi powód odmowy.
- **Karta Rewind:** logika tej karty ma `IsAvailable = false` w `Cards/BoardActionsSpecial.cs:216`. Nie wymaga jeszcze SFX w działającej pętli gry.
