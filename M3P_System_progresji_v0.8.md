# M3P — system progresji

Postać, kolejka kart, Resolve, runy, tile deck, skille, AI i crafting

_Wersja robocza 0.8 · 27 września 2026 · dokument koncepcyjny_

## 1. Cel i status dokumentu
Dokument porządkuje dotychczasowe ustalenia dotyczące progresji i walki w M3P. Wersja 0.8 zachowuje model kolejki kart, Resolve oraz generowanego dungeonu z wersji 0.4–0.5, a w §9.6 utrzymuje katalog skilli zgodny z bieżącymi assetami w Unity. AI przeciwnika pozostaje oparte na planowanej, widocznej intencji.

> Rdzeń projektu: gracz układa z kart deterministyczną sekwencję manipulacji, wykonuje ją przez Resolve, a dopiero potem plansza sprawdza matche i kaskady. Karty określają sposób budowania wyniku, statystyki premiują wybrany profil wyniku, tile deck nadaje mu małe konsekwencje, a skille pozwalają świadomie skonsumować zgromadzone zasoby lub statusy.

| Oznaczenie | Znaczenie |
| --- | --- |
| ZAŁOŻENIE | Element wynikający bezpośrednio z obecnej koncepcji projektu. |
| REKOMENDACJA | Proponowana wersja robocza, którą należy zweryfikować prototypem. |
| OTWARTE | Decyzja lub wartość wymagająca testów i późniejszego doprecyzowania. |
ZMIANY 0.5: dodano reguły kosztów, loadoutu i cooldownów skilli, sześć skilli pierwszego prototypu oraz kierunki pełnego katalogu. AI przeciwnika planuje jedną akcję, pokazuje ją jako intencję podczas tury gracza i wykonuje bez ukrytej zmiany; akcje wybiera deterministycznie na podstawie warunków i priorytetów.
ZMIANY 0.7: zsynchronizowano opis skilli z assetami w `Assets/_Project/Configs/Skills` (katalog ~30 skilli gracza + 4 wrogie). Uaktualniono Odnowienie, Ognistą Iskrę, Gardę wroga, statusy Iskra/Odnowienie oraz kotwice balansu TTK (HP podstawowego wroga, dmg z matchy). Szczegóły w §9.4–9.6 i §10.4.
ZMIANY 0.8: wszystkie koszty skilli gracza podwojono do 2 AP i dwukrotności many. Dodano pełny opis ostatniego balansu, propozycję Rage oraz zestawy Leszinia i Minotaura; Guard Up i Ember usunięto wyłącznie z zestawu Minotaura.

## 2. Cele systemu progresji
- Dać graczowi wyraźną motywację do rozgrywania kolejnych walk i powrotu do gry.
- Pozwolić budować różne buildy bez sytuacji, w której karty, tile’e i skille wykonują tę samą funkcję.
- Powiązać umiejętne tworzenie dużych matchy z trwałą progresją poprzez shardy.
- Zapewnić kilka warstw rozwoju: poziom postaci, kolekcję kart, runy, tile deck, skille i crafting.
- Utrzymać kontrolę nad ekonomią AP, EXP i shardów, aby nie powstawały nieskończone tury ani farmienie jednej walki.
- Zostawić miejsce na późniejszy roguelite’owy flow dungeonu, ale nie uzależniać od niego obecnego prototypu.
Zapewnić satysfakcję z budowania talii przez składanie kilku kart w jedną czytelną sekwencję, zamiast traktować każdą kartę jako odizolowany efekt.
Uczynić moment Resolve decyzją taktyczną: gracz wybiera między krótką sekwencją i szybką wypłatą a większą inwestycją w super match, multimatch albo kaskadę.

## 3. Architektura systemów

| System | Główna rola | Typowa skala efektu |
| --- | --- | --- |
| Karty | Tworzą kolejkę deterministycznych komend manipulujących planszą. | Koszt staminy/AP; efekt bazowy + runy |
| Resolve | Wykonuje kolejkę kart, a następnie matche, kaskady i wypłaty. | Jedna lub kilka sekwencji w turze |
| Tile deck | Nadaje małe, losowo pojawiające się premie po zbiciu tile’a. | Około 1 jednostki efektu |
| Skille | Dają silne i kontrolowane efekty bojowe lub taktyczne. | Około 8–20 jednostek |
| Statystyki | Skalują postać i wyznaczają limity ręki, AP, HP i siły efektów. | Mały bonus co punkt + progi |
| Crafting | Tworzy i specjalizuje elementy kolekcji. | Długoterminowy rozwój buildu |
| Ekwipunek / relikty | Opcjonalnie zmienia reguły całego buildu. | Warstwa późniejsza |

> Zasada rozdziału odpowiedzialności: karty konstruują profil Resolve, statystyki określają, jaki profil jest opłacalny, tile’e dostarczają małych zasobów lub statusów, a skille są silną i świadomą konwersją. Paladyn wykorzystujący Shield Bash i postać detonująca Fire są przykładami tego schematu, a nie zamkniętą listą klas.

## 4. Zasoby i ekonomia

### 4.1. Doświadczenie
ZAŁOŻENIE: postać zdobywa doświadczenie i awansuje na kolejne poziomy. Każdy poziom przyznaje 1 punkt statystyki.
REKOMENDACJA: podstawowy EXP jest przyznawany za zwycięstwo, a dodatkowy EXP z run i tile’i nalicza się dopiero po zakończeniu wygranej walki. Porażka lub ucieczka nie zachowuje premii. Chroni to system przed celowym przedłużaniem pojedynczego starcia.

| Źródło | Warunek | Efekt |
| --- | --- | --- |
| Runa Wiedzy | Karta tworzy match 4+ | +1 EXP po walce; maks. 3 aktywacje na walkę |
| Tile Doświadczenia | Tile zostaje zbity | +1 EXP; po aktywacji jest zwykłym tile’em do końca walki |
| Tile Mędrca | Tile bierze udział w matchu 5+ | +2 EXP; rzadszy wariant |
OTWARTE: krzywa EXP, level cap oraz zasady respecu.

### 4.2. Shardy
ZAŁOŻENIE: shardy są trwałym surowcem do craftingu. Powstają wyłącznie przy matchu co najmniej czterech tile’i.

> Wzór: shardy = max(0, liczba tile’i w matchu − 3)

| Rozmiar matcha | Bazowe shardy |
| --- | --- |
| 3 | 0 |
| 4 | 1 |
| 5 | 2 |
| 6 | 3 |
| 7 | 4 |
REKOMENDACJA: shardy zachowują kolor tile’i, z których powstały. Każdy kolor ma własną tożsamość craftingową. Matche powstałe w kaskadach mogą generować shardy osobno, ale wymaga to testu tempa ekonomii.

### 4.3. Zasoby bojowe

| Zasób | Rola | Status |
| --- | --- | --- |
| Stamina / AP | Koszt dodawania kart do kolejki i wykonywania aktywnych akcji. Stamina zdobyta po Resolve może zasilić kolejną sekwencję w tej samej turze. | ZAŁOŻENIE v0.3 |
| Mana koloru | Tymczasowy zasób generowany przez matche i wydawany na skille. | REKOMENDACJA |
| HP / tarcza / statusy | Stan walki modyfikowany przez skille, runy i craftowane tile’e. | ZAŁOŻENIE / rozwój |
| Złoto | Zakupy u kupca lub dodatkowy sink ekonomiczny. | OTWARTE; zależne od game loopu |
Termin roboczy: w warstwie gracza zasób może być nazywany Staminą, natomiast AP pozostaje nazwą techniczną. Resolve kończy tylko bieżącą sekwencję; End Turn kończy turę gracza.

## 5. Rozwój postaci
Postać posiada cztery główne statystyki. Jeden punkt statystyki jest przyznawany za każdy poziom. INT i AGI nie powinny zwiększać ręki ani AP przy każdym punkcie — te wartości są zbyt silne i powinny rosnąć progowo.

| Statystyka | Rola | Efekt roboczy za punkt | Próg roboczy |
| --- | --- | --- | --- |
| STR | Obrażenia fizyczne i ofensywne skille | +5% obrażeń fizycznych | Co 5 pkt: talent lub bonus ofensywny |
| CON | Maksymalne HP, tarcza i leczenie | +5 HP | Co 5 pkt: premia do tarczy / leczenia |
| INT | Efekty magiczne i zarządzanie ręką | +5% siły efektów magicznych | Co 5 pkt: +1 karta na ręce |
| AGI | Tempo, długość kolejki i ekonomia akcji | Mały bonus do inicjatywy / pierwszej sekwencji | Co 5 pkt: +1 maks. AP |
REKOMENDACJA: przy progach 5/10/15 punktów gracz może wybierać jeden z dwóch talentów. Statystyka daje wtedy nie tylko procent, ale prowadzi do konkretnego celu buildu.

