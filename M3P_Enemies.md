# M3P — przeciwnicy i AI

_Wersja robocza 0.9 · 10 października 2026 · wyciąg z M3P System Progresji_

Dokument zbiera ustalenia dotyczące przeciwników, ich akcji, intencji, progresji i balansu walki. Stan wdrożenia opisuje wartości z `Assets/_Project/Configs/Skills/Enemies`; reguły wyboru akcji, progresji i nowe zestawy przeciwników są kierunkiem designu do implementacji i playtestu.

## 1. Założenia walki z przeciwnikiem

Przeciwnik tworzy czytelną zagadkę bojową. Przygotowuje zagrożenie, pokazuje je graczowi, a gracz odpowiada planszą, kartami i skillami. AI nie zmienia zapowiedzianej akcji na korzystniejszą tuż przed jej wykonaniem.

- Zwykły przeciwnik wykonuje jedną zaplanowaną akcję na turę.
- AGI nie zwiększa liczby akcji. Może wpływać na inicjatywę, częstotliwość akcji specjalnej lub inne parametry tempa.
- Multiatak pozostaje właściwością konkretnego skilla i jest wyświetlany jako jedna intencja, na przykład `2 × 4 obrażenia`.
- Przeciwnicy nie płacą kolorową maną ani AP gracza. Ograniczają ich warunki, cooldowny, wzorce i fazy.

## 2. Cykl planowania i intencji

| Moment | Zachowanie systemu | Wymaganie czytelności |
| --- | --- | --- |
| Planowanie | Na początku walki i po własnej akcji przeciwnik wybiera następną akcję. | Decyzja zapada przed turą gracza. |
| Intencja | Wybrana akcja jest przechowywana przez całą turę gracza. | UI pokazuje ikonę, cel, typ efektu i przewidywaną wartość. |
| Kontrgra | Gracz może usunąć status, zbudować Shield, uleczyć się lub przyspieszyć finisher. | Osłabienie intencji jest nagrodą za przygotowanie. |
| Wykonanie | Przeciwnik wykonuje dokładnie zapowiedzianą akcję. | AI nie podmienia osłabionej akcji. |
| Wyjątek | Akcja może zostać anulowana tylko przy braku legalnego celu, śmierci albo wymuszonej zmianie fazy. | UI pokazuje anulowanie; system nie zastępuje go ukrytym atakiem. |

## 3. Wybór akcji

Każda akcja ma warunki legalności, bazowy priorytet i modyfikatory sytuacyjne. AI odrzuca akcje nielegalne, oblicza wynik pozostałych i wybiera najwyższy. Remisy rozstrzyga deterministycznie seed walki. Każdy zestaw musi mieć bezwarunkową akcję awaryjną.

| Dane decyzji | Przykłady | Zastosowanie |
| --- | --- | --- |
| Warunki | HP poniżej progu, Burn na celu, poprzednia akcja, cooldown, numer tury, faza bossa. | Określają legalność akcji. |
| Priorytet bazowy | Atak 20, obrona 60, finisher 100. | Nadaje zestawowi domyślną hierarchię. |
| Modyfikatory | +20 za stack Burn, +100 przy zabójstwie, −50 za powtórzenie. | Reagują na stan bez rozbudowanego drzewa zachowań. |
| Pamięć | Poprzednia akcja, liczba użyć, aktywna faza, akcje jednorazowe. | Zapobiega monotonii i obsługuje sekwencje. |
| Fallback | Podstawowy atak albo słaba obrona. | Gwarantuje legalny ruch. |

## 4. Aktualne akcje przeciwników

Poniższe liczby odpowiadają obecnym assetom w `Configs/Skills/Enemies` i powiązanym efektom bojowym.

| Akcja | Asset | Cooldown | Efekt | Projektowy warunek / priorytet |
| --- | --- | --- | --- | --- |
| Cios | `EnemyStrike` | 1 | 3 obrażenia fizyczne. | Fallback. |
| Garda | `EnemyGuard` | 3 | +5 Shield przez efekt `GuardUp`. | Wysoki priorytet, gdy przeciwnik nie ma Shield. |
| Miażdżenie | `EnemySlam` | 2 | `4 + 4 × F` obrażeń fizycznych, gdzie `F` to efektywne piętro przeciwnika. | Po Gardzie albo co trzecią turę. |
| Rozniecenie | `EnemyEmber` | 1 | Nakłada 1 Burn, bez obrażeń natychmiast. | Gdy cel ma mało Burn. |

