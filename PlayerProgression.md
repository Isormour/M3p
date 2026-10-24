# PlayerProgression — progresja gracza w M3P

_Wyodrębniono 11 października 2026 z dokumentu koncepcyjnego [M3P — system progresji, wersja 0.8](M3P_System_progresji_v0.8.md), datowanego na 27 września 2026._

Dokument zbiera zasady rozwoju postaci, kolekcji i buildu oraz ekonomię nagród między walkami. Parametry zachowują status ze źródła i wymagają weryfikacji prototypem. Szczegółowe reguły kolejki kart, Resolve, AI przeciwników, katalogi efektów bojowych i generowanie grafu dungeonu pozostają w dokumencie źródłowym.

| Oznaczenie | Znaczenie |
| --- | --- |
| ZAŁOŻENIE | Element wynikający bezpośrednio z obecnej koncepcji projektu. |
| REKOMENDACJA | Proponowana wersja robocza do zweryfikowania prototypem. |
| OTWARTE | Decyzja lub wartość wymagająca testów i doprecyzowania. |

## 1. Cele i warstwy progresji

Progresja ma motywować do kolejnych walk, umożliwiać różne buildy i łączyć umiejętne tworzenie dużych matchy z trwałymi nagrodami. Wzrost mocy powinien iść w parze z odblokowywaniem nowych możliwości, aby rozwój nie sprowadzał się do zwiększania procentowych premii.

| Warstwa | Sposób rozwoju | Rola w buildzie |
| --- | --- | --- |
| Postać | EXP, poziomy, punkty STR/CON/INT/AGI i automatyczne drzewka perków. | Podnosi obrażenia bazowego ataku, HP, rękę albo maksymalną liczbę AP. |
| Kolekcja kart | Pozyskiwanie, tworzenie, wymiana, usuwanie i specjalizacja kart. | Określa dostępne manipulacje planszą. |
| Runy | Tworzenie, montowanie i wymiana run w kartach. | Zmieniają funkcję karty albo nagradzają wynik Resolve. |
| Tile deck | Kolekcjonowanie konkretnych tile’i i craftowanie ich efektów. | Dostarcza małych zasobów, statusów i premii. |
| Skille | Trwałe odblokowania, wybór loadoutu i proponowane ulepszenia lub warianty. | Pozwalają świadomie wykorzystać przygotowane zasoby. |
| Crafting | Wydawanie kolorowych shardów i odzyskiwanie części materiałów. | Specjalizuje kolekcję i łączy pozostałe warstwy. |
| Ekwipunek / relikty | Opcjonalna warstwa późniejsza. | Może zmieniać reguły całego buildu. |

Gra nie wymaga klas ani wyboru bohatera. Modele postaci mogą być kosmetyczne, ale nie mają osobnych statystyk, drzewek ani ekskluzywnych pul. Nowy profil rozpoczyna z trzema skillami opisanymi w §7; każdy zajmuje normalny slot loadoutu i może zostać później wymieniony.

## 2. Doświadczenie i poziomy

ZAŁOŻENIE: postać zdobywa EXP i awansuje na kolejne poziomy. Każdy poziom przyznaje **1 punkt statystyki**.

REKOMENDACJA: podstawowy EXP jest przyznawany za zwycięstwo. Dodatkowy EXP z run i tile’i nalicza się dopiero po zakończeniu wygranej walki; porażka lub ucieczka nie zachowuje premii. Ogranicza to farmienie jednej walki przez celowe przedłużanie starcia.

| Źródło dodatkowego EXP | Warunek | Nagroda i ograniczenie |
| --- | --- | --- |
| Runa Wiedzy | Match 4+ w Resolve z kartą wyposażoną w runę. | +1 EXP po walce; maks. 3 aktywacje na walkę. |
| Tile Doświadczenia | Tile zostaje zbity. | +1 EXP po zwycięstwie; po aktywacji zwykły tile do końca walki. |
| Tile Mędrca | Tile bierze udział w matchu 5+. | +2 EXP; rzadszy wariant. |

OTWARTE: bazowy EXP za walkę, krzywa kosztu kolejnych poziomów, level cap oraz zasady respecu.

## 3. Rozwój statystyk i drzewka perków

Katalog zawiera **8 drzewek po 10 perków**. Każdy punkt w drzewku odblokowuje kolejny perk: progi wynoszą **1, 2, 3, …, 10**, a maksymalna wartość jednego drzewka to **10 punktów**. Nie ma pustych poziomów między perkami. Odblokowane efekty zostają i sumują się z wcześniejszymi; kolejny perk nie zastępuje poprzedniego.

Baza przed perkami: obrażenia bazowego ataku **1**, maksymalne HP **20**, ręka **3**, maksymalne AP **2**. Nowy profil nie ma odblokowanych drzewek. Wybranie nowego drzewka przyznaje jego pierwszy punkt i pierwszy perk bez wydawania punktu za awans. Do pełnego rozwinięcia wybranego drzewka potrzeba następnie **9 wydanych punktów**.

| Drzewko | Asset | Kierunek rozwoju |
| --- | --- | --- |
| Siła | `PerkTree_Strength` | Obrażenia podstawowe, Power i premie za wiele dopasowań w jednej sekwencji. |
| Inteligencja | `PerkTree_Intelligence` | Arcane, początkowa ręka i premie za jedno duże dopasowanie. |
| Kondycja | `PerkTree_Constitution` | Maksymalne HP, Guard, Recovery i przetrwanie przy niskim zdrowiu. |
| Zręczność | `PerkTree_Agility` | Precision, Tempo i premie do punktów akcji. |
| Żywioły | `PerkTree_Elements` | Arcane, Recovery i efekty związane z Burn. |
| Might | `PerkTree_Might` | Power, maksymalne HP i walka przy niskim zdrowiu. |
| Tactic | `PerkTree_Tactic` | Tempo, początkowa ręka i efekty taktyczne. |
| Faith | `PerkTree_Faith` | Guard, Dusze na początku walki i efekty obronne. |

Kolejność i efekty perków określają assety drzewek. Przejście z progów co 3 punkty na progi co 1 punkt zachowuje tę kolejność i wartości efektów; ostatni perk jest dostępny przy 10 punktach zamiast 30.

Drzewka nie zawierają wyboru jednego z dwóch talentów. Katalog talentów jest pusty.

### 3.1. Statystyki jako filtr wyniku planszy

REKOMENDACJA: statystyki zmieniają względną wartość profilu Resolve. Te same karty mogą dzięki temu wspierać różne buildy zależnie od rozwoju postaci, tile decku i skilli.

| Statystyka | Preferowany profil | Naturalna synergia | Możliwe przełamanie talentem |
| --- | --- | --- | --- |
| STR | Wiele osobnych grup i kilka krótkich Resolve. | Każda grupa może wykonywać osobny fizyczny atak. | Heavy Blow premiujący największy match. |
| CON | Aktywacja defensywnych enchantów. | Skaluje HP, leczenie, Shield i pojemność obrony. | Konwersja obrony w ofensywę. |
| INT | Super match, monokolor albo spektrum. | Większa ręka i mocniejsza konwersja many/statusów przez skille. | Mag kaskad wzmacniany liczbą fal. |
| AGI | Dłuższe kolejki lub więcej sekwencji. | Więcej staminy/AP zwiększa liczbę instrukcji przed Resolve. | Premia za krótkie, precyzyjne sekwencje. |