### 5.1. Statystyki jako filtr wyniku planszy
REKOMENDACJA: statystyka nie powinna być wyłącznie liniowym bonusem. Powinna zmieniać względną wartość profilu Resolve, dzięki czemu te same karty wspierają różne buildy zależnie od rozwoju postaci, tile decku i skilli.

| Statystyka | Preferowany profil | Naturalna synergia | Możliwe przełamanie talentem |
| --- | --- | --- | --- |
| STR | Wiele osobnych grup i kilka krótkich Resolve | Każda grupa może wykonywać osobny fizyczny atak. | Heavy Blow premiujący największy match |
| CON | Aktywacja defensywnych enchantów | Skaluje HP, leczenie, Shield i pojemność obrony. | Konwersja obrony w ofensywę |
| INT | Super match, monokolor albo spektrum | Większa ręka i mocniejsza konwersja many/statusów przez skille. | Mag kaskad wzmacniany liczbą fal |
| AGI | Dłuższe kolejki lub więcej sekwencji | Więcej staminy/AP zwiększa liczbę instrukcji przed Resolve. | Premia za krótkie, precyzyjne sekwencje |
Przykład napięcia: trzy matche po trzy tile’e mogą wykonać trzy ataki STR, ale nie tworzą shardów; jeden match dziewięciu tile’i wykonuje jeden atak, lecz daje dużą wypłatę progową, manę i shardy. Talenty mogą odwracać tę domyślną preferencję.

## 6. Talia kart
ZAŁOŻENIE: karty są podstawowym interfejsem programowania ruchów na planszy. Zagranie karty wydaje staminę/AP, zapisuje komendę wraz z celami i umieszcza kartę w kolejce. Plansza rzeczywista zmienia się dopiero po użyciu Resolve.

| Rodzina karty | Efekt bazowy | Możliwy rozwój |
| --- | --- | --- |
| Swap | Zamiana dwóch tile’i według geometrii określonej przez kartę. | Poziom, pion, dowolny kierunek, skos lub większy dystans |
| Change Color | Zmiana wybranego tile’a na konkretny kolor. | Wybór dowolnego koloru albo zachowanie craftowanego efektu |
| Destroy | Usunięcie jednego tile’a lub precyzyjnie wskazanej pary. | Recykling tile’a, niszczenie blokad i efektów negatywnych |
| Specjalne | Rzadsza manipulacja geometrią albo stanem planszy. | Obrót 2×2, zmiana grawitacji, rewind lub ryzyko za silniejszy efekt |

### 6.1. Sekwencja kart, Resolve i End Turn
ZAŁOŻENIE: jedna tura gracza może zawierać kilka sekwencji. Resolve kończy bieżącą sekwencję, ale nie kończy tury. Po wypłacie enchantów, many, statusów i staminy gracz ponownie otrzymuje kontrolę, może rozpocząć nową kolejkę, użyć skilla albo wybrać End Turn.

| Faza | Zachowanie |
| --- | --- |
| Planowanie | Gracz dodaje karty do kolejki, wybiera cele na planszy przewidywanej i od razu płaci koszt staminy/AP. |
| Resolve kart | Komendy kart wykonują się na planszy rzeczywistej dokładnie w kolejności zagrania. |
| Resolve planszy | Po ostatniej karcie następują zniszczenia bezpośrednie, opadanie i uzupełnienie, sprawdzenie matchy oraz pełne kaskady. |
| Wypłata | Uruchamiają się runy, enchanty, obrażenia, mana, statusy, shardy i zwroty staminy. |
| Kontynuacja | Jeśli gracz ma zasób i legalną akcję, rozpoczyna kolejną sekwencję lub używa skilla. |
| End Turn | Kończy całą turę niezależnie od liczby wykonanych Resolve. |
Decyzja taktyczna: wczesny Resolve zachowuje więcej kart na ręce i może odzyskać staminę na kolejną sekwencję; późny Resolve inwestuje więcej kart w super match, multimatch albo zaprogramowaną kaskadę.

### 6.2. Plansza rzeczywista i przewidywana

| Element | Rola | Wymaganie UX / techniczne |
| --- | --- | --- |
| Plansza rzeczywista | Stan autorytatywny walki; nie zmienia się podczas dodawania kart do kolejki. | Animuje komendy dopiero po Resolve. |
| Plansza przewidywana | Deterministyczna symulacja wszystkich zakolejkowanych kart. | Kolejne cele są walidowane względem tego stanu. |
| Kolejka | Karty, cele, wybory koloru/kierunku i parametry losowe w kolejności wykonania. | Każda karta ma numer kroku i czytelne oznaczenia celów. |
| Podgląd wyniku | Przewidywane grupy matchy, największy match i inne pewne informacje. | Nie powinien obiecywać wyniku losowych refillów i kaskad. |
REKOMENDACJA: w fazie planowania tile’e nie wykonują pełnych animacji ruchu. UI pokazuje półprzezroczysty stan przewidywany, strzałki i numery kroków; właściwe animacje kart następują dopiero po Resolve.

### 6.3. Undo przed Resolve
ZAŁOŻENIE: przed Resolve gracz może cofnąć ostatnią kartę w kolejce. Karta wraca na rękę, jej koszt staminy/AP zostaje zwrócony, cele i wybory są usuwane, a plansza przewidywana jest przeliczana od początku pozostałej kolejki.
Undo można powtarzać krok po kroku aż do pustej kolejki. Nie wolno dowolnie wyjmować ani przestawiać kart ze środka, ponieważ unieważniałoby to cele późniejszych komend. Po naciśnięciu Resolve cofnięcie jest niedostępne; błędy po wykonaniu są częścią konsekwencji decyzji.

### 6.4. Kolejność wykonania i karty specjalne

| Reguła | Wersja robocza |
| --- | --- |
| Cele | Każda karta zapisuje cele na planszy przewidywanej; Resolve odtwarza te same komendy na planszy rzeczywistej. |
| Destroy | W kroku karty nakłada Cracked. Oznaczenie może przemieszczać się z tile’em; faktyczne usunięcie następuje po wszystkich kartach, przed opadaniem i sprawdzeniem matchy. |
| Gravity Shift | Ustawia kierunek opadania używany po zakończeniu kolejki i podczas następujących kaskad zgodnie z opisem karty. |
| Chaotic Shuffle | Karta losowa otrzymuje słowo kluczowe Finale i musi być ostatnia albo ustala seed już przy dodaniu, aby podgląd pozostał deterministyczny. |
| Rewind | Wymaga przeprojektowania w modelu kolejki; nie wchodzi do pierwszego prototypu Resolve. |
| End Turn z kolejką | REKOMENDACJA: wykonuje Resolve & End Turn. Stamina uzyskana z tej wypłaty nie jest już wydawana; UI komunikuje konsekwencję. |

### 6.5. Katalog kart
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

### 6.6. Zestaw pierwszego prototypu

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

### 6.7. Model run na karcie
REKOMENDACJA: karta ma dwa różne sloty runiczne. Rozdzielenie funkcji od nagrody ułatwia balans i pozwala tworzyć czytelne warianty tej samej karty.

| Slot | Co zmienia | Przykład |
| --- | --- | --- |
| Runa funkcjonalna | Sposób wykonania ruchu lub geometrię efektu. | Swap po skosie albo swap o zwiększonym zasięgu |
| Runa nagrody | Warunkową premię za dobrze wykonany ruch. | Dobierz kartę po dwóch matchach, +1 shard po matchu 4+ |

> Przykładowa karta: Horizontal Swap + Runa Przekątnej + Runa Kombinacji: karta pozwala wykonać swap po skosie, a utworzenie co najmniej dwóch matchy dobiera kartę.

### 6.8. Archetypy synergii kart
Archetyp karty określa sposób konstruowania wyniku, a nie klasę postaci. Ten sam profil Resolve może zasilać Shield, Fire, Combo, reagenty, Souls albo inne zasoby zależnie od tile decku i skilli.

| Archetyp | Pożądany profil Resolve | Narzędzia i naturalne synergie |
| --- | --- | --- |
| Kaskada | Duża liczba kolejnych fal po pierwszym matchu. | Gravity Shift, Cracked, przesunięcia; STR/AGI, Fire, Static i efekty za falę. |
| Super match | Jeden match 5–8+ przygotowany kilkoma kartami przed sprawdzeniem planszy. | Change Color, Distant Swap, rotacje; INT/AGI, Heavy Blow, progi shardów i many. |
| Multimatch | Kilka niezależnych grup utworzonych jednocześnie. | Swapy i rotacje obszaru; STR, Combo, Bleed albo Shield za grupę. |
| Precyzja | Dokładnie jeden kontrolowany match, bez nieplanowanej kaskady. | Precyzyjne swapy, Purge i podgląd; STR/AGI, Focus, crit i Execute. |
| Spektrum / sekwencja | Kilka kolorów w jednym Resolve albo określona kolejność kolorów w turze. | Wild Transmutation i elastyczne ruchy; INT, reagenty i receptury skilli. |
| Poświęcenie | Świadome usuwanie wartościowych tile’i kartami, niezależnie od naturalnego matcha. | Destroy i efekty kosztujące HP; CON/INT, Blood, Corruption i Souls. |