### 4.1. Burn

| Status | Czas | Tick | Źródła |
| --- | --- | --- | --- |
| Burn | 2 tury, stackowalny | 1 obrażenie za stack na turę wroga. | Tile Stack Burn, `EnemyEmber`, Ignite / Zapłon. |

## 5. Zestawy przeciwników

`level` oznacza piętro efektywne przeciwnika (z uwzględnieniem premii elity opisanej w sekcji 8.2), a `rank` rangę przeciwnika. W poniższych formułach `L = level`, `R = rank`. Skille wymienione poniżej są specyfikacją zestawów do implementacji i playtestu; wartości bez osobnego mnożnika zachowują aktualny efekt wspólnego assetu, dopóki nie zostaną nadpisane osobnym efektem. Nowe współczynniki są punktem startowym do testów balansu.

Guard oznacza efekt obronny przyznający Shield, zgodnie z obecnym `GuardUp`; nie wprowadza osobnego zasobu. Armor (pancerz) jest zasobem używanym przez `Corrosion`, `IronWall` i `ArmorBash` frenzy.

Confusion jest statusem nakładanym na gracza. Na początku jego następnej tury, po odnowieniu AP, odejmuje punkty akcji (minimum do 0), po czym znika. `Scream` RudoBata odbiera `L` AP. `KonarHit` Entona odbiera zawsze 1 AP.

### 5.1. Leszinio

| Skill | Efekt | Czas | Stan |
| --- | --- | --- | --- |
| Regeneracja | +2 HP na turę, maksymalnie 10 HP. | 5 tur. | Propozycja. |

Moment ticku, odświeżanie i stackowanie regeneracji wymagają decyzji przed implementacją.

### 5.2. Miniootaur

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `HeadButt` | Zadaje obrażenia. | `10 × level × rank`; CD 3 tury. |
| `DefensiveStance` | Nakłada na Miniootaura status redukcji obrażeń o stałą wartość, nie procent. | Redukcja `L` obrażeń, minimum do 0, przez `2 × L` tur. |
| `LickWounds` | Nakłada na Miniootaura status regeneracji. | Leczy `2 × L` HP na tick przez `3 × R` tur. |

Zestaw Miniootaura nie zawiera `EnemyGuard` ani `EnemyEmber`. Usunięcie dotyczy wyłącznie tego przeciwnika; oba skille oraz efekt `GuardUp` pozostają dostępne dla innych przeciwników.

### 5.3. SufoFajstos

| Skill | Efekt | Skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `EmberStrike` | Zadaje obrażenia i nakłada Burn na cel. | `3 × level` obrażeń i `1 × level` stacków Burn. |
| `EmberLight` | Nakłada Burn. | `2 × level` stacków Burn. |
| `EmberDetonation` | Zlicza stacki Burn na celu, zadaje obrażenia i zużywa wszystkie stacki Burn tego celu. | `5 × BurnStacks` obrażeń, według liczby stacków przed ich zużyciem. |

### 5.4. RudoBat

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `Scream` | Zadaje obrażenia i nakłada na gracza status Confusion. | `7 × L` obrażeń; status odbiera `L` AP na początku następnej tury gracza, po odnowieniu AP, po czym znika. AP nie spada poniżej 0. |
| `Rage` | Nakłada buff do ataku podstawowego. | `+3` obrażenia przez `2 × level` tur. |
| `MultiStrike` | Wykonuje serię ataków po 5 obrażeń. | `2 × level` trafień; łącznie `10 × level` obrażeń przed modyfikatorami. |

### 5.5. dexrafi

| Skill | Efekt | Skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `GuardUp` | Nakłada Shield. | Prototyp dexrafiego: `6 × L` Shield. Wspólny efekt daje aktualnie +5 Shield; wymaga osobnego skalowania dla tego zestawu. |
| `Laser` | Zadaje obrażenia i odbiera manę z każdego typu osobno. | `10 × L` obrażeń; do `3 × L` many każdego typu, minimum do 0 dla każdego zasobu. |
| `DexrafiSlam` | Zadaje obrażenia. | `15 × level` obrażeń. |

### 5.6. frenzy