Przykład: trzy matche po trzy tile’e mogą wykonać trzy ataki STR, ale nie tworzą shardów. Jeden match dziewięciu tile’i wykonuje jeden atak, lecz daje dużą wypłatę progową, manę i shardy. Talenty mogą odwracać tę domyślną preferencję.

## 4. Shardy i ekonomia trwałych nagród

ZAŁOŻENIE: shardy są trwałym surowcem do craftingu. Bazowo powstają wyłącznie przy matchu co najmniej czterech tile’i.

> shardy = max(0, liczba tile’i w matchu − 3)

| Rozmiar matcha | Bazowe shardy |
| --- | --- |
| 3 | 0 |
| 4 | 1 |
| 5 | 2 |
| 6 | 3 |
| 7 | 4 |

REKOMENDACJA: shardy zachowują kolor tile’i, z których powstały. Matche powstałe w kaskadach mogą generować shardy osobno, ale wymaga to testu tempa ekonomii.

| Kolor shardów | Typowe zastosowanie craftingowe |
| --- | --- |
| Czerwone | Obrażenia, przebicie, podpalenie. |
| Zielone | Leczenie, regeneracja, trucizna. |
| Niebieskie | Dobieranie, kontrola, redukcja kosztu. |
| Żółte | AP, szybkość, dodatkowe akcje. |
| Czarne | Klątwy, poświęcenie HP, wysoka nagroda za ryzyko. |

### 4.1. Premie do trwałych nagród

| Źródło | Warunek | Efekt roboczy |
| --- | --- | --- |
| Runa Obfitości | Pierwszy match 4+ w Resolve. | +1 shard. |
| Runa Kaskady | Pierwsza dodatkowa fala w Resolve. | +1 shard. |
| Runa Doskonałości | Match 5+. | Dodatkowa nagroda zależna od koloru. |
| Tile +1 shard | Tile bierze udział w matchu 4+. | +1 shard; rare, drogi crafting i limit kopii. |

Runy nagrody obserwują raport Resolve, w którego kolejce znalazła się wyposażona karta. Trwałe nagrody mają wspólne limity, aby wiele kopii tej samej runy nie tworzyło pętli; konkretne wartości wymagają doprecyzowania.

Złoto pozostaje OTWARTE jako waluta zakupów u kupca lub dodatkowy sink ekonomiczny zależny od game loopu. Mana kolorów jest zasobem tymczasowym walki; stamina/AP służy do opłacania kart i aktywnych akcji.

## 5. Kolekcja kart i run

ZAŁOŻENIE: karty są podstawowym interfejsem programowania ruchów na planszy. Rozwój kolekcji rozszerza dostępne manipulacje, podczas gdy runy specjalizują konkretne karty.

| Rodzina karty | Kierunek rozwoju |
| --- | --- |
| Swap | Poziom, pion, dowolny kierunek, skos lub większy dystans. |
| Change Color | Konkretny kolor, wybór dowolnego koloru albo zachowanie craftowanego efektu. |
| Destroy | Precyzyjne usuwanie tile’i, recykling, niszczenie blokad i efektów negatywnych. |
| Specjalne | Rotacje obszaru, zmiana grawitacji albo ryzyko za silniejszy efekt; Rewind wymaga przeprojektowania. |

REKOMENDACJA: karta ma dwa różne sloty runiczne.

| Slot | Co zmienia | Przykład |
| --- | --- | --- |
| Runa funkcjonalna | Sposób wykonania ruchu lub geometrię efektu. | Swap po skosie albo o zwiększonym zasięgu. |
| Runa nagrody | Warunkową premię za wynik Resolve. | Dobierz kartę po dwóch matchach, +1 shard po matchu 4+. |

Przykład: Horizontal Swap + Runa Przekątnej + Runa Kombinacji pozwala wykonać swap po skosie, a utworzenie co najmniej dwóch matchy dobiera kartę.

Archetypy talii obejmują kaskadę, super match, multimatch, precyzję, spektrum / sekwencję i poświęcenie. Określają sposób konstruowania wyniku, a nie klasę postaci; ten sam profil może zasilać różne zasoby i skille.

Sklep kart służy do pozyskiwania, wymiany i usuwania kart. Ulepszanie kart pozostaje w sklepie, dopóki nie stanie się osobnym, rozbudowanym systemem.

## 6. Kolekcja tile’i i rozwój tile decku

ZAŁOŻENIE: tile deck zachowuje stałą liczbę tile’i każdego koloru. Gracz konfiguruje ich efekty, ale nie może usuwać kolorów i zmieniać podstawowej matematyki match-3.

REKOMENDACJA: każdy tile jest osobnym egzemplarzem w kolekcji. Gracz może posiadać więcej tile’i danego koloru, lecz do aktywnego decku wybiera zawsze dokładnie wymaganą liczbę.

| Parametr prototypu | Wartość robocza |
| --- | --- |
| Plansza | 8×8, czyli 64 pola. |
| Kolory | Czerwony, zielony, niebieski, żółty i czarny. |
| Tile’e na kolor | 16. |
| Aktywny tile deck | 80 tile’i; około 16 poza planszą na starcie. |

Efekt jest przypisany do konkretnego egzemplarza tile’a i uruchamia się przy jego zbiciu, z dodatkowymi warunkami dla silnych premii. Jeden tile posiada jeden upgrade; wyjątki powinny zajmować większą część budżetu mocy decku.

Dostępne kierunki efektów obejmują manę koloru, leczenie, Shield, Burn, Dusze, obrażenia, wzmocnienie następnego skilla, dobieranie, staminę oraz EXP i shardy. Kuźnia tile’i umożliwia tworzenie tile’i, dodawanie lub wymianę upgrade’u i modyfikację aktywnego decku.

### 6.1. Ograniczenia rozwoju decku

REKOMENDACJA: limitować moc efektów. Dla 16 tile’i jednego koloru można przetestować skład: **8 zwykłych, 4 common, 2 uncommon, 1 rare i 1 epic**. Alternatywą jest wspólny budżet punktów efektów.

- Maksymalnie 2 kopie tego samego silnego efektu.
- Tile’e AP, EXP i Match Neighbor mają osobne limity; w prototypie maks. 2 tile’e AP i maks. 2 tile’e Match Neighbor w aktywnym decku. Limit EXP pozostaje do ustalenia.
- Tile staminy wymaga matcha 5+; maks. 1 aktywacja staminy na Resolve. Dobieranie z tile’a wymaga matcha 5+ i działa maks. raz na Resolve.
- Enchant Duszy przyznaje +1 Duszę, gdy jego tile zostanie zbity przez match albo bezpośrednio kartą; wszystkie takie enchanty mają wspólny limit 3 Dusz na Resolve.
- Tile EXP traci efekt do końca walki po pierwszej aktywacji, także po przetasowaniu stosu.
- Bonus shardów z tile’a wymaga matcha 4+ i wysokiego kosztu craftingu.
- Match Neighbor wymaga naturalnego matcha 4+; zbity sąsiad nie zwiększa rozmiaru matcha ani shardów, nie aktywuje własnego upgrade’u i nie uruchamia kolejnego Match Neighbor.