### 6.9. Raport Resolve i zakresy stanu

| Zakres | Zapamiętywane dane | Przykładowi odbiorcy |
| --- | --- | --- |
| Sekwencja / Resolve | Karty w kolejce, liczba grup, największy match, głębokość kaskady, kolory, zniszczenia kartami i aktywowane enchanty. | Runy kart, obrażenia bazowe, nagrody za jakość ruchu. |
| Tura | Liczba Resolve, łączna liczba matchy, kolejność kolorów, odzyskana stamina i zużyte limity. | Talenty STR/AGI, Spektrum, Momentum i limity anty-loop. |
| Walka | Statusy, mana, Shield, Fire, EXP oczekujący, zużyte tile’e specjalne i limity nagród. | Skille, tile deck, ekonomia oraz rozliczenie zwycięstwa. |

## 7. Katalog run kart
Runy funkcjonalne zmieniają komendę swojej karty, a runy nagrody obserwują raport Resolve, w którego kolejce znalazła się karta. Kilka kart może więc współpracować na jeden wynik. Zwroty staminy, dobieranie i trwałe nagrody mają wspólne limity na Resolve lub turę, aby wiele kopii tej samej runy nie tworzyło pętli.

### 7.1. Runy nagradzające jakość ruchu

| Runa | Warunek | Efekt roboczy |
| --- | --- | --- |
| Obfitości | Pierwszy match 4+ w Resolve | +1 shard |
| Doskonałości | Match 5+ | Dodatkowa nagroda zależna od koloru |
| Kombinacji | Co najmniej dwa matche | Dobierz kartę |
| Kaskady | Pierwsza dodatkowa fala w Resolve | +1 shard |
| Precyzji | Dokładnie jeden match 4+ | Odzyskaj 1 staminę; wspólny limit na Resolve |
| Wiedzy | Match 4+ | +1 EXP po walce; limit aktywacji |

### 7.2. Runy bojowe i defensywne

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

### 7.3. Runy ręki i AP

| Runa | Warunek | Efekt roboczy |
| --- | --- | --- |
| Impetu | Co najmniej dwa matche | Odzyskaj 1 staminę; wspólny limit na Resolve |
| Wnikliwości | Match 4+ | Dobierz kartę; maks. raz na Resolve |
| Powrotu | Match 5+ | Po Resolve ta karta wraca do draw pile zamiast na discard |
| Odnowienia | Resolve nie tworzy matcha | Odrzuć losową kartę i dobierz nową |
| Przygotowania | Po dodaniu karty do kolejki | Następna karta tego samego typu przed Resolve kosztuje o 1 mniej |
| Echa | Match 6+ | Utwórz tymczasową kopię karty kosztującą 1 AP |

### 7.4. Runy funkcjonalne

| Runa | Karta | Zmiana funkcji |
| --- | --- | --- |
| Dalekiego Swapu | Swap | Może zamieniać tile’e oddalone o dwa pola |
| Przekątnej | Swap | Pozwala wykonać swap po skosie |
| Rozszczepienia | Destroy | Niszczy także jeden losowy sąsiedni tile |
| Recyklingu | Destroy | Zniszczony tile trafia na spód draw pile zamiast na discard |
| Transmutacji | Change Color | Zmienia też jeden losowy sąsiedni tile |
| Zachowania | Change Color | Zmieniany tile zachowuje craftowany efekt |

## 8. Tile deck
ZAŁOŻENIE: tile deck nie zmienia liczby kolorów. Gracz konfiguruje efekty na stałej liczbie tile’i każdego koloru, więc nie może usunąć koloru i zepsuć podstawowej matematyki match-3.
REKOMENDACJA: każdy tile jest osobnym egzemplarzem w kolekcji. Gracz może posiadać więcej tile’i danego koloru, ale do aktywnego decku wybiera zawsze dokładnie wymaganą liczbę.

### 8.1. Rozmiar decku dla planszy 8×8

| Parametr | Wartość robocza | Uzasadnienie |
| --- | --- | --- |
| Liczba kolorów | 5 | Aktualny zestaw: czerwony, zielony, niebieski, żółty i czarny |
| Pola na planszy | 64 | Plansza 8×8 |
| Tile’e na kolor | 16 | Zapewnia równą reprezentację i zapas poza planszą |
| Łącznie w decku | 80 | 64 na planszy, około 16 w draw pile na starcie |
Techniczne minimum przy pięciu kolorach to 13 tile’i na kolor (65 łącznie), ale pozostawia ono tylko jeden tile poza planszą. Dlatego 16 na kolor, czyli 80 łącznie, jest bezpieczniejszym punktem startowym.

### 8.2. Praca stosu tile’i
- Tile’e są losowane bez zwracania z draw pile.
- Zbite tile’e trafiają do discardu.
- Po opróżnieniu draw pile discard jest tasowany i staje się nowym stosem dobierania.
- Przy braku legalnego ruchu plansza jest automatycznie przetasowywana.
- Generator planszy powinien kontrolować startowe matche zgodnie z założeniem trybu gry.

### 8.3. Efekty craftowanych tile’i
Efekt tile’a jest mały i uruchamia się przy jego zbiciu, z dodatkowymi warunkami dla najmocniejszych premii. Efekt jest przypisany do konkretnego egzemplarza tile’a, a nie do całego koloru. Jeden tile posiada jeden upgrade; wyjątki powinny zajmować większą część budżetu mocy decku.

| Efekt tile’a | Warunek / zabezpieczenie | Rola i balans |
| --- | --- | --- |
| Mana: +1 mana koloru | Po zbiciu; kolor many odpowiada kolorowi tile’a | Common. Stabilnie zasila skille, ale nie przedłuża bezpośrednio tury. |
| HP: ulecz 1 HP | Po zbiciu; bez nadleczenia ponad maksymalne HP | Common. Małe podtrzymanie, słabsze od aktywnego skilla leczącego. |
| Stamina: +1 | Tile bierze udział w matchu 5+; maks. 1 aktywacja staminy na Resolve | Rare. Najwyższe ryzyko nieskończonych tur; maks. 2 takie tile’e w aktywnym decku. |
| Shield: +1 tarczy | Po zbiciu; używa standardowego limitu i czasu trwania tarczy | Common. Zapobiega obrażeniom, lecz nie może tworzyć trwałej obrony między walkami. |
| Stack Burn: +1 Burn | Po zbiciu; maks. 3 stacki Fire/Burn z tile’i na Resolve | Common/Uncommon. Buduje payoff dla Ignite; sam stack nie powinien natychmiast zadawać pełnych obrażeń. |
| Match Neighbor | Tile musi wejść do naturalnego matcha 4+; zbija 1 losowy ortogonalnie sąsiedni tile | Rare. Sąsiad nie zwiększa rozmiaru matcha ani shardów, nie aktywuje własnego upgrade’u i nie uruchamia kolejnego Match Neighbor. |
| +1 obrażenie | Po zbiciu | Common. Prosty efekt ofensywny i punkt odniesienia dla mocy tile’a. |
| +1 shard | Tile bierze udział w matchu 4+ | Rare. Drogi crafting, limit kopii i osobny monitoring tempa ekonomii. |
| +1 EXP | Raz na walkę; naliczane dopiero po zwycięstwie | Po aktywacji tile jest zwykły do końca walki. |
| Wzmocnij skill o 1 | Po zbiciu; dotyczy następnego skilla tego samego koloru | Uncommon. Tile przygotowuje finisher, lecz nie zastępuje skilla. |
| Dobranie karty | Match 5+; maks. raz na Resolve | Rare. Gwarantowany trigger na matchu 3 byłby zbyt stabilny. |