| Skill | Efekt | Skalowanie |
| --- | --- | --- |
| `Corrosion` | Zabiera Armor graczowi i przekazuje faktycznie skradzioną ilość frenzy. | Do `5 × L` Armor, maksymalnie tyle, ile gracz posiada. |
| `EnemySlam` | Zadaje obrażenia. | `4 + 4 × F` obrażeń fizycznych, gdzie `F` to efektywne piętro przeciwnika; CD 2. |
| `IronWall` | Dodaje Armor, przygotowując zasób do `ArmorBash`. | `10 × L` Armor. |
| `ArmorBash` (dawniej `ShieldBash`) | Zużywa cały własny Armor i zadaje obrażenia w przeliczniku 1:1. | Obrażenia równe ilości zużytego Armor; wymaga dodatniego Armor. |

`Corrosion` i `IronWall` budują ten sam zasób, który następnie konsumuje `ArmorBash`. Nazwa `ShieldBash` w poprzednim prototypie odnosiła się do tego skilla; docelowa mechanika korzysta z Armor.

### 5.7. Enton

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny, dostępny od piętra 1. | 3 obrażenia fizyczne; CD 1. |
| `Entail` | Zakorzenia losowe kafelki. Zakorzeniony kafelek pozostaje w miejscu, dopóki nie zostanie zniszczony. | `3 × level` kafelków. |
| `KonarHit` | Zadaje obrażenia i nakłada na gracza Confusion. | `5 × L` obrażeń; CD 3 tury; status odbiera 1 AP po odnowieniu AP na początku następnej tury gracza, po czym znika. |
| `ThrowBeeHive` | Nakłada na gracza Poison, który zadaje 2 obrażenia na turę. | `2 × level × rank` tur. |

### 5.8. Minioluch

Rola: trucizna, osłabianie gracza i unikanie ataków podczas działania trucizny.

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `ToxicBite` | Zadaje obrażenia i nakłada na gracza status Weakened. | `4 × L` obrażeń; każdy atak gracza zadaje o `L` mniej obrażeń, minimum 0, przez następne `2 × R` tury gracza. |
| `PoisonGas` | Nakłada na gracza słabą truciznę (small poison). | Siła i czas Poison do ustalenia. |
| Zakopanie | Nakłada na Miniolucha status pozwalający uniknąć wszystkich obrażeń. | 1 tura gracza; CD 3 tury przeciwnika. |

### 5.9. Blobinio glut

Rola: trucizna, regeneracja i obrona utrudniająca przebicie serią słabych ataków.

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `PoisonBlob` | Zadaje obrażenia przy trafieniu i nakłada na gracza słabą truciznę. Podstawowy atak awaryjny zestawu. | Obrażenia oraz siła i czas Poison do ustalenia. |
| `Regen` | Nakłada na Bloba status regeneracji. | Ilość leczenia, czas i moment ticku do ustalenia. |
| `PoisonCloud` | Nakłada na gracza silną truciznę (strong poison). | Siła i czas Poison do ustalenia. |
| `SlimeCoat` | Daje Blobowi Shield i status Toxic Skin. Każde trafienie Bloba przez gracza nakłada na atakującego 1 stack Poison. | `6 × L` Shield; Toxic Skin trwa `2 × R` tury gracza. |

Limit aktywacji Toxic Skin na turę oraz siła i czas Poison z tego statusu wymagają decyzji przed implementacją.

### 5.10. Molok (Mol)

Rola: stopniowe wyczerpywanie many gracza i podtrzymywanie własnego życia skradzionym zasobem.

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `ManaNibble` | Zadaje obrażenia i usuwa manę jednego losowego typu, którego gracz ma dodatnią ilość. | `4 × L` obrażeń; usuwa do `2 × L` many wybranego typu. |
| `ManaFeast` | Usuwa manę jednego losowego typu z dodatnią ilością i leczy Moloka o faktycznie usuniętą ilość. | Usuwa do `3 × L` many; leczenie 1:1, do Max HP. Wymaga dostępnej many na celu. |
| `ArcaneHunger` | Nakłada na Moloka status: Enemy Strike dodatkowo usuwa manę jednego losowego typu, którego gracz ma dodatnią ilość. | Do `L` many na atak; status trwa `2 × R` tury Moloka. |

Utrata many nie obniża zasobu poniżej 0. Gdy gracz nie ma many, `ManaNibble` i `EnemyStrike` nadal zadają obrażenia, a dodatkowy efekt usuwania many nie działa. Losowanie typu many korzysta z seeda walki.

### 5.11. Klops

Rola: ciężki przeciwnik, który chroni się, wykonuje mocne uderzenie kosztem własnego HP i odzyskuje zdrowie atakiem.