## 7. Kolekcja i odblokowywanie skilli

ZAŁOŻENIE: gracz może posiadać **maksymalnie 4 drzewka skilli**, ale rozpoczyna rozgrywkę z **jednym drzewkiem**. Na początku rozgrywki pojawia się ekran wyboru **1 z 3 oferowanych drzewek skilli**. Wybrane drzewko staje się pierwszym drzewkiem gracza. Limit drzewek jest niezależny od liczby skilli w aktywnym loadoucie.

Stan docelowy: profil przechowuje kolekcję odblokowanych skilli, a do walki gracz wybiera **maksymalnie 5**. Wyposażone i odblokowane skille są zawsze dostępne podczas walki i nie są dobierane losowo jak karty. Nowy profil odblokowuje wyłącznie trzy skille z §7.1.

Źródło opisuje katalog 30 definicji gracza, pogrupowanych jako Mag, Wojownik i Cień — po 10 skilli. Są to grupy katalogu, a nie obowiązkowe klasy postaci ani ekskluzywne pule.

### 7.1. Skille startowe

Nowy profil ma odblokowane i domyślnie wyposażone dokładnie trzy skille:

| Skill | Asset | Funkcja startowa |
| --- | --- | --- |
| Mocne Cięcie | `Player/Warrior/HeavySlash` | 8 obrażeń fizycznych; podstawowa opcja ofensywna. |
| Odnowienie | `Player/Mage/Renew` | Regeneracja: +2 HP na początku każdej tury gracza przez 4 tury; cooldown 4 tury. |
| Garda | `Player/Warrior/GuardStance` | 10 Shield; cooldown 2 tury. |

Te trzy skille zajmują 3 z 5 miejsc początkowego loadoutu. Gracz może je później zdjąć lub zastąpić innymi odblokowanymi skillami. Wszystkie pozostałe skille z docelowego katalogu rozpoczynają jako zablokowane.

Legacy/testowe definicje `BasicAttackSkill`, `BasicHeal`, `GuardUp`, `ShieldBash` i `Ignite` nie należą do puli odblokowań za bossów. Wszystkie trzy skille startowe — `HeavySlash`, `Renew` i `GuardStance` — należą do katalogu 30 skilli. Pozostaje więc **27 skilli możliwych do odblokowania za bossów**.

### 7.2. Nagroda za bossa

- Po pokonaniu bossa istniejący panel losuje **trzy różne skille** z puli nadal zablokowanych skilli i pokazuje je graczowi.
- Skille startowe, legacy/testowe definicje oraz skille już odblokowane nie mogą pojawić się w ofercie.
- Gracz wybiera jeden z trzech skilli i odblokowuje go **na stałe** w kolekcji profilu. Dwa niewybrane pozostają zablokowane i mogą wrócić do puli przy kolejnych bossach.
- Losowanie nie powinno powtarzać skilla w obrębie jednej oferty. Jeśli w puli zostały mniej niż trzy skille, panel pokazuje wszystkie pozostałe.
- Docelowo oferta może uwzględniać build: jeden skill zgodny z jego kolorami lub zasobami, jeden otwierający inny kierunek oraz jeden wybór nietypowy. Do pierwszej wersji wystarczy losowanie trzech różnych legalnych opcji.
- Po bossie gracz może od razu zmienić loadout przed następnym piętrem.
- Po odblokowaniu całego katalogu nagrodę może zastąpić wybór wariantu skilla, punkt mistrzostwa albo duża paczka shardów.

### 7.3. Rozwój i synergie

Crafting może służyć do ulepszania skilli lub odblokowywania alternatywnych wersji. Docelowa pula powinna rozszerzać silniki buildu: Fire/Burn, Shield, Combo/Focus, Spektrum, Blood/Souls oraz rękę / tempo.

Build Dusz może generować zasób na dwa sposoby: przez Żniwo Cieni za tile’e zniszczone kartami oraz przez enchant tile’a **Dusza**. Enchant daje +1 Duszę, gdy jego tile zostanie zbity przez match albo bezpośrednio kartą, maksymalnie 3 Dusze łącznie z enchantów na Resolve. Dusze pozostają zasobem tymczasowym walki i są zużywane między innymi przez Ostrze Dusz.

Aktualne drzewka nie skalują obrażeń skilli procentem ze STR ani efektów magicznych z INT. Siła podnosi obrażenia bazowego ataku z matcha, kondycja maksymalne HP, inteligencja rękę, a zręczność maksymalne AP, zgodnie z §3.

Domyślny koszt każdego skilla gracza wynosi **1 AP oraz podwojoną względem wcześniejszej wersji manę kolorów**. Konkretne koszty many, cooldowny i efekty opisuje katalog w §14.4; kosztów many już podwojonych nie należy podwajać ponownie.

## 8. Crafting

Crafting jest głównym sinkiem shardów i podstawową motywacją do wielokrotnego rozgrywania walk. Powinien specjalizować kolekcję, a nie tylko zwiększać liczby o kolejne poziomy +1/+2/+3.

Możliwe zastosowania:

- tworzenie nowych kart i wariantów kart bazowych;
- tworzenie, montowanie i wymiana run w kartach;
- tworzenie specjalnych egzemplarzy tile’i i zmiana ich efektów;
- ulepszanie skilli lub odblokowywanie alternatywnych wersji;
- odzyskiwanie części materiałów z niepotrzebnych kart, run i tile’i;
- w późniejszej wersji: tworzenie ekwipunku, reliktów i startowych konfiguracji.

OTWARTE: koszty receptur oraz wybór między tworzeniem konkretnego efektu, losowaniem właściwości i modelem mieszanym.

## 9. Pętla progresji między walkami

1. **Walka:** gracz buduje sekwencje kart, używa Resolve, tworzy matche i kaskady oraz wykorzystuje zasoby przez skille.
2. **Rozliczenie zwycięstwa:** otrzymuje bazowy EXP, bonusowy EXP i shardy z matchy 4+.
3. **Poziom:** po zebraniu wymaganej liczby EXP otrzymuje 1 punkt statystyki. Wydany punkt odblokowuje kolejne perki swojego drzewka.
4. **Crafting:** wydaje kolorowe shardy na karty, runy, tile’e lub rozwój skilli.
5. **Konfiguracja:** układa talię kart, tile deck i aktywne skille oraz rozdziela punkty statystyk.
6. **Kolejna walka:** testuje build i zdobywa materiały do następnej iteracji.

### 9.1. Wybór trasy jako decyzja o rozwoju

Graf dungeonu określa, którą warstwę gracz może rozwinąć przed następną walką i bossem.