### 8.4. Ograniczenia mocy tile decku
REKOMENDACJA: ograniczenie powinno dotyczyć mocy efektów, nie liczby kolorów. Dla prototypu można przetestować profil 16 tile’i jednego koloru: 8 zwykłych, 4 common, 2 uncommon, 1 rare i 1 epic. Alternatywą jest wspólny budżet punktów efektów.
- Maksymalnie 2 kopie tego samego silnego efektu.
- Tile’e AP, EXP i Match Neighbor mają osobne limity w aktywnym decku. Dla prototypu: maks. 2 tile’e AP i maks. 2 tile’e Match Neighbor.
- Efekt uruchamia się raz na konkretny zbity tile. Stamina i dobieranie mają wspólny limit na Resolve, a Match Neighbor nigdy nie aktywuje efektu zbitego sąsiada ani kolejnego Match Neighbor.
- Specjalny tile EXP ma stan „zużyty w tej walce”, aby nie dało się go farmić po przetasowaniu stosu.
- Warto rozważyć budżet: wiele małych efektów albo kilka drogich, ale nie oba warianty naraz.

## 9. Skille
Skille pozostają potrzebne, jeśli zapewniają mocny i kontrolowany efekt. Tile daje losową, małą premię, runa nagradza profil Resolve, natomiast skill działa wtedy, kiedy gracz świadomie konsumuje zgromadzony zasób, status albo przygotowaną sytuację.

| Kategoria skilla | Przykłady |
| --- | --- |
| Ofensywa | Bezpośrednie obrażenia, przebicie, atak obszarowy, detonacja statusów |
| Defensywa | Leczenie, tarcza, oczyszczenie, regeneracja |
| Kontrola | Buffy, debuffy, osłabienie przeciwnika, zmiana reguł tury |
| Ręka | Wymiana kart, dobieranie, odzyskiwanie kart z discardu |
| Plansza — wyjątek | Silna manipulacja połączona z kosztem, np. Meteor niszczący obszar 3×3 |

> Przykład synergii: czerwony tile Stack Burn nakłada 1 Burn (Podpalenie: 1 obrażenie/tura za stack, 2 tury, stackowalne), karta pozwala zbudować czerwony match, a skill Zapłon zużywa wszystkie stacki Burn i zadaje 4 obrażenia magiczne za każdy stack. Ognista Iskra używa osobnego statusu Iskra (2 obrażenia/tura przez 4 tury), nie globalnego Burn.

### 9.1. Okno skilli między sekwencjami
Po pełnej wypłacie Resolve gracz odzyskuje kontrolę. Może natychmiast użyć skilla wykorzystującego wygenerowaną manę, Shield, Fire albo inny zasób, a następnie — jeśli posiada staminę i karty — rozpocząć kolejną sekwencję. Użycie skilla nie musi automatycznie kończyć tury.

### 9.2. Przykładowe silniki tile–skill

| Silnik | Generowanie | Kontrolowana wypłata |
| --- | --- | --- |
| Bank Shield | Enchanty tile’i i Garda gracza (GuardStance) zwiększają Shield. | Shield Bash zadaje obrażenia równe Shield i zeruje zasób. |
| Fire / Burn | Enchanty trafionych tile’i nakładają stacki Fire/Burn. | Explode lub Ignite konsumuje stacki i zadaje obrażenia. |
| Combo / Focus | Osobne grupy, precyzyjny match albo kolejne Resolve budują zasób. | Flurry lub Execute konsumuje zasób na serię albo finisher. |
| Reagenty | Matche różnych kolorów tworzą zestaw składników. | Skill wybiera recepturę zależną od kombinacji kolorów. |
| Souls / Blood | Destroy i poświęcenie HP generują zasób ryzyka. | Skill zużywa zasób ofensywnie, defensywnie albo do przywołania. |
| Growth / Marks | Efekty tymczasowo pozostają na planszy i są grupowane kartami. | Harvest lub podobny skill konsumuje przygotowane oznaczenia. |
Powyższe silniki są modułami, nie zamkniętymi klasami. Przykładowo build Shield może korzystać z multimatchy albo super matcha, a build Fire może specjalizować się w kaskadach, precyzji lub monokolorze.
REKOMENDACJA: w pierwszym prototypie każdy skill gracza kosztuje 2 AP oraz dwukrotność wskazanej many kolorów. Mana i AP są podstawowym ograniczeniem częstotliwości, a cooldown pozostaje dodatkowym zabezpieczeniem dla leczenia, kontroli i efektów o wyjątkowo wysokiej mocy. Wartości liczbowe pozostają robocze.

### 9.3. Koszt, dostępność i loadout
Skille gracza są zawsze dostępnymi elementami aktywnego loadoutu, a nie kartami dobieranymi losowo. Profil przechowuje kolekcję odblokowanych skilli, lecz do walki można wyposażyć maksymalnie pięć. Użycie skilla następuje wyłącznie między sekwencjami, po pełnym zakończeniu Resolve, i samo w sobie nie kończy tury.

| Element | Reguła pierwszego prototypu | Uzasadnienie |
| --- | --- | --- |
| Koszt podstawowy | 2 AP oraz dwukrotność wskazanej many kolorów. | Skill konkuruje o tempo tury i wymaga wcześniejszego przygotowania planszy. |
| Cooldown | Brak dla zwykłych wypłat; Zależny od skilla; Odnowienie: 4 tury, Garda gracza: 2 tury, Ognista Iskra: 0. | Mana ogranicza większość skilli; cooldown zabezpiecza efekty, które mogłyby blokować walkę. |
| Loadout | Maksymalnie 5 wyposażonych skilli. | Gracz buduje spójny zestaw zamiast mieć zawsze dostęp do całej kolekcji. |
| Skalowanie | Obrażenia fizyczne skalują się ze STR, efekty magiczne z INT, a leczenie i Shield mogą otrzymać powiązania z CON. | Statystyki powinny wzmacniać różne archetypy bez zamykania skilli w klasach. |
| Atak bez many | Nie jest standardowym skillem gracza; może pozostać akcją przeciwnika. | Darmowy atak za samo AP omija przygotowanie Match-3 i osłabia rolę kart. |

### 9.4. Rdzeń pierwszego playtestu (zgodny z buildem)
Zestaw startowy nadal testuje Fire/Burn, Bank Shield, leczenie w czasie i ekonomię ręki. Poniższe wartości odpowiadają polom `_description` i kosztom w ScriptableObjectach (stan gry, nie wyłącznie koncepcja).

| Skill (asset) | Koszt | Efekt | Rola |
| --- | --- | --- | --- |
| Ognista Iskra (FireSpark) | 2 AP + 6 czerwonych | 2 obrażenia magiczne + status Iskra: 2 obrażenia/tura przez 4 tury (osobny od Burn). | Podstawowa wypłata ognia bez wymagania stacków Burn. |
| Zapłon (IgniteBurst) | 2 AP + 4 czerwone + 2 czarne | Zużywa wszystkie stacki Burn; 4 obrażenia magiczne za każdy stack. | Finisher na Podpalenie z tile’i / Rozniecenia / wroga. |
| Garda (GuardStance) | 2 AP + 6 niebieskich | 10 Shield. Cooldown: 2 tury. | Bank Shield gracza (Wojownik). |
| Uderzenie Tarczą (ShieldSlam) | 2 AP + 4 niebieskie + 2 żółte | Zużywa cały Shield; obrażenia równe wartości Shield, maks. 20. | Ofensywna wypłata Bank Shield. |
| Odnowienie (Renew) | 2 AP + 6 zielonych | Status regeneracji 4 tury: +2 HP na początku każdej tury gracza. Cooldown: 4 tury. | Podtrzymanie rozłożone w czasie, nie burst heal. |
| Impet (Momentum) | 2 AP + 4 żółte | Dobiera 1 kartę; przy ≥2 grupach w ostatnim Resolve dobiera drugą. Raz na turę. | Multimatch → kolejna sekwencja (Cień). |

ZAŁOŻENIE: każdy skill gracza kosztuje domyślnie 2 AP oraz dwukrotność many kolorów (patrz §9.3). Źródło prawdy: `Assets/_Project/Configs/Skills/Player/**` oraz `SkillConfig.asset`.

### 9.5. Kierunki pełnego katalogu
Docelowa pula około 30 skilli powinna rozszerzać silniki zamiast mnożyć warianty tych samych obrażeń. Każdy nowy skill powinien co najmniej generować zasób, konsumować przygotowany stan albo zmieniać wartość konkretnego profilu Resolve.

| Rodzina | Przykładowe skille | Profil Resolve / build |
| --- | --- | --- |
| Fire / Burn | Rozniecenie, Feniks, Pożoga | Czerwone matche, kaskady i kumulowanie statusu. |
| Shield | Mur, Odbicie, Twierdza | Multimatch, CON i świadome bankowanie obrony. |
| Combo / Focus | Flurry, Egzekucja, Perfekcyjny Cios | Wiele grup albo dokładnie jeden precyzyjny match. |
| Spektrum | Widmowy Rytuał, Pryzmat, Alchemia | Kilka kolorów w jednym Resolve lub określona kolejność kolorów. |
| Blood / Souls | Krwawy Pakt, Żniwo Dusz, Ostatnia Ofiara | Destroy, utrata HP i wysoka nagroda za ryzyko. |
| Ręka / tempo | Recykling, Echo, Przyspieszenie | Długie kolejki, kilka Resolve i kontrola discardu. |