| Skill | Efekt | Czas / skalowanie |
| --- | --- | --- |
| `EnemyStrike` | Standardowy atak awaryjny. | Aktualnie 3 obrażenia fizyczne; CD 1. |
| `BodySlam` | Zadaje obrażenia graczowi i płaci własnym HP. Koszt HP omija Armor i Shield Klopsa. | `12 × L` obrażeń; koszt `3 × L` HP. |
| `ThickHide` | Nakłada na Klopsa status redukcji obrażeń o stałą wartość. | Redukcja `2 × L`, minimum do 0, przez `2 × R` tury gracza. |
| `Devour` | Zadaje obrażenia i leczy Klopsa proporcjonalnie do faktycznie odebranego graczowi HP. | `6 × L` obrażeń; leczenie równe 50% utraconego przez gracza HP, do Max HP. |

## 6. Kotwice balansu

- Podstawowy przeciwnik piętra 1 — goblin, debug, Leszinio lub RudoBat — ma około **30 Max HP** (±5), przy **Constitution 3** i progu +10 HP w drzewku Kondycji.
- Bonus obrażeń basic attack z matchy liczy wyłącznie kafelki powyżej matcha 3. Supermatch i dodatkowy atak na kaskadzie pozostają bez zmian.
- Po korekcie obrażeń z matchy TTK wydłużył się. HP przeciwników i bieżący balans Odnowienia, Iskry oraz Gardy wroga są pierwszą odpowiedzią, bez cofania formuły matchy.
- `EnemyGuard` daje 5 Shield, nie 10. Współdzielony legacy skill `GuardUp` korzysta z tego samego efektu i również daje 5 Shield.
- Prototyp dexrafiego zakłada osobne skalowanie `GuardUp` do `6 × L` Shield; nie zmienia to opisu aktualnego wspólnego efektu powyżej.

## 7. Bossowie i fazy

Boss używa tego samego planera co zwykły przeciwnik, lecz każda faza może mieć osobną pulę akcji, priorytety i regułę wejścia. Zmiana fazy może wymusić akcję przejściową, wyczyścić wybrane cooldowny lub odblokować finisher.

- Gracz widzi zmianę fazy i pierwszą intencję nowego zestawu przed jej wykonaniem.
- W pierwszym prototypie wystarczają progi 60% i 30% HP.
- Boss nie wykonuje dodatkowej akcji od razu po przekroczeniu progu, chyba że wcześniej zapowiedziana mechanika definiuje przejście.

## 8. Progresja między piętrami

Jeden `EnemyDefinition` opisuje gatunek lub archetyp, nie wariant konkretnego piętra. Przy rozpoczęciu walki system łączy dane bazowe z numerem piętra i typem spotkania, tworząc niemutowalny wariant runtime ze statystykami, aktywnymi skillami, rangą nazwy i nagrodą EXP.

| Zasada | Ustalenie |
| --- | --- |
| Odblokowania | Skill ma minimalne piętro. Aktywna pula obejmuje tylko skille odblokowane na piętrze efektywnym. |
| Tempo nowych zachowań | Statystyki rosną na każdym piętrze; nowe zachowania pojawiają się zwykle co 2–3 piętra. |
| Fallback | Każdy przeciwnik ma go od piętra 1. |
| Rozmiar puli | Zwykły przeciwnik ma maksymalnie 3–4 aktywne akcje. Nowy próg może zastąpić słabszy skill. |
| Cel odblokowania | Ma dodawać obronę, status, multiatak lub przygotowanie, a nie wyłącznie zwiększać obrażenia. |
| Rangi | Nazwy takie jak Zwiadowca, Wartownik, Weteran i Herszt komunikują osiągnięty próg. |

### 8.1. Przykład goblina

| Statystyka | Wartość bazowa | Przyrost na piętro |
| --- | --- | --- |
| STR | 2 | +1,00 |
| INT | 0 | +0,34 |
| CON | 3 | +1,00 |
| AGI | 1 | +0,20 |

Wartości są punktem startowym do playtestu i korzystają ze wspólnego `StatProgressionConfig`.

### 8.2. Typ spotkania

Typ spotkania modyfikuje piętro efektywne po wybraniu `EnemyDefinition`. Numer piętra jest ograniczany do minimum 1, a elita otrzymuje premię +1: `piętro efektywne = max(1, piętro dungeona) + 1`. Premia wpływa na statystyki, progi rang i skilli, skalowane efekty oraz EXP. Elita może korzystać z tej samej definicji i wcześniejszych progów skilli. Boss powinien mieć osobną definicję, gdy posiada unikalne fazy, model lub reguły zwycięstwa.