| Węzeł | Wartość dla progresji |
| --- | --- |
| Walka | Podstawowe źródło EXP i shardów z matchy 4+. |
| Elita | Większe ryzyko; rzadszy tile, karta, większa paczka shardów albo przyszły relikt. |
| Skrzynia | Jednorazowa nagroda: shardy wybranych kolorów, EXP, karta lub materiał do ulepszeń. |
| Sklep kart | Pozyskiwanie, wymiana, usuwanie i ulepszanie kart. |
| Kuźnia tile’i | Tworzenie i modyfikacja tile’i oraz aktywnego decku. |
| Kapliczka / wydarzenie | Późniejsze zmiany skilli, loadoutu, statystyk lub wybory ryzyko–nagroda. |
| Boss | Duża wypłata i wybór jednego z trzech skilli. |

Przed pierwszym sklepem lub kuźnią musi istnieć możliwość zdobycia shardów. Trudniejsza trasa powinna mieć zauważalnie lepszą lub bardziej ukierunkowaną nagrodę. Co najmniej jedna rozsądna droga do bossa powinna prowadzić przez sklep kart albo kuźnię.

### 9.2. Porażka i trwałość nagród

- Porażka odbiera nagrody naliczone w przegranym spotkaniu i cofa gracza do poprzedniego bezpiecznego węzła, zgodnie z modelem mapy opisanym w źródle.
- Pokonana walka, otwarta skrzynia i wykorzystana jednorazowa usługa nie wypłacają nagrody ponownie.
- W pierwszym prototypie shardy i EXP mogą pozostać trwałe natychmiast po zwycięstwie.
- Model sakwy bankowanej dopiero po bossie pozostaje opcją do testu.
- Ograniczona Determinacja albo rosnące zagrożenie po porażce są opcjonalnymi zabezpieczeniami, jeżeli darmowe próby okażą się nadużywane.

Statystyki bojowe są przygotowywane od nowa na początku walki. Obozowisko z leczeniem nie jest potrzebne w obecnym modelu; może wrócić po wprowadzeniu zasobu przenoszonego między spotkaniami.

## 10. Długoterminowe odblokowania

Główne kierunki odblokowań:

- przepisy craftingowe;
- nowe karty i rodziny ruchów;
- runy funkcjonalne i runy nagrody;
- skille i ich alternatywne wersje;
- efekty specjalnych tile’i;
- startowe talie, modele kosmetyczne i początkowe skille;
- po ustaleniu game loopu: modyfikatory trudności, bossowie i zawartość dungeonu.

## 11. Balans progresji i zabezpieczenia ekonomii

| Warstwa | Typowa moc efektu | Uzasadnienie |
| --- | --- | --- |
| Craftowany tile | Około 1 jednostki. | Częsty, losowy i część dużego decku. |
| Runa karty | Około 2–4 jednostek. | Warunkowa nagroda albo zmiana funkcji. |
| Skill | Około 8–20 jednostek. | Świadomie aktywowany i opłacony zasobem. |

- Bonusowy EXP rozlicza się po zwycięstwie i ogranicza liczbę aktywacji na walkę.
- Tile EXP nie może być ponownie farmiony w tej samej walce.
- Zwroty staminy i dobieranie mają wspólne limity na Resolve; dodatkowy limit zwrotów na turę wymaga playtestu.
- Tile deck zachowuje stały skład kolorów i limit mocy efektów.
- Karty i runy nie powinny zapewniać pewnych efektów o skali skilla bez porównywalnego kosztu.
- Tempo shardów musi dawać realne decyzje craftingowe po kilku walkach, bez farmienia jednej nagrody.

## 12. Minimum pierwszego prototypu progresji

| Element | Proponowane minimum |
| --- | --- |
| Postać | STR, CON, INT, AGI; 1 punkt statystyki na poziom. |
| Ekonomia | EXP i kolorowe shardy z matchy 4+; testowa mana kolorów oraz stamina/AP w walce. |
| Karty | 20 kart: 4 Horizontal Swap, 4 Vertical Swap, 2 Free Swap, po 1 Change Color dla pięciu kolorów, 3 Destroy Tile, 1 Diagonal Swap, 1 Cycle 2×2. |
| Runy | Około 12 run reprezentujących funkcję, nagrodę, AP, rękę i EXP. |
| Tile deck | 80 tile’i: 16 każdego z pięciu kolorów; kilka efektów testowych. |
| Skille | Start: `HeavySlash`, `GuardStance` i `Renew`; pozostałe 27 skilli katalogu zablokowane. Maks. 5 w loadoucie. Po bossie panel pokazuje 3 różne zablokowane skille, z których gracz odblokowuje 1. |
| Crafting | Tworzenie lub podmiana jednej runy i jednego efektu tile’a. |
| Buildy | Co najmniej cztery profile: kaskada, super match, multimatch oraz Spektrum albo Poświęcenie. |

### 12.1. Pytania do playtestu

- Czy talia kart i tile deck są odczuwane jako dwa uzupełniające się systemy?
- Czy gracz potrafi świadomie dążyć do matchy 4+ bez nadmiernego wydłużania tury?
- Czy tempo shardów daje realną decyzję craftingową po kilku walkach?
- Czy tile’e i runy EXP tworzą ciekawy wybór między mocą bojową a szybszym rozwojem?
- Czy specjalne tile’e są zauważalne, ale nie dominujące?
- Czy statystyki zmieniają preferowany profil Resolve, zamiast tylko zwiększać końcowe liczby?
- Czy różne archetypy prowadzą do wyraźnie innych talii i decyzji?
- Czy wybór trasy pozwala świadomie rozwijać karty, tile deck albo skille?
- Czy zwroty AP, premie shardów i ponowne próby walk nie pozwalają na nadużycia ekonomii?

## 13. Otwarte decyzje dotyczące progresji

| Temat | Pytanie do rozstrzygnięcia |
| --- | --- |
| Krzywa EXP | Ile EXP daje walka, jak rośnie koszt poziomu i czy istnieje level cap? |
| Talenty | Drzewka dają automatyczne, sumujące się perki. Czy zastąpić je wyborem na progach 5/10/15? |
| Respec | Czy można resetować punkty statystyk i build; jeśli tak, jakim kosztem? |
| Shardy | Czy są wspólne dla profilu, wyprawy, czy działają w modelu mieszanym? |
| Duże matche | Jak liczyć krzyże, kształty T/L, nakładające się linie i kaskady na potrzeby shardów? |
| Tile deck | Czy limitować liczbę specjalnych tile’i, czy używać punktowego budżetu mocy? |
| Crafting | Czy gracz tworzy konkretny efekt, losuje właściwości, czy używa obu metod? Jakie są koszty? |
| Porażka | Które nagrody zostają po przegranej i jak zapobiec nadużyciom? Aneks źródła podaje roboczy model utraty nagród z przegranego spotkania. |
| Skille | Czy koszt 1 AP i podwojonej many zapewnia właściwą częstotliwość? Jak rozwijać warianty i zastąpić nagrodę bossa po ukończeniu kolekcji? |
| Runy nagrody | Jak limitować kilka identycznych run obserwujących ten sam raport Resolve? |
| Pętle staminy | Jaki wspólny limit na Resolve i turę pozwala kontynuować turę bez nieskończonych pętli? |
| Game loop | Czy trzy piętra i 6–8 warstw decyzji na piętro dają wyprawę o właściwej długości oraz właściwe tempo rozwoju? |