### 9.6. Katalog zaimplementowanych skilli (stan gry)
Pełny katalog gracza: 10 Mag, 10 Wojownik, 10 Cień — łącznie 30 definicji w `Configs/Skills/Player` (plus legacy/test: BasicAttackSkill, BasicHeal, GuardUp, ShieldBash, Ignite oraz 4 skille wrogów w `Configs/Skills/Enemies`). Każdy wpis w kolumnie kosztu zawiera manę po podwojeniu i wymaga dodatkowo 2 AP. W loadoucie maks. 5 skilli (§9.3).

| Mag — skill | Koszt many + 2 AP | CD / limit | Efekt (skrót) |
| --- | --- | --- | --- |
| Ognista Iskra | 6 czerwonych | 0 | 2 dmg magiczne + Iskra: 2 dmg/turę przez 4 tury |
| Rozniecenie | 4 czerwone | raz/turę | +2 Burn, bez dmg natychmiast |
| Zapłon | 4 czerwone + 2 czarne | — | detonacja Burn, 4 dmg/stack |
| Pożoga | 6 czerwonych + 2 czarne | 2 tury | 6 dmg + jeden tick Burn bez zdjęcia statusu |
| Chromatyczny Pocisk | 6 różnych kolorów | — | 3 dmg/kolor z ostatniego Resolve, maks. 15 |
| Łańcuch Błyskawic | 4 żółte + 2 czarne | — | 4 dmg + 3 za falę kaskady po 1., maks. 16 |
| Lodowa Osłona | 4 niebieskie + 2 czarne | 2 tury | 8 Shield, następny atak wroga −3 dmg |
| Konwergencja | po 2 z pięciu kolorów | 4 tury | 15 dmg, 6 heal, 6 Shield; +50% przy ≥4 kolorach w Resolve |
| Odnowienie | 6 zielonych | 4 tury | +2 HP na początku każdej swojej tury przez 4 tury |
| Transmutacja Many | 4 dowolne | raz/turę | zamiana do 3 many między dwoma kolorami |

| Wojownik — skill | Koszt many + 2 AP | CD / limit | Efekt (skrót) |
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

| Cień — skill | Koszt many + 2 AP | CD / limit | Efekt (skrót) |
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

### 9.7. Zmiany balansu i powiązania assetów
Odnowienie (Renew) zastępuje natychmiastowe +10 HP z BasicHeal statusem Odnowienie (RenewRegen). Status trwa 4 tury i leczy +2 HP na początku każdej swojej tury, łącznie do 8 HP. Cooldown wzrasta z 3 do 4 tur; po podwojeniu koszt wynosi 2 AP + 6 zielonych. Nowe assety: RenewRegen, RenewHealTick i RenewRegenApply.
Ognista Iskra (FireSpark) zadaje 2 obrażenia magiczne zamiast 6 i nakłada Iskrę (FireSparkEmber): 2 obrażenia na turę przez 4 tury. Iskra jest osobnym statusem i nie stackuje się z globalnym Burn. Łączny bazowy efekt przy wszystkich tickach: 2 + 8 = 10 obrażeń. Cooldown 0 pozostaje bez zmian, a koszt po podwojeniu wynosi 2 AP + 6 czerwonych. Nowe assety: FireSparkEmber i FireSparkDotTick; FireSparkBurn wskazuje na Iskrę zamiast Burn.
Garda wroga (EnemyGuard) zachowuje cooldown 3, a jej efekt GuardUp daje 5 Shield zamiast 10. Ten sam efekt wykorzystuje legacy skill GuardUp (gracz/test), więc również daje 5 Shield. Garda gracza (GuardStance) korzysta z osobnego efektu GuardStanceShield i zachowuje 10 Shield oraz cooldown 2; jej koszt po podwojeniu to 2 AP + 6 niebieskich.
Efekty pozostałych 27 skilli gracza pozostają bez zmian. Koszty wszystkich 30 skilli gracza zostały podwojone. Nowe propozycje z §9.8 nie zastępują istniejących skilli gracza i nie są oznaczone jako wdrożone.

### 9.8. Propozycje nowych skilli i zestawów przeciwników
Poniższe skille są propozycjami designu. Parametry, których nie podano, pozostają do ustalenia.

| Użytkownik | Skill | Efekt | Czas / skalowanie |
| --- | --- | --- | --- |
| Do przypisania | Rage | Nakłada status Rage: +X obrażeń do ataku podstawowego. | Y tur; X i Y do ustalenia. |
| Leszinio (leszninio) | Regeneracja | Status regeneracji: +2 HP na turę, łącznie do 10 HP. | 5 tur. |
| Minotaur | Defensywna postawa | Status redukujący otrzymywane obrażenia o 3 × rangę. | 5 tur; ranga skaluje wartość redukcji. |
| Minotaur | Headbutt | Zadaje 15 × poziom Minotaura obrażeń. | Jednorazowy cios; typ obrażeń do ustalenia. |

Z docelowego zestawu Minotaura usuwamy Guard Up (EnemyGuard) oraz Ember (EnemyEmber). Usunięcie dotyczy wyłącznie Minotaura. Skille pozostają dostępne innym przeciwnikom; nie usuwamy efektu GuardUp, legacy skilla GuardUp ani statusu FireSparkEmber używanego przez gracza.
OTWARTE: wartości X i Y oraz użytkownicy Rage; koszty i cooldowny nowych skilli; moment ticku regeneracji Leszinia; odświeżanie i stackowanie nowych statusów; kolejność naliczania redukcji względem Shield i innych modyfikatorów. Dla Minotaura przyjmujemy 3 × rangę redukcji przez stałe 5 tur; sposób wyznaczania rangi i poziomu wymaga doprecyzowania.

## 10. Przeciwnicy i zachowania AI
AI nie powinno próbować ukrycie kontrować każdej decyzji gracza. Jego zadaniem jest tworzenie czytelnej zagadki bojowej: przeciwnik przygotowuje zagrożenie, komunikuje je, a gracz wykorzystuje planszę, karty i skille do odpowiedzi. Skille przeciwnika korzystają z tego samego systemu efektów co skille gracza, lecz ich wybór i ekonomia są asymetryczne.

### 10.1. Cykl plan — intencja — wykonanie

| Moment | Zachowanie systemu | Wymaganie czytelności |
| --- | --- | --- |
| Planowanie | Na początku walki i po zakończeniu własnej akcji przeciwnik wybiera następną akcję. | Decyzja powstaje przed turą gracza, aby można było na nią odpowiedzieć. |
| Intencja | Wybrana akcja jest przechowywana i widoczna przez całą turę gracza. | UI pokazuje ikonę, cel, typ efektu i przewidywaną wartość. |
| Kontrgra | Gracz może usunąć status, zbudować Shield, uleczyć się albo przyspieszyć finisher. | Skuteczne osłabienie intencji jest nagrodą za przygotowanie. |
| Wykonanie | Przeciwnik wykonuje dokładnie zapowiedzianą akcję, bez ponownego wyboru tuż przed użyciem. | AI nie podmienia osłabionej akcji na korzystniejszą. |
| Wyjątek | Akcja może zostać anulowana tylko przy braku legalnego celu, śmierci albo wymuszonej zmianie fazy. | System pokazuje anulowanie; nie zastępuje go ukrytym atakiem. |

### 10.2. Wybór akcji przez warunki i priorytety
Każda akcja posiada warunki legalności, bazowy priorytet oraz modyfikatory sytuacyjne. AI odrzuca akcje nielegalne, oblicza wynik pozostałych i wybiera najwyższy. Remisy rozstrzyga deterministycznie z użyciem seeda walki. Każdy zestaw musi zawierać bezwarunkową akcję awaryjną.

| Dane decyzji | Przykłady | Zastosowanie |
| --- | --- | --- |
| Warunki | HP poniżej progu, cel ma Burn, poprzednia akcja, cooldown, numer tury, faza bossa. | Określają, czy akcja jest legalna. |
| Priorytet bazowy | Atak 20, obrona 60, finisher 100. | Nadaje zestawowi domyślną hierarchię. |
| Modyfikatory | +20 za stack Burn, +100 gdy akcja jest zabójcza, −50 za powtórzenie. | Pozwalają reagować na stan bez rozbudowanego drzewa zachowań. |
| Pamięć | Poprzednia akcja, liczba użyć, aktywna faza i akcje jednorazowe. | Zapobiega monotonii i obsługuje sekwencje. |
| Fallback | Podstawowy atak albo słaba akcja obronna. | Gwarantuje legalny ruch przy niespełnionych warunkach. |