## 9. Model techniczny

| Element | Odpowiedzialność |
| --- | --- |
| `SkillDefinition` | Wspólny koszt, cooldown i efekty dla skilli gracza oraz przeciwnika. |
| `EnemyActionDefinition` | Opakowuje skill w dane AI: intencję, priorytet, warunki i ograniczenia powtórzeń. |
| `EnemyDecisionContext` | Niemutowalny snapshot HP, Shield, statusów, numeru tury, poprzedniej akcji i fazy. |
| `EnemyActionPlanner` | Czysta klasa wybierająca akcję bez zależności od MonoBehaviour. |
| `EnemyBattleCharacter` | Przechowuje `plannedAction`, wykonuje ją w swojej turze, następnie planuje kolejną. |
| UI intencji | Korzysta z tego samego obliczenia wartości co `BattleEffect`. |
| `EnemyDefinition` | Przechowuje `BaseHardStats`, `BaseExperienceReward`, `StatGrowthPerFloor`, `SkillUnlocks` i progi rang. |
| `EnemyProgressionResolver` | Przyjmuje `EnemyDefinition`, `FloorIndex` i `MapNodeType`, zwraca `EnemyRuntimeSpec`. |
| `EnemyRuntimeSpec` | Zawiera końcowe statystyki, aktywne skille, EXP, nazwę wyświetlaną i piętro efektywne. |
| `BattleManager` | Łączy `FloorIndex` z `MapRunState` i typ spotkania z `PendingEncounter`, następnie przekazuje spec do `EnemyBattleCharacter`. |

Oryginalny ScriptableObject nie jest modyfikowany w trakcie walki. Do zapisu runu wystarczają piętro, encounter i seed, o ile balans nie zmienił się między wersjami save’a.

## 10. Zakres pierwszego prototypu i testy

- Jedna intencja oraz jedna akcja przeciwnika na turę.
- Pierwsza akcja planowana przed pierwszą turą gracza; kolejna po każdej akcji przeciwnika.
- UI intencji pokazuje nazwę, ikonę, cel i przewidywany efekt.
- Planner obsługuje warunki HP, statusu, cooldownu, poprzedniej akcji i numeru tury oraz deterministyczne remisy.
- Zestawy do wdrożenia i testów: Leszinio, Miniootaur, SufoFajstos, RudoBat, dexrafi, frenzy, Enton, Minioluch, Blobinio glut, Molok (Mol) i Klops.
- Jeden `EnemyDefinition` goblina używany na wszystkich piętrach prototypu.
- Skill unlocki testowe: `EnemyStrike` od piętra 1, `EnemyGuard` od 2, `EnemyEmber` od 3 i `EnemySlam` od 5.
- Skalowanie STR, INT, CON, AGI i EXP na podstawie `FloorIndex`, bez skalowania poziomem gracza.
- Modyfikatory zwykłej walki i elity; osobna definicja bossa jest dopuszczalna.
- Testy pięter 1, 2, 3, 5 i 8, wariantu elitarnego, deterministyczności oraz braku modyfikacji assetu.

## 11. Otwarte decyzje

- Cooldowny nowych skilli przeciwników poza ustalonym CD Zakopania Miniolucha (3 tury przeciwnika).
- Tick, odświeżanie i stackowanie regeneracji Leszinia i `LickWounds` Miniootaura; wartości, czas i tick `Regen` Bloba.
- Odświeżanie i stackowanie Confusion, Weakened, Toxic Skin, Arcane Hunger oraz statusów obronnych. Jedno rozstrzygnięcie Confusion następuje po odnowieniu AP w następnej turze gracza, a status następnie znika.
- Kolejność redukcji `DefensiveStance` i `ThickHide` względem Armor, Shield i innych modyfikatorów.
- Siła, czas, moment ticku oraz reguły stackowania słabej i silnej trucizny; obrażenia `PoisonBlob`.
- Limit aktywacji Toxic Skin na turę i jego interakcja z multiatakiem.
- Czy Zakopanie blokuje także obrażenia ze statusów oraz czy pozwala przeciwnikowi wykonywać inne akcje podczas jego trwania.
- Moment odliczania czasu `ArcaneHunger` względem tury, w której został nałożony.
- Zaokrąglanie leczenia `Devour` oraz legalność `BodySlam`, gdy koszt HP może zabić Klopsa.
- Moment planowania akcji, zakres danych w UI intencji oraz ewentualna zmiana zapowiedzi przez bossa przy zmianie fazy.