## 14. Szczegółowe katalogi przeniesione z dokumentu źródłowego

Poniższe katalogi zachowują kompletne wartości i reguły z wersji 0.8. Stanowią źródło szczegółów implementacyjnych dla progresji gracza.

### 14.1. Katalog kart i zestaw prototypowy

REKOMENDACJA: karty powinny przede wszystkim manipulować planszą. Efekty bojowe, leczenie i nagrody pozostają domeną run, tile’i oraz skilli. Przesuwanie całych rzędów i kolumn zostało usunięte z zakresu.

| Rodzina | Karta | Efekt roboczy |
| --- | --- | --- |
| Swap | Horizontal Swap | Zamień dwa sąsiednie tile’e w poziomie. |
| Swap | Vertical Swap | Zamień dwa sąsiednie tile’e w pionie. |
| Swap | Free Swap | Zamień dwa sąsiednie tile’e w dowolnym kierunku; wyższy koszt AP. |
| Swap | Diagonal Swap | Zamień dwa tile’e stykające się rogami. |
| Swap | Distant Swap | Zamień dwa tile’e oddalone o dwa pola. |
| Change Color | Change to Red / Green / Blue / Yellow / Black | Zmień wybrany tile na wskazany kolor. |
| Change Color | Wild Transmutation | Zmień wybrany tile na dowolny kolor; wysoki koszt AP. |
| Destroy | Destroy Tile | Oznacz jeden tile jako Cracked; usuń go po wykonaniu kolejki. |
| Destroy | Destroy Horizontal Pair | Oznacz dwa sąsiadujące tile’e w poziomie jako Cracked. |
| Destroy | Destroy Vertical Pair | Oznacz dwa sąsiadujące tile’e w pionie jako Cracked. |
| Destroy | Purge | Usuń blokadę, negatywny tile lub element umieszczony przez przeciwnika. |
| Specjalne | Cycle 2×2 | Obróć cztery tile’e w wybranym obszarze 2×2. |
| Specjalne | Triangle Rotation | Obróć trzy wskazane, sąsiadujące tile’e. |
| Specjalne | Gravity Shift | Ustaw kierunek opadania dla najbliższego Resolve planszy. |
| Specjalne | Chaotic Shuffle | Finale: przetasuj planszę według ustalonego seeda; karta musi kończyć kolejkę. |
| Specjalne | Rewind | OTWARTE: przeprojektować pod model kolejki; poza pierwszym prototypem Resolve. |
| Specjalne | Forbidden Transmutation | Zmień trzy tile’e na jeden kolor kosztem HP lub wyczerpania karty do końca walki. |

#### 14.1.1. Zestaw pierwszego prototypu

| Karta | Liczba | Rola w prototypie |
| --- | --- | --- |
| Horizontal Swap | 4 | Podstawowy, precyzyjny ruch w poziomie |
| Vertical Swap | 4 | Podstawowy, precyzyjny ruch w pionie |
| Free Swap | 2 | Elastyczny swap za większy koszt |
| Change to Red / Green / Blue / Yellow / Black | 5 × 1 | Budowanie matchy konkretnego koloru |
| Destroy Tile | 3 | Kontrolowanie układu i uruchamianie kaskad |
| Diagonal Swap | 1 | Test bardziej zaawansowanej geometrii |
| Cycle 2×2 | 1 | Test specjalnej manipulacji obszarem |
| Łącznie | 20 | 10 projektów kart, licząc pięć kolorów osobno |

#### 14.1.2. Model run na karcie
REKOMENDACJA: karta ma dwa różne sloty runiczne. Rozdzielenie funkcji od nagrody ułatwia balans i pozwala tworzyć czytelne warianty tej samej karty.

| Slot | Co zmienia | Przykład |
| --- | --- | --- |
| Runa funkcjonalna | Sposób wykonania ruchu lub geometrię efektu. | Swap po skosie albo swap o zwiększonym zasięgu |
| Runa nagrody | Warunkową premię za dobrze wykonany ruch. | Dobierz kartę po dwóch matchach, +1 shard po matchu 4+ |

> Przykładowa karta: Horizontal Swap + Runa Przekątnej + Runa Kombinacji: karta pozwala wykonać swap po skosie, a utworzenie co najmniej dwóch matchy dobiera kartę.

#### 14.1.3. Archetypy synergii kart
Archetyp karty określa sposób konstruowania wyniku, a nie klasę postaci. Ten sam profil Resolve może zasilać Shield, Fire, Combo, reagenty, Souls albo inne zasoby zależnie od tile decku i skilli.

| Archetyp | Pożądany profil Resolve | Narzędzia i naturalne synergie |
| --- | --- | --- |
| Kaskada | Duża liczba kolejnych fal po pierwszym matchu. | Gravity Shift, Cracked, przesunięcia; STR/AGI, Fire, Static i efekty za falę. |
| Super match | Jeden match 5–8+ przygotowany kilkoma kartami przed sprawdzeniem planszy. | Change Color, Distant Swap, rotacje; INT/AGI, Heavy Blow, progi shardów i many. |
| Multimatch | Kilka niezależnych grup utworzonych jednocześnie. | Swapy i rotacje obszaru; STR, Combo, Bleed albo Shield za grupę. |
| Precyzja | Dokładnie jeden kontrolowany match, bez nieplanowanej kaskady. | Precyzyjne swapy, Purge i podgląd; STR/AGI, Focus, crit i Execute. |
| Spektrum / sekwencja | Kilka kolorów w jednym Resolve albo określona kolejność kolorów w turze. | Wild Transmutation i elastyczne ruchy; INT, reagenty i receptury skilli. |
| Poświęcenie | Świadome usuwanie wartościowych tile’i kartami, niezależnie od naturalnego matcha. | Destroy i efekty kosztujące HP; CON/INT, Blood, Corruption i Souls. |

### 14.2. Pełny katalog run kart

Runy funkcjonalne zmieniają komendę swojej karty, a runy nagrody obserwują raport Resolve, w którego kolejce znalazła się karta. Kilka kart może więc współpracować na jeden wynik. Zwroty staminy, dobieranie i trwałe nagrody mają wspólne limity na Resolve lub turę, aby wiele kopii tej samej runy nie tworzyło pętli.

#### 14.2.1. Runy nagradzające jakość ruchu

| Runa | Warunek | Efekt roboczy |
| --- | --- | --- |
| Obfitości | Pierwszy match 4+ w Resolve | +1 shard |
| Doskonałości | Match 5+ | Dodatkowa nagroda zależna od koloru |
| Kombinacji | Co najmniej dwa matche | Dobierz kartę |
| Kaskady | Pierwsza dodatkowa fala w Resolve | +1 shard |
| Precyzji | Dokładnie jeden match 4+ | Odzyskaj 1 staminę; wspólny limit na Resolve |
| Wiedzy | Match 4+ | +1 EXP po walce; limit aktywacji |

#### 14.2.2. Runy bojowe i defensywne