### 10.3. Ekonomia akcji przeciwnika
REKOMENDACJA: zwykły przeciwnik wykonuje jedną zaplanowaną akcję na turę. AGI nie określa liczby użyć skilla; może wpływać na inicjatywę, częstotliwość akcji specjalnej albo osobne parametry tempa. Multiatak jest właściwością konkretnego skilla i jest pokazany jako jedna intencja, np. „2 × 4 obrażenia”. Przeciwnicy nie muszą płacić kolorową maną ani AP gracza; ograniczają ich warunki, cooldowny, wzorce oraz fazy.

### 10.4. Przykładowe archetypy
Poniższe liczby dla akcji wroga odpowiadają `Configs/Skills/Enemies` i powiązanym `BattleEffect`. Logika wyboru (priorytety, warunki) pozostaje koncepcyjna; wartości dmg/Shield — implementacja.

| Archetyp / akcja | Warunek lub priorytet (koncepcja) | Efekt w buildzie |
| --- | --- | --- |
| Wojownik — Cios (EnemyStrike) | Fallback. | 3 obrażenia fizyczne; CD 1. |
| Wojownik — Garda (EnemyGuard) | Wysoki priorytet bez Shield. | +5 Shield; CD 3. |
| Wojownik — Miażdżenie (EnemySlam) | Po Gardzie albo co trzecią turę. | 4 obrażenia fizyczne; CD 2. |
| Podpalacz — Rozniecenie (EnemyEmber) | Cel ma mało Burn. | +1 Burn, bez dmg natychmiast; CD 1. |
| Podpalacz — Detonacja | Najwyższy priorytet przy stackach Burn (skill gracza / boss). | Zapłon: 4 obrażenia magiczne za stack. |
| Łowca — Odsłonięcie (ExposeWeakness) | Koncepcja Cienia gracza. | Następne 3 trafienia +3 dmg. |
| Łowca — Szybkie Cięcie (QuickCut) | Ofensywa Cienia. | 2 × 4 obrażenia. |
| Łowca — Egzekucja (Execute) | Poniżej progu HP wroga. | 8 dmg; 16 gdy wróg <25% HP; CD 2. |

### 10.4a. Kotwice balansu walki (playtest)
REKOMENDACJA z bieżącego tuningu (wrzesień 2026):
- Podstawowy wróg piętra 1 (goblin, debug, Leszinio, RudoBat): około **30 Max HP** (±5) przy **Constitution 3** i progu +10 HP w drzewku Kondycji.
- Obrażenia basic attack z matchy: bonus za długość liczy tylko kafelki **powyżej matcha 3** (`BattleConfig`); supermatch i dodatkowy atak na kaskadzie bez zmian.
- Po korekcie dmg z matchy TTK wydłużył się — HP wrogów i powyższe skille (Odnowienie, Iskra, Garda wroga) są pierwszą odpowiedzią balansową, nie cofnięciem formuły matchy.

### 10.5. Bossowie i fazy
Boss korzysta z tego samego planera, ale każda faza może mieć własną pulę akcji, priorytety i regułę wejścia. Zmiana fazy może wymusić akcję przejściową, wyczyścić wybrane cooldowny lub odblokować nowy finisher. Gracz powinien widzieć zmianę fazy oraz pierwszą intencję nowego zestawu przed możliwością jej wykonania.
W pierwszym prototypie wystarczą progi 60% i 30% HP. Boss nie powinien wykonywać dodatkowej akcji natychmiast po przekroczeniu progu, chyba że przejście samo jest wcześniej zapowiedzianą mechaniką.

### 10.6. Model techniczny
- SkillDefinition przechowuje koszt, cooldown i efekty wspólne dla gracza oraz przeciwnika.
- EnemyActionDefinition opakowuje skill w dane AI: intencję, priorytet, warunki i ograniczenia powtórzeń.
- EnemyDecisionContext jest niemutowalnym snapshotem HP, Shield, statusów, numeru tury, poprzedniej akcji i fazy.
- EnemyActionPlanner pozostaje czystą klasą bez zależności od MonoBehaviour, dzięki czemu można testować decyzje bez uruchamiania sceny.
- EnemyBattleCharacter przechowuje plannedAction, wykonuje ją w swojej turze i dopiero potem planuje następną.
- UI intencji korzysta z tego samego obliczenia wartości co rzeczywisty BattleEffect, aby opis i wynik nie mogły się rozjechać.

### 10.7. Minimalny zakres AI
- Jedna akcja przeciwnika na turę oraz usunięcie liczby ataków zależnej bezpośrednio od AGI.
- Planowanie pierwszej akcji przed pierwszą turą gracza i kolejnej po każdym ruchu przeciwnika.
- Panel intencji pokazujący nazwę, ikonę, cel i przewidywany efekt.
- Warunki HP, statusu, cooldownu, poprzedniej akcji i numeru tury.
- Deterministyczne rozstrzyganie remisów i bezwarunkowy fallback.
- Dwa zestawy testowe: Wojownik i Podpalacz; Łowca jako następny test multiataku i progu HP.

## 11. Crafting
Crafting jest głównym sinkiem shardów i podstawową motywacją do wielokrotnego rozgrywania walk. Powinien specjalizować kolekcję, a nie tylko zwiększać liczby o kolejne poziomy +1/+2/+3.

### 11.1. Możliwe zastosowania
- tworzenie nowych kart i wariantów kart bazowych;
- tworzenie, montowanie i wymiana run w kartach;
- tworzenie specjalnych egzemplarzy tile’i oraz zmiana ich efektów;
- ulepszanie skilli lub odblokowywanie ich alternatywnych wersji;
- odzyskiwanie części materiałów z niepotrzebnych kart, run i tile’i;
- w późniejszej wersji: tworzenie ekwipunku, reliktów i startowych konfiguracji.

### 11.2. Tożsamość kolorów

| Kolor shardów | Typowe zastosowanie |
| --- | --- |
| Czerwone | Obrażenia, przebicie, podpalenie |
| Zielone | Leczenie, regeneracja, trucizna |
| Niebieskie | Dobieranie, kontrola, redukcja kosztu |
| Żółte | AP, szybkość, dodatkowe akcje |
| Czarne | Klątwy, poświęcenie HP, wysoka nagroda za ryzyko |

## 12. Hierarchia mocy i zabezpieczenia

| Warstwa | Typowa moc | Dlaczego |
| --- | --- | --- |
| Craftowany tile | około 1 jednostki | Częsty, losowy i część dużego decku |
| Runa karty | około 2–4 jednostek | Warunkowa nagroda albo zmiana funkcji |
| Skill | około 8–20 jednostek | Świadomie aktywowany i opłacony zasobem |
- Zwrot staminy wymaga matcha 5+, co najmniej dwóch grup, głębokiej kaskady albo innego trudnego warunku; wszystkie źródła mają wspólny limit co najmniej raz na Resolve, z dodatkowym limitem na turę do ustalenia w playteście.
- Bonusowy EXP jest naliczany po zwycięstwie i ma limit aktywacji na walkę.
- Tile EXP po pierwszym użyciu traci efekt do końca starcia.
- Bonus +1 shard na tile’u działa tylko przy matchu 4+ i ma wysoki koszt craftingu.
- Tile deck ma stałą liczbę tile’i każdego koloru oraz limit mocy efektów.
- System zawsze sprawdza legalne ruchy i przetasowuje planszę, gdy ruchów brakuje.
- Karty i runy nie powinny zapewniać pewnych efektów o skali skilla bez porównywalnego kosztu.
Undo działa tylko na ostatniej karcie i tylko przed Resolve. Po wykonaniu sekwencji wynik jest nieodwracalny, poza osobnymi efektami zaprojektowanymi jako mechanika walki.
Losowe karty muszą być deterministyczne w podglądzie albo mieć Finale. Plansza przewidywana i rzeczywista muszą wykonywać identyczne komendy, inaczej gracz straci zaufanie do planowania.
Dobieranie i zwroty staminy są rozliczane dopiero po pełnym zakończeniu kaskad. Gra sprawdza możliwość kontynuacji tury po wszystkich enchantach i runach, nie bezpośrednio po kliknięciu Resolve.

## 13. Pętla progresji między walkami
Poniższa pętla opisuje rozwój systemowy między walkami. Sposób połączenia jej z generowanym grafem dungeonu opisuje Aneks A.
- Walka: gracz układa kolejkę kart, używa Resolve, tworzy matche i kaskady, aktywuje tile’e, odzyskuje staminę i wydaje zgromadzone zasoby na skille. Jedna tura może zawierać kilka sekwencji.
- Rozliczenie: po zwycięstwie otrzymuje bazowy EXP, bonusowy EXP oraz shardy z matchy 4+.
- Poziom: po zebraniu wymaganej liczby EXP otrzymuje 1 punkt statystyki; na progach może wybrać talent.
- Crafting: wydaje kolorowe shardy na karty, runy, tile’e lub rozwój skilli.
- Konfiguracja: układa talię kart, tile deck, aktywne skille i rozdziela punkty statystyk.
- Kolejna walka: testuje nowy build i zdobywa materiały do następnej iteracji.