| Runa | Warunek | Efekt roboczy |
| --- | --- | --- |
| Uderzenia | Match 4+ | Zadaj 2 obrażenia |
| Krwawienia | Czerwony match | Nałóż 1 Krwawienie |
| Osłabienia | Niebieski lub czarny match | Osłab następny atak przeciwnika |
| Przebicia | Match 5+ | Następny skill ignoruje część pancerza |
| Egzekucji | Wróg ma mniej niż 25% HP | Podwój ofensywny efekt runy |
| Regeneracji | Zielony match 4+ | Ulecz 2 HP |
| Ochrony | Match 4+ dowolnego koloru | +2 tarczy |
| Oczyszczenia | Match 5+ | Usuń jeden negatywny status |
| Wytrwałości | Resolve nie tworzy matcha | Otrzymaj niewielką tarczę |
| Odwetu | Match 4+ | Przygotuj kontratak na następne uderzenie |

#### 14.2.3. Runy ręki i AP

| Runa | Warunek | Efekt roboczy |
| --- | --- | --- |
| Impetu | Co najmniej dwa matche | Odzyskaj 1 staminę; wspólny limit na Resolve |
| Wnikliwości | Match 4+ | Dobierz kartę; maks. raz na Resolve |
| Powrotu | Match 5+ | Po Resolve ta karta wraca do draw pile zamiast na discard |
| Odnowienia | Resolve nie tworzy matcha | Odrzuć losową kartę i dobierz nową |
| Przygotowania | Po dodaniu karty do kolejki | Następna karta tego samego typu przed Resolve kosztuje o 1 mniej |
| Echa | Match 6+ | Utwórz tymczasową kopię karty kosztującą 1 AP |

#### 14.2.4. Runy funkcjonalne

| Runa | Karta | Zmiana funkcji |
| --- | --- | --- |
| Dalekiego Swapu | Swap | Może zamieniać tile’e oddalone o dwa pola |
| Przekątnej | Swap | Pozwala wykonać swap po skosie |
| Rozszczepienia | Destroy | Niszczy także jeden losowy sąsiedni tile |
| Recyklingu | Destroy | Zniszczony tile trafia na spód draw pile zamiast na discard |
| Transmutacji | Change Color | Zmienia też jeden losowy sąsiedni tile |
| Zachowania | Change Color | Zmieniany tile zachowuje craftowany efekt |

### 14.3. Szczegółowe reguły tile decku

ZAŁOŻENIE: tile deck nie zmienia liczby kolorów. Gracz konfiguruje efekty na stałej liczbie tile’i każdego koloru, więc nie może usunąć koloru i zepsuć podstawowej matematyki match-3.
REKOMENDACJA: każdy tile jest osobnym egzemplarzem w kolekcji. Gracz może posiadać więcej tile’i danego koloru, ale do aktywnego decku wybiera zawsze dokładnie wymaganą liczbę.

#### 14.3.1. Rozmiar decku dla planszy 8×8

| Parametr | Wartość robocza | Uzasadnienie |
| --- | --- | --- |
| Liczba kolorów | 5 | Aktualny zestaw: czerwony, zielony, niebieski, żółty i czarny |
| Pola na planszy | 64 | Plansza 8×8 |
| Tile’e na kolor | 16 | Zapewnia równą reprezentację i zapas poza planszą |
| Łącznie w decku | 80 | 64 na planszy, około 16 w draw pile na starcie |
Techniczne minimum przy pięciu kolorach to 13 tile’i na kolor (65 łącznie), ale pozostawia ono tylko jeden tile poza planszą. Dlatego 16 na kolor, czyli 80 łącznie, jest bezpieczniejszym punktem startowym.

#### 14.3.2. Praca stosu tile’i
- Tile’e są losowane bez zwracania z draw pile.
- Zbite tile’e trafiają do discardu.
- Po opróżnieniu draw pile discard jest tasowany i staje się nowym stosem dobierania.
- Przy braku legalnego ruchu plansza jest automatycznie przetasowywana.
- Generator planszy powinien kontrolować startowe matche zgodnie z założeniem trybu gry.

#### 14.3.3. Efekty craftowanych tile’i
Efekt tile’a jest mały i uruchamia się przy jego zbiciu, z dodatkowymi warunkami dla najmocniejszych premii. Efekt jest przypisany do konkretnego egzemplarza tile’a, a nie do całego koloru. Jeden tile posiada jeden upgrade; wyjątki powinny zajmować większą część budżetu mocy decku.

| Efekt tile’a | Warunek / zabezpieczenie | Rola i balans |
| --- | --- | --- |
| Mana: +1 mana koloru | Po zbiciu; kolor many odpowiada kolorowi tile’a | Common. Stabilnie zasila skille, ale nie przedłuża bezpośrednio tury. |
| HP: ulecz 1 HP | Po zbiciu; bez nadleczenia ponad maksymalne HP | Common. Małe podtrzymanie, słabsze od aktywnego skilla leczącego. |
| Stamina: +1 | Tile bierze udział w matchu 5+; maks. 1 aktywacja staminy na Resolve | Rare. Najwyższe ryzyko nieskończonych tur; maks. 2 takie tile’e w aktywnym decku. |
| Shield: +1 tarczy | Po zbiciu; używa standardowego limitu i czasu trwania tarczy | Common. Zapobiega obrażeniom, lecz nie może tworzyć trwałej obrony między walkami. |
| Stack Burn: +1 Burn | Po zbiciu; maks. 3 stacki Fire/Burn z tile’i na Resolve | Common/Uncommon. Buduje payoff dla Ignite; sam stack nie powinien natychmiast zadawać pełnych obrażeń. |
| Dusza: +1 Dusza | Tile zbity przez match albo bezpośrednio kartą; wspólny limit 3 Dusz z enchantów na Resolve | Uncommon. Buduje zasób dla Ostrza Dusz niezależnie od Żniwa Cieni. Zbicie przez Match Neighbor nie aktywuje enchantu. |
| Match Neighbor | Tile musi wejść do naturalnego matcha 4+; zbija 1 losowy ortogonalnie sąsiedni tile | Rare. Sąsiad nie zwiększa rozmiaru matcha ani shardów, nie aktywuje własnego upgrade’u i nie uruchamia kolejnego Match Neighbor. |
| +1 obrażenie | Po zbiciu | Common. Prosty efekt ofensywny i punkt odniesienia dla mocy tile’a. |
| +1 shard | Tile bierze udział w matchu 4+ | Rare. Drogi crafting, limit kopii i osobny monitoring tempa ekonomii. |
| +1 EXP | Raz na walkę; naliczane dopiero po zwycięstwie | Po aktywacji tile jest zwykły do końca walki. |
| Wzmocnij skill o 1 | Po zbiciu; dotyczy następnego skilla tego samego koloru | Uncommon. Tile przygotowuje finisher, lecz nie zastępuje skilla. |
| Dobranie karty | Match 5+; maks. raz na Resolve | Rare. Gwarantowany trigger na matchu 3 byłby zbyt stabilny. |

#### 14.3.4. Ograniczenia mocy tile decku
REKOMENDACJA: ograniczenie powinno dotyczyć mocy efektów, nie liczby kolorów. Dla prototypu można przetestować profil 16 tile’i jednego koloru: 8 zwykłych, 4 common, 2 uncommon, 1 rare i 1 epic. Alternatywą jest wspólny budżet punktów efektów.
- Maksymalnie 2 kopie tego samego silnego efektu.
- Tile’e AP, EXP, Duszy i Match Neighbor mają osobne limity w aktywnym decku. Dla prototypu: maks. 2 tile’e AP i maks. 2 tile’e Match Neighbor; limit liczby tile’i Duszy pozostaje do ustalenia po teście wspólnego limitu aktywacji.
- Efekt uruchamia się raz na konkretny zbity tile. Stamina i dobieranie mają wspólny limit na Resolve, enchanty Duszy dają łącznie maks. 3 Dusze na Resolve, a Match Neighbor nigdy nie aktywuje efektu zbitego sąsiada ani kolejnego Match Neighbor.
- Specjalny tile EXP ma stan „zużyty w tej walce”, aby nie dało się go farmić po przetasowaniu stosu.
- Warto rozważyć budżet: wiele małych efektów albo kilka drogich, ale nie oba warianty naraz.

### 14.4. Koszty, loadout i katalog zaimplementowanych skilli

Skille gracza są elementami aktywnego loadoutu, a nie kartami dobieranymi losowo. Do loadoutu można wybrać wyłącznie skille odblokowane w kolekcji profilu, maksymalnie pięć. Użycie skilla następuje wyłącznie między sekwencjami, po pełnym zakończeniu Resolve, i samo w sobie nie kończy tury.

| Element | Reguła pierwszego prototypu | Uzasadnienie |
| --- | --- | --- |
| Koszt podstawowy | 1 AP oraz dwukrotność wskazanej many kolorów. | Skill konkuruje o tempo tury i wymaga wcześniejszego przygotowania planszy. |
| Cooldown | Brak dla zwykłych wypłat; Zależny od skilla; Odnowienie: 4 tury, Garda gracza: 2 tury, Ognista Iskra: 0. | Mana ogranicza większość skilli; cooldown zabezpiecza efekty, które mogłyby blokować walkę. |
| Loadout | Maksymalnie 5 wyposażonych skilli. | Gracz buduje spójny zestaw zamiast mieć zawsze dostęp do całej kolekcji. |
| Skalowanie | STR: obrażenia bazowego ataku. CON: maksymalne HP. INT: rozmiar ręki. AGI: maksymalne AP. Progi i sumowanie opisuje §3. | Perk jest przypisany do statystyki, a nie do klasy skilla. |
| Atak bez many | `BasicAttackSkill` pozostaje definicją legacy/test i nie jest odblokowany na starcie ani oferowany przez bossów. | Darmowy atak za samo AP omija przygotowanie Match-3 i osłabia rolę kart. |

#### 14.4.1. Rdzeń testów balansu (nie zestaw startowy)
Poniższy zestaw służy do testowania Fire/Burn, Bank Shield, leczenia w czasie i ekonomii ręki. Nie określa początkowych odblokowań profilu; te definiuje §7.1. Wartości odpowiadają polom `_description` i kosztom w ScriptableObjectach (stan gry, nie wyłącznie koncepcja).

| Skill (asset) | Koszt | Efekt | Rola |
| --- | --- | --- | --- |
| Ognista Iskra (FireSpark) | 1 AP + 6 czerwonych | 2 obrażenia magiczne + status Iskra: 2 obrażenia/tura przez 4 tury (osobny od Burn). | Podstawowa wypłata ognia bez wymagania stacków Burn. |
| Zapłon (IgniteBurst) | 1 AP + 4 czerwone + 2 czarne | Zużywa wszystkie stacki Burn; 4 obrażenia magiczne za każdy stack. | Finisher na Podpalenie z tile’i / Rozniecenia / wroga. |
| Garda (GuardStance) | 1 AP + 6 niebieskich | 10 Shield. Cooldown: 2 tury. | Bank Shield gracza (Wojownik). |
| Uderzenie Tarczą (ShieldSlam) | 1 AP + 4 niebieskie + 2 żółte | Zużywa cały Shield; obrażenia równe wartości Shield, maks. 20. | Ofensywna wypłata Bank Shield. |
| Odnowienie (Renew) | 1 AP + 6 zielonych | Status regeneracji 4 tury: +2 HP na początku każdej tury gracza. Cooldown: 4 tury. | Podtrzymanie rozłożone w czasie, nie burst heal. |
| Impet (Momentum) | 1 AP + 4 żółte | Dobiera 1 kartę; przy ≥2 grupach w ostatnim Resolve dobiera drugą. Raz na turę. | Multimatch → kolejna sekwencja (Cień). |

ZAŁOŻENIE: każdy skill gracza kosztuje domyślnie 1 AP oraz dwukrotność many kolorów (patrz §14.4). Źródło prawdy dla projektu docelowego stanowi ten dokument; implementacja pozostaje do późniejszej synchronizacji.

#### 14.4.2. Kierunki pełnego katalogu
Docelowa pula około 30 skilli powinna rozszerzać silniki zamiast mnożyć warianty tych samych obrażeń. Każdy nowy skill powinien co najmniej generować zasób, konsumować przygotowany stan albo zmieniać wartość konkretnego profilu Resolve.

| Rodzina | Przykładowe skille | Profil Resolve / build |
| --- | --- | --- |
| Fire / Burn | Rozniecenie, Feniks, Pożoga | Czerwone matche, kaskady i kumulowanie statusu. |
| Shield | Mur, Odbicie, Twierdza | Multimatch, CON i świadome bankowanie obrony. |
| Combo / Focus | Flurry, Egzekucja, Perfekcyjny Cios | Wiele grup albo dokładnie jeden precyzyjny match. |
| Spektrum | Widmowy Rytuał, Pryzmat, Alchemia | Kilka kolorów w jednym Resolve lub określona kolejność kolorów. |
| Blood / Souls | Krwawy Pakt, Żniwo Dusz, Ostatnia Ofiara | Destroy, utrata HP i wysoka nagroda za ryzyko. |
| Ręka / tempo | Recykling, Echo, Przyspieszenie | Długie kolejki, kilka Resolve i kontrola discardu. |

#### 14.4.3. Katalog zaimplementowanych skilli (stan gry)
Pełny katalog gracza: 10 Mag, 10 Wojownik, 10 Cień — łącznie 30 definicji w `Configs/Skills/Player`. Poza katalogiem znajdują się legacy/testowe `BasicAttackSkill`, `BasicHeal`, `GuardUp`, `ShieldBash` i `Ignite`; nie należą one do puli nagród. Każdy wpis w kolumnie kosztu zawiera manę po podwojeniu i wymaga dodatkowo 1 AP. W loadoucie mieści się maksymalnie 5 odblokowanych skilli (§7 i §14.4).