## 14. Długoterminowa motywacja
Progresja powinna łączyć wzrost mocy postaci z odblokowywaniem nowych możliwości. Sama stała premia typu +20% obrażeń szybko zamienia kolejne rozgrywki w obowiązkowy grind.
Główne odblokowania: nowe przepisy craftingowe, karty i rodziny ruchów, runy funkcjonalne i nagrody, skille i ich alternatywne wersje, efekty specjalnych tile’i, startowe talie, modele kosmetyczne i początkowe skille oraz — po ustaleniu game loopu — modyfikatory trudności, bossowie i zawartość dungeonu.

## 15. Zakres pierwszego prototypu progresji

| Element | Proponowane minimum |
| --- | --- |
| Plansza | 8×8, pięć kolorów |
| Tile deck | 80 tile’i: 16 każdego koloru; kilka efektów testowych |
| Karty | 20 kart: Horizontal/Vertical/Free Swap, pięć Change Color, Destroy Tile, Diagonal Swap i Cycle 2×2 |
| Runy | Około 12 run reprezentujących funkcję, nagrodę, AP, rękę i EXP |
| Skille | Katalog 30 skilli gracza (Mag/Wojownik/Cień) w configu; rdzeń playtestu §9.4; maks. 5 w loadoucie |
| Postać | STR, CON, INT, AGI; 1 punkt statystyki na poziom |
| Ekonomia | EXP, shardy z matchy 4+, stamina/AP wypłacana po Resolve i testowa mana kolorów |
| Crafting | Tworzenie lub podmiana jednej runy i jednego efektu tile’a |
| Kolejka / Resolve | Plansza przewidywana, wykonanie kart w kolejności, osobne Resolve i End Turn oraz Undo ostatniej karty przed Resolve |
| Archetypy kart | Co najmniej cztery kontrastowe profile: kaskada, super match, multimatch oraz Spektrum albo Poświęcenie |
| Przeciwnicy / AI | Jedna intencja na turę, planner warunków i priorytetów, Wojownik i Podpalacz jako dwa zestawy testowe |

### 15.1. Pytania dla playtestu
- Czy talia kart i tile deck są odczuwane jako dwa różne, uzupełniające się systemy?
- Czy gracz potrafi świadomie dążyć do matchy 4+ bez nadmiernego wydłużania tury?
- Jak często aktywują się specjalne tile’e i czy ich efekty są zauważalne, ale nie dominujące?
- Czy zwroty AP tworzą nieskończone lub zbyt długie tury i czy warunek matcha 5+ jest wystarczająco rzadki?
- Czy Match Neighbor pozostaje czytelny, nie zawyża shardów i nie uruchamia niekontrolowanych łańcuchów?
- Czy skille nadal są atrakcyjne, gdy tile’e generują manę, leczenie, tarczę i stacki Burn?
- Czy tempo zdobywania shardów daje realną decyzję craftingową po kilku walkach?
Czy tile’e i runy EXP tworzą ciekawy wybór między mocą bojową a szybszym rozwojem?
Czy Resolve jest odczuwany jako satysfakcjonująca wypłata zaprogramowanej sekwencji, a nie zbędne potwierdzenie?
Ile Resolve gracz wykonuje średnio w turze i czy stamina rzeczywiście tworzy decyzję między krótką a długą sekwencją?
Czy plansza przewidywana jasno pokazuje kolejność kart, cele i pewne matche bez obiecywania losowych kaskad?
Czy Undo ostatniej karty wystarcza do poprawiania pomyłek bez potrzeby dowolnej edycji środka kolejki?
Czy kaskada, super match, multimatch i Spektrum/Poświęcenie prowadzą do wyraźnie innych talii i decyzji?
Czy statystyki zmieniają preferowany profil Resolve, zamiast jedynie zwiększać końcowe liczby?
Czy intencja przeciwnika daje graczowi realną możliwość odpowiedzi, zamiast być wyłącznie zapowiedzią nieuniknionych obrażeń?
Czy AI pozostaje czytelne, gdy priorytety reagują na HP, Shield i statusy?
Czy jedna akcja przeciwnika na turę daje właściwe tempo, a multiataki pozostają wyjątkowe i łatwe do oszacowania?
Czy osłabienie zapowiedzianej akcji — np. oczyszczenie Burn przed Detonacją — jest wystarczająco satysfakcjonujące?

## 16. Otwarte decyzje

| Temat | Pytanie do rozstrzygnięcia |
| --- | --- |
| Krzywa EXP | Ile EXP daje walka, jak rośnie koszt poziomu i czy istnieje level cap? |
| Skille | Czy model 2 AP + podwójna mana kolorów daje właściwą częstotliwość, a cooldowny pozostają potrzebne wyłącznie dla leczenia, kontroli i mocnej obrony? |
| Shardy | Czy są wspólne dla profilu, wyprawy, czy działają w modelu mieszanym? |
| Duże matche | Jak liczyć krzyże, kształty T/L, nakładające się linie i kaskady? |
| Tile deck | Czy limitować liczbę specjalnych tile’i, czy użyć punktowego budżetu mocy? |
| Crafting | Czy gracz tworzy konkretny efekt, losuje właściwości, czy używa obu metod? |
| Porażka | Które nagrody zostają po przegranej i jak zapobiec nadużyciom? |
| Respec | Czy punkty statystyk i build można resetować; jeśli tak, jakim kosztem? |
| Game loop | Czy trzy piętra, 6–8 warstw decyzji na piętro i wskazane proporcje węzłów dają wyprawę o właściwej długości? |
| Długość kolejki | Czy ogranicza ją wyłącznie ręka i stamina, czy także twardy limit kart przed Resolve? |
| End Turn | Czy niepusta kolejka wykonuje Resolve & End Turn i świadomie traci świeżo zdobytą staminę? |
| Podgląd | Które wyniki pokazywać jako pewne, a których nie przewidywać z powodu refillów i losowych kaskad? |
| Karty losowe | Czy wszystkie używają Finale, czy część ustala seed podczas dodawania do kolejki? |
| Rewind | Jaką funkcję zachowuje w systemie z darmowym Undo ostatniej karty przed Resolve? |
| Runy nagrody | Jak limitować kilka identycznych run obserwujących ten sam raport Resolve? |
| Pętle staminy | Jaki wspólny limit zwrotów na Resolve i turę zachowuje możliwość kontynuacji bez nieskończonych tur? |
| AI i intencje | Jak wcześnie planować akcję, ile informacji pokazywać i czy boss może zmieniać zapowiedź przy przejściu fazy? |

## Aneks A. Generowanie dungeonu jako grafu

> Status: zaakceptowany kierunek koncepcyjny. Struktura grafu, role sklepu kart i kuźni tile’i, nagrody za bossów, pierwszy katalog skilli oraz model AI oparty na widocznej intencji stanowią bazę dalszego prototypu. Parametry liczbowe pozostają robocze i wymagają playtestu.
Założenie: dungeon jest generowanym grafem logicznym, a nie fizycznym labiryntem. Węzeł reprezentuje walkę, nagrodę albo usługę, natomiast krawędź reprezentuje możliwy wybór dalszej trasy. Pozycje na mapie są nadawane dopiero po utworzeniu i zweryfikowaniu poprawnego grafu.

### A.1. Rola dungeonu w pętli gry
Mapa ma zmuszać gracza do przygotowania konkretnego buildu Match-3 przed bossem. Wybór drogi powinien odpowiadać na pytanie, czy gracz potrzebuje doświadczenia, shardów określonych kolorów, nowych kart, modyfikacji tile decku czy trudniejszej walki z lepszą nagrodą. Dungeon nie jest osobną grą eksploracyjną; łączy walkę, crafting i konfigurację kolekcji.
- Karty pozostają taktycznymi narzędziami do programowania ruchów i przebudowy planszy.
- Tile deck określa skład oraz małe efekty konkretnych tile’i losowanych na planszę.
- Skille są zawsze dostępnymi elementami loadoutu, które konsumują manę, staminę/AP, statusy albo przygotowany wynik Resolve.
- Graf decyduje, którą z tych warstw gracz może rozwinąć przed następną walką i bossem.