| Mag — skill | Koszt many + 1 AP | CD / limit | Efekt (skrót) |
| --- | --- | --- | --- |
| Ognista Iskra | 6 czerwonych | 0 | 2 dmg magiczne + Iskra: 2 dmg/turę przez 4 tury |
| Rozniecenie | 4 czerwone | raz/turę | +2 Burn, bez dmg natychmiast |
| Zapłon | 4 czerwone + 2 czarne | — | detonacja Burn, 4 dmg/stack |
| Pożoga | 6 czerwonych + 2 czarne | 2 tury | 6 dmg + jeden tick Burn bez zdjęcia statusu |
| Chromatyczny Pocisk | 2 many z 3 różnych kolorów | — | 3 dmg/kolor z ostatniego Resolve, maks. 15 |
| Łańcuch Błyskawic | 4 żółte + 2 czarne | — | 4 dmg + 3 za falę kaskady po 1., maks. 16 |
| Lodowa Osłona | 4 niebieskie + 2 czarne | 2 tury | 8 Shield, następny atak wroga −3 dmg |
| Konwergencja | po 2 z pięciu kolorów | 4 tury | 15 dmg, 6 heal, 6 Shield; +50% przy ≥4 kolorach w Resolve |
| Odnowienie | 6 zielonych | 4 tury | +2 HP na początku każdej swojej tury przez 4 tury |
| Transmutacja Many | 4 różnych kolorów | raz/turę | zamiana do 3 many między dwoma kolorami |

| Wojownik — skill | Koszt many + 1 AP | CD / limit | Efekt (skrót) |
| --- | --- | --- | --- |
| Garda | 6 niebieskich | 2 tury | 10 Shield |
| Twierdza | 4 niebieskie + 2 żółte | 2 tury | 4 Shield + 3/grupa ostatniego Resolve, maks. 16 |
| Uderzenie Tarczą | 4 niebieskie + 2 żółte | — | dmg = Shield (maks. 20) |
| Mocne Cięcie | 6 czerwonych | — | 8 dmg fizycznych |
| Szeroki Zamach | 4 czerwone + 4 żółte | — | 1 trafienie/grupa ostatniego Resolve, 3 dmg, maks. 5 |
| Szał Bitewny | 4 czerwone + 2 żółte | — | 4 dmg + 3 za wcześniejszy Resolve w turze, maks. 13 |
| Perfekcyjny Cios | 4 czerwone + 2 czarne | — | 6 dmg; +8 gdy dokładnie 1 match bez kaskady |
| Egzekucja | 6 czerwonych + 2 czarne | 2 tury | 8 dmg; 16 gdy wróg <25% HP |
| Riposta | 4 niebieskie + 2 czarne | 2 tury | następny atak wroga −4 dmg, kontra 6 |
| Nieugięty | 6 żółtych + 2 niebieskie + 2 czarne | 4 tury | 12 Shield, +50% nast. skill fiz.; <30% HP → +1 AP |

| Cień — skill | Koszt many + 1 AP | CD / limit | Efekt (skrót) |
| --- | --- | --- | --- |
| Impet | 4 żółte | raz/turę | dobieranie 1–2 kart (multimatch) |
| Recykling | 4 zielone | 1 tura | wybrana karta z discardu na rękę |
| Szybkie Cięcie | 4 czerwone + 2 czarne | — | 2×4 dmg |
| Cios w Plecy | 4 czerwone + 2 czarne | — | 9 dmg; 15 gdy wróg zapowiada ofensywę |
| Taniec Ostrzy | 4 czerwone + 4 czarne | — | 2 dmg/karta w ostatniej kolejce, maks. 16 |
| Zabójcza Sekwencja | 6 czerwonych + 4 czarne | 3 tury | 4 dmg/karta w kolejce, maks. 20; bonus +5 przy 1 match bez kaskady |
| Odsłonięcie Słabości | 4 żółte + 2 czarne | — | następne 3 trafienia +3 dmg |
| Krwawy Pakt | 4 zielone | raz/turę | −5 HP, +2 AP (nie zabija) |
| Żniwo Cieni | 4 zielone | — | 1 Dusza/tile zniszczony kartą w Resolve, maks. 6 |
| Ostrze Dusz | 4 czerwone + 2 czarne | — | do 4 trafień ×4 dmg (zużywa Dusze) |

| Wrogowie — skill (asset) | CD | Efekt |
| --- | --- | --- |
| Cios (EnemyStrike) | 1 | 3 obrażenia fizyczne |
| Garda (EnemyGuard) | 3 | 5 Shield (efekt GuardUp) |
| Miażdżenie (EnemySlam) | 2 | 4 obrażenia fizyczne |
| Rozniecenie (EnemyEmber) | 1 | nakłada 1 Burn (bez dmg natychmiast) |

| Status | Czas | Tick | Uwagi |
| --- | --- | --- | --- |
| Podpalenie (Burn) | 2 tury, stackowalne | 1 obrażenie/stack na turę wroga | Kindle, tile Stack Burn, EnemyEmber, Zapłon |
| Iskra (FireSparkEmber) | 4 tury | 2 obrażenia magiczne/tura | tylko Ognista Iskra; osobny status, nie stackuje się z Burn |
| Odnowienie (RenewRegen) | 4 tury | +2 HP/tura gracza | skill Odnowienie |

#### 14.4.4. Zmiany balansu i powiązania assetów
Odnowienie (Renew) zastępuje natychmiastowe +10 HP z BasicHeal statusem Odnowienie (RenewRegen). Status trwa 4 tury i leczy +2 HP na początku każdej swojej tury, łącznie do 8 HP. Cooldown wzrasta z 3 do 4 tur; koszt docelowy wynosi 1 AP + 6 zielonych. Nowe assety: RenewRegen, RenewHealTick i RenewRegenApply.
Ognista Iskra (FireSpark) zadaje 2 obrażenia magiczne zamiast 6 i nakłada Iskrę (FireSparkEmber): 2 obrażenia na turę przez 4 tury. Iskra jest osobnym statusem i nie stackuje się z globalnym Burn. Łączny bazowy efekt przy wszystkich tickach: 2 + 8 = 10 obrażeń. Cooldown 0 pozostaje bez zmian, a koszt docelowy wynosi 1 AP + 6 czerwonych. Nowe assety: FireSparkEmber i FireSparkDotTick; FireSparkBurn wskazuje na Iskrę zamiast Burn.
Garda wroga (EnemyGuard) zachowuje cooldown 3, a jej efekt GuardUp daje 5 Shield zamiast 10. Ten sam efekt wykorzystuje legacy skill GuardUp (gracz/test), więc również daje 5 Shield. Garda gracza (GuardStance) korzysta z osobnego efektu GuardStanceShield i zachowuje 10 Shield oraz cooldown 2; jej koszt docelowy to 1 AP + 6 niebieskich.
Efekty pozostałych 27 skilli gracza pozostają bez zmian. Koszty many wszystkich 30 skilli gracza pozostają podwojone względem wcześniejszej wersji, natomiast koszt AP każdego skilla wynosi 1. Nowe propozycje z §9.8 nie zastępują istniejących skilli gracza i nie są oznaczone jako wdrożone.

Źródło pierwotne: §§2–9, 11–16 oraz Aneks A dokumentu [M3P_System_progresji_v0.8.md](M3P_System_progresji_v0.8.md). Po rozdzieleniu dokumentów źródłowych zasady progresji gracza są utrzymywane tutaj.