### A.2. Struktura wyprawy i piętra
- Wyprawa składa się roboczo z trzech pięter i trwa około 30–45 minut.
- Jedno piętro zawiera 6–8 warstw decyzji, około 12–18 widocznych węzłów oraz jednego bossa. Gracz odwiedza tylko część z nich.
- Każda warstwa oferuje zwykle 2–3 dalsze możliwości. Gałęzie mogą ponownie się łączyć, ale nie powinny tworzyć przypadkowej sieci.
- Ruch postępuje w kierunku bossa. Po wybraniu nowej gałęzi gracz nie wraca swobodnie do nieodwiedzonych alternatyw, dzięki czemu wybór trasy ma konsekwencje.
- Boss i główne typy węzłów są widoczne na grafie. Szczegóły spotkań mogą być odkrywane stopniowo, ale wybór sklepu kart albo kuźni nigdy nie jest ukrytym losowaniem.

> Skrzynia -- Elita / \ Start -- Walka -- Sklep kart -- Boss \ / Kuźnia -- Walka

### A.3. Etapy generowania grafu
- Ustal seed wyprawy, numer piętra, temat środowiska, pulę przeciwników oraz bossa. Boss wyznacza końcowy test, pod który gracz może przygotowywać build.
- Zbuduj szkielet Start – obowiązkowe warstwy postępu – Boss. Każda poprawna trasa musi mieć porównywalną liczbę kroków.
- Dodaj boczne gałęzie i ponowne połączenia. Rozgałęzienie powinno przedstawiać różne wartości, a nie dwie kopie tego samego wyboru.
- Nadaj gałęziom tożsamość: walka i doświadczenie, ekonomia kart, rozwój tile decku albo wysokie ryzyko z rzadką nagrodą.
- Rozmieść typy węzłów zgodnie z ograniczeniami ekonomii. Najpierw rozkładane są wymagane punkty, następnie elastyczne walki i nagrody.
- Wylosuj zawartość spotkań z puli właściwej dla piętra oraz aktualnego poziomu trudności. Typ węzła pozostaje czytelny przed wyborem drogi.
- Zweryfikuj osiągalność bossa, długość tras, rozmieszczenie usług i nagród. Dopiero poprawny graf otrzymuje pozycje wizualne oraz połączenia na mapie.

### A.4. Typy węzłów

| Typ węzła | Funkcja w obecnej grze |
| --- | --- |
| Start | Początek piętra, konfiguracja informacji o bossie i dostępnych trasach. |
| Walka | Podstawowe źródło EXP i shardów generowanych przez matche 4+. Spotkanie korzysta z przeciwnika właściwego dla piętra. |
| Elita | Trudniejsza walka z wyraźnie lepszą nagrodą: rzadki tile, karta, większa paczka shardów albo przyszły relikt. |
| Skrzynia | Jednorazowa, jawna kategoria nagrody: shardy wybranych kolorów, EXP, karta albo materiał do ulepszeń. |
| Sklep kart | Pozyskiwanie, wymiana i usuwanie kart. Ulepszanie kart pozostaje tutaj, dopóki nie stanie się osobnym, rozbudowanym systemem. |
| Kuźnia tile’i | Tworzenie tile’i, dodawanie lub wymiana upgrade’u oraz modyfikacja aktywnego tile decku. |
| Kapliczka / wydarzenie | Późniejszy węzeł dla rzadkich zmian skilli, loadoutu, statystyk lub wyborów ryzyko–nagroda. Nie jest wymagany w pierwszej wersji. |
| Boss | Koniec piętra, duża wypłata oraz wybór jednego z trzech skilli. |

### A.5. Reguły rozmieszczania węzłów
- Przed pierwszym sklepem kart lub kuźnią musi istnieć możliwość zdobycia shardów.
- Dwa sklepy, dwie kuźnie albo dwie skrzynie nie powinny występować bezpośrednio po sobie.
- Pomiędzy większymi nagrodami powinna zwykle znajdować się walka.
- Co najmniej jedna rozsądna trasa do bossa powinna prowadzić przez sklep kart albo kuźnię.
- Trudniejsza trasa musi mieć zauważalnie lepszą lub bardziej ukierunkowaną nagrodę.
- Ślepa odnoga jest dopuszczalna wyłącznie jako świadomy koszt dojścia do wyjątkowej nagrody; nie może przypadkowo blokować ukończenia piętra.
- Generator nie oferuje obok siebie dwóch węzłów, które z punktu widzenia gracza wykonują praktycznie tę samą funkcję.
- Obozowisko z leczeniem nie jest potrzebne w obecnym modelu, ponieważ statystyki bojowe są przygotowywane od nowa na początku walki. Może wrócić dopiero po wprowadzeniu zasobu przenoszonego między spotkaniami.

### A.6. Tożsamości tras

| Profil trasy | Dominujące węzły | Decyzja gracza |
| --- | --- | --- |
| Siła | Walki, elita | Większe ryzyko w zamian za EXP i rzadkie nagrody. |
| Karty | Skrzynia, sklep kart | Rozbudowa i oczyszczenie talii sterującej planszą. |
| Tile deck | Skrzynia shardów, kuźnia | Tworzenie oraz ulepszanie konkretnych tile’i. |
| Przygotowanie | Łatwiejsza walka, kapliczka | Bezpieczna korekta loadoutu przed bossem. |

### A.7. Skille jako nagroda za bossa
Model docelowy: gra zawiera ogólny katalog około 30 unikalnych skilli. Profil przechowuje kolekcję odblokowanych skilli, natomiast do walki gracz wybiera maksymalnie 5. Wyposażone skille są zawsze dostępne i nie są dobierane losowo jak karty.
- Po pokonaniu bossa gracz otrzymuje trzy nieodblokowane skille i wybiera jeden na stałe do swojej kolekcji.
- Oferta powinna zawierać co najmniej jeden skill zgodny z kolorami lub zasobami bieżącego buildu, jeden otwierający inny kierunek oraz jeden wybór nietypowy.
- Po bossie gracz może od razu zmienić loadout przed następnym piętrem.
- Karty odpowiadają głównie za zmianę planszy, a skille za obrażenia, leczenie, tarczę, statusy i świadome wydawanie zasobów wyprodukowanych przez Resolve.
- Po odblokowaniu całego katalogu nagroda bossa może zostać zastąpiona wyborem wariantu skilla, punktem mistrzostwa albo dużą paczką shardów.
Postać: gra nie wymaga klas ani wyboru bohatera. Modele mogą być kosmetyczne i ewentualnie sugerować prosty skill początkowy, ale nie mają osobnych statystyk, drzewek ani ekskluzywnych pul. Początkowy skill zajmuje normalny slot i może zostać później wymieniony.

### A.8. Porażka, nagrody i powtarzanie węzłów
- Porażka w walce odbiera nagrody naliczone w tym spotkaniu i cofa gracza do poprzedniego bezpiecznego węzła, zgodnie z obecnym modelem mapy.
- Pokonana walka, otwarta skrzynia i wykorzystana jednorazowa usługa nie wypłacają nagrody ponownie.
- W pierwszym prototypie shardy i EXP mogą pozostać trwałe natychmiast po zwycięstwie. Model sakwy bankowanej dopiero po bossie pozostaje opcją do testu, jeśli wyprawa nie będzie miała wystarczającego napięcia.
- Jeżeli darmowe próby okażą się nadużywane, piętro może otrzymać ograniczoną Determinację albo rosnące zagrożenie po porażce. Nie jest to wymagane do pierwszej wersji generatora.

### A.9. Walidacja wygenerowanego piętra
- Istnieje co najmniej jedna poprawna droga Start – Boss.
- Każdy węzeł należy do osiągalnej części grafu albo jest świadomie oznaczoną odnogą nagrody.
- Wszystkie główne trasy mają porównywalną liczbę kroków i oczekiwaną wartość nagród.
- Sklep kart i kuźnia nie pojawiają się przed pierwszą szansą zdobycia shardów.
- Nie występują niedozwolone serie węzłów usługowych ani wybory między duplikatami funkcji.
- Boss ma przypisane poprawne spotkanie, a jego oferta skilli może wygenerować trzy legalne wybory.
- Pozycje wizualne nie przecinają nadmiernie krawędzi i zachowują czytelny kierunek postępu.
- Seed pozwala ponownie wygenerować identyczny graf do testów i odtwarzania błędów.

### A.10. Minimalny zakres pierwszej wersji
Pierwsza wersja generatora powinna obsługiwać Start, zwykłą walkę, trudniejszą walkę, skrzynię, sklep kart, kuźnię i bossa. Piętro może mieć około 12 węzłów oraz 6 decyzji przed bossem. Kapliczki, wydarzenia, relikty, Determinacja i warianty skilli są rozszerzeniami. Najważniejszym testem jest to, czy gracz potrafi świadomie wybrać trasę pod rozwój kart, tile decku albo skilli, zamiast wybierać wyłącznie najkrótszą drogę.

