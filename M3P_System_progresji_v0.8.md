# M3P — system walki i graf dungeonu

Kolejka kart, Resolve, skille w walce, AI i generowanie grafu

_Wersja robocza 0.8 · rozdzielona 11 października 2026 · dokument koncepcyjny_

## 1. Cel i status dokumentu

Dokument opisuje przebieg walki w M3P: planowanie kolejki kart, Resolve, stan planszy, użycie skilli, intencje przeciwnika oraz techniczne reguły generowania grafu dungeonu.

Progresja postaci, EXP, statystyki, kolekcje kart, run i tile’i, odblokowywanie skilli, crafting oraz nagrody między walkami zostały przeniesione do [PlayerProgression.md](PlayerProgression.md). Ten plik nie jest już źródłem prawdy dla tych zasad.

> Rdzeń walki: gracz układa z kart deterministyczną sekwencję manipulacji, wykonuje ją przez Resolve, a dopiero potem plansza sprawdza matche i kaskady. Po wypłacie efektów gracz może rozpocząć kolejną sekwencję, użyć skilla albo zakończyć turę.

| Oznaczenie | Znaczenie |
| --- | --- |
| ZAŁOŻENIE | Element wynikający bezpośrednio z obecnej koncepcji projektu. |
| REKOMENDACJA | Proponowana wersja robocza do zweryfikowania prototypem. |
| OTWARTE | Decyzja lub wartość wymagająca testów i doprecyzowania. |

## 2. Architektura walki

| System | Główna rola w tym dokumencie |
| --- | --- |
| Karty | Tworzą kolejkę deterministycznych komend manipulujących planszą. Katalog i rozwój kolekcji opisuje PlayerProgression. |
| Resolve | Wykonuje kolejkę kart, a następnie zniszczenia, opadanie, matche, kaskady i wypłaty. |
| Plansza przewidywana | Symuluje pewny wynik zakolejkowanych komend bez obiecywania losowych refillów. |
| Tile’e i runy | Dostarczają efektów rozliczanych podczas wypłaty; ich kolekcję i crafting opisuje PlayerProgression. |
| Skille | Są aktywowane świadomie między sekwencjami i konsumują zasoby przygotowane w walce. |
| AI | Planuje widoczną intencję i wykonuje ją bez ukrytej zmiany. |
| Graf dungeonu | Buduje poprawne trasy i rozmieszczenie węzłów; ich nagrody opisuje PlayerProgression. |

## 3. Zasoby bojowe

| Zasób | Rola | Status |
| --- | --- | --- |
| Stamina / AP | Koszt dodawania kart do kolejki i wykonywania aktywnych akcji. Stamina zdobyta po Resolve może zasilić kolejną sekwencję w tej samej turze. | ZAŁOŻENIE v0.3 |
| Mana koloru | Tymczasowy zasób generowany przez matche i wydawany na skille. | REKOMENDACJA |
| HP / tarcza / statusy | Stan walki modyfikowany przez skille, runy i craftowane tile’e. | ZAŁOŻENIE / rozwój |
| Złoto | Zakupy u kupca lub dodatkowy sink ekonomiczny. | OTWARTE; zależne od game loopu |
Termin roboczy: w warstwie gracza zasób może być nazywany Staminą, natomiast AP pozostaje nazwą techniczną. Resolve kończy tylko bieżącą sekwencję; End Turn kończy turę gracza.

## 4. Kolejka kart i Resolve

ZAŁOŻENIE: zagranie karty wydaje staminę/AP, zapisuje komendę wraz z celami i umieszcza kartę w kolejce. Plansza rzeczywista zmienia się dopiero po użyciu Resolve. Katalog kart, runy i archetypy talii znajdują się w [PlayerProgression.md](PlayerProgression.md).

### 4.1. Sekwencja kart, Resolve i End Turn
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

### 4.2. Plansza rzeczywista i przewidywana

| Element | Rola | Wymaganie UX / techniczne |
| --- | --- | --- |
| Plansza rzeczywista | Stan autorytatywny walki; nie zmienia się podczas dodawania kart do kolejki. | Animuje komendy dopiero po Resolve. |
| Plansza przewidywana | Deterministyczna symulacja wszystkich zakolejkowanych kart. | Kolejne cele są walidowane względem tego stanu. |
| Kolejka | Karty, cele, wybory koloru/kierunku i parametry losowe w kolejności wykonania. | Każda karta ma numer kroku i czytelne oznaczenia celów. |
| Podgląd wyniku | Przewidywane grupy matchy, największy match i inne pewne informacje. | Nie powinien obiecywać wyniku losowych refillów i kaskad. |
REKOMENDACJA: w fazie planowania tile’e nie wykonują pełnych animacji ruchu. UI pokazuje półprzezroczysty stan przewidywany, strzałki i numery kroków; właściwe animacje kart następują dopiero po Resolve.

### 4.3. Undo przed Resolve
ZAŁOŻENIE: przed Resolve gracz może cofnąć ostatnią kartę w kolejce. Karta wraca na rękę, jej koszt staminy/AP zostaje zwrócony, cele i wybory są usuwane, a plansza przewidywana jest przeliczana od początku pozostałej kolejki.
Undo można powtarzać krok po kroku aż do pustej kolejki. Nie wolno dowolnie wyjmować ani przestawiać kart ze środka, ponieważ unieważniałoby to cele późniejszych komend. Po naciśnięciu Resolve cofnięcie jest niedostępne; błędy po wykonaniu są częścią konsekwencji decyzji.

### 4.4. Kolejność wykonania i karty specjalne

| Reguła | Wersja robocza |
| --- | --- |
| Cele | Każda karta zapisuje cele na planszy przewidywanej; Resolve odtwarza te same komendy na planszy rzeczywistej. |
| Destroy | W kroku karty nakłada Cracked. Oznaczenie może przemieszczać się z tile’em; faktyczne usunięcie następuje po wszystkich kartach, przed opadaniem i sprawdzeniem matchy. |
| Gravity Shift | Ustawia kierunek opadania używany po zakończeniu kolejki i podczas następujących kaskad zgodnie z opisem karty. |
| Chaotic Shuffle | Karta losowa otrzymuje słowo kluczowe Finale i musi być ostatnia albo ustala seed już przy dodaniu, aby podgląd pozostał deterministyczny. |
| Rewind | Wymaga przeprojektowania w modelu kolejki; nie wchodzi do pierwszego prototypu Resolve. |
| End Turn z kolejką | REKOMENDACJA: wykonuje Resolve & End Turn. Stamina uzyskana z tej wypłaty nie jest już wydawana; UI komunikuje konsekwencję. |

## 5. Raport Resolve i zakresy stanu

| Zakres | Zapamiętywane dane | Przykładowi odbiorcy |
| --- | --- | --- |
| Sekwencja / Resolve | Karty w kolejce, liczba grup, największy match, głębokość kaskady, kolory, zniszczenia kartami i aktywowane enchanty. | Runy kart, obrażenia bazowe, nagrody za jakość ruchu. |
| Tura | Liczba Resolve, łączna liczba matchy, kolejność kolorów, odzyskana stamina i zużyte limity. | Talenty STR/AGI, Spektrum, Momentum i limity anty-loop. |
| Walka | Statusy, mana, Shield, Fire, EXP oczekujący, zużyte tile’e specjalne i limity nagród. | Skille, tile deck, ekonomia oraz rozliczenie zwycięstwa. |

## 6. Skille
Skille pozostają potrzebne, jeśli zapewniają mocny i kontrolowany efekt. Tile daje losową, małą premię, runa nagradza profil Resolve, natomiast skill działa wtedy, kiedy gracz świadomie konsumuje zgromadzony zasób, status albo przygotowaną sytuację.

| Kategoria skilla | Przykłady |
| --- | --- |
| Ofensywa | Bezpośrednie obrażenia, przebicie, atak obszarowy, detonacja statusów |
| Defensywa | Leczenie, tarcza, oczyszczenie, regeneracja |
| Kontrola | Buffy, debuffy, osłabienie przeciwnika, zmiana reguł tury |
| Ręka | Wymiana kart, dobieranie, odzyskiwanie kart z discardu |
| Plansza — wyjątek | Silna manipulacja połączona z kosztem, np. Meteor niszczący obszar 3×3 |

> Przykład synergii: czerwony tile Stack Burn nakłada 1 Burn (Podpalenie: 1 obrażenie/tura za stack, 2 tury, stackowalne), karta pozwala zbudować czerwony match, a skill Zapłon zużywa wszystkie stacki Burn i zadaje 4 obrażenia magiczne za każdy stack. Ognista Iskra używa osobnego statusu Iskra (2 obrażenia/tura przez 4 tury), nie globalnego Burn.

### 6.1. Okno skilli między sekwencjami
Po pełnej wypłacie Resolve gracz odzyskuje kontrolę. Może natychmiast użyć skilla wykorzystującego wygenerowaną manę, Shield, Fire albo inny zasób, a następnie — jeśli posiada staminę i karty — rozpocząć kolejną sekwencję. Użycie skilla nie musi automatycznie kończyć tury.

### 6.2. Przykładowe silniki tile–skill

| Silnik | Generowanie | Kontrolowana wypłata |
| --- | --- | --- |
| Bank Shield | Enchanty tile’i i Garda gracza (GuardStance) zwiększają Shield. | Shield Bash zadaje obrażenia równe Shield i zeruje zasób. |
| Fire / Burn | Enchanty trafionych tile’i nakładają stacki Fire/Burn. | Explode lub Ignite konsumuje stacki i zadaje obrażenia. |
| Combo / Focus | Osobne grupy, precyzyjny match albo kolejne Resolve budują zasób. | Flurry lub Execute konsumuje zasób na serię albo finisher. |
| Reagenty | Matche różnych kolorów tworzą zestaw składników. | Skill wybiera recepturę zależną od kombinacji kolorów. |
| Souls / Blood | Destroy, poświęcenie HP i enchanty Duszy na tile’ach generują zasób ryzyka. | Skill zużywa zasób ofensywnie, defensywnie albo do przywołania. |
| Growth / Marks | Efekty tymczasowo pozostają na planszy i są grupowane kartami. | Harvest lub podobny skill konsumuje przygotowane oznaczenia. |
Powyższe silniki są modułami, nie zamkniętymi klasami. Przykładowo build Shield może korzystać z multimatchy albo super matcha, a build Fire może specjalizować się w kaskadach, precyzji lub monokolorze.
REKOMENDACJA: w pierwszym prototypie każdy skill gracza kosztuje 1 AP oraz dwukrotność wskazanej many kolorów. Mana i AP są podstawowym ograniczeniem częstotliwości, a cooldown pozostaje dodatkowym zabezpieczeniem dla leczenia, kontroli i efektów o wyjątkowo wysokiej mocy. Wartości liczbowe pozostają robocze.

Koszty, loadout oraz kompletny katalog skilli gracza są utrzymywane w [PlayerProgression.md](PlayerProgression.md). Opis walki zakłada, że użycie skilla jest legalne wyłącznie wtedy, gdy gracz spełnia zdefiniowany tam koszt i ograniczenia.

## 7. Przeciwnicy i zachowania AI
AI nie powinno próbować ukrycie kontrować każdej decyzji gracza. Jego zadaniem jest tworzenie czytelnej zagadki bojowej: przeciwnik przygotowuje zagrożenie, komunikuje je, a gracz wykorzystuje planszę, karty i skille do odpowiedzi. Skille przeciwnika korzystają z tego samego systemu efektów co skille gracza, lecz ich wybór i ekonomia są asymetryczne.

### 7.1. Cykl plan — intencja — wykonanie

| Moment | Zachowanie systemu | Wymaganie czytelności |
| --- | --- | --- |
| Planowanie | Na początku walki i po zakończeniu własnej akcji przeciwnik wybiera następną akcję. | Decyzja powstaje przed turą gracza, aby można było na nią odpowiedzieć. |
| Intencja | Wybrana akcja jest przechowywana i widoczna przez całą turę gracza. | UI pokazuje ikonę, cel, typ efektu i przewidywaną wartość. |
| Kontrgra | Gracz może usunąć status, zbudować Shield, uleczyć się albo przyspieszyć finisher. | Skuteczne osłabienie intencji jest nagrodą za przygotowanie. |
| Wykonanie | Przeciwnik wykonuje dokładnie zapowiedzianą akcję, bez ponownego wyboru tuż przed użyciem. | AI nie podmienia osłabionej akcji na korzystniejszą. |
| Wyjątek | Akcja może zostać anulowana tylko przy braku legalnego celu, śmierci albo wymuszonej zmianie fazy. | System pokazuje anulowanie; nie zastępuje go ukrytym atakiem. |

### 7.2. Wybór akcji przez warunki i priorytety
Każda akcja posiada warunki legalności, bazowy priorytet oraz modyfikatory sytuacyjne. AI odrzuca akcje nielegalne, oblicza wynik pozostałych i wybiera najwyższy. Remisy rozstrzyga deterministycznie z użyciem seeda walki. Każdy zestaw musi zawierać bezwarunkową akcję awaryjną.

| Dane decyzji | Przykłady | Zastosowanie |
| --- | --- | --- |
| Warunki | HP poniżej progu, cel ma Burn, poprzednia akcja, cooldown, numer tury, faza bossa. | Określają, czy akcja jest legalna. |
| Priorytet bazowy | Atak 20, obrona 60, finisher 100. | Nadaje zestawowi domyślną hierarchię. |
| Modyfikatory | +20 za stack Burn, +100 gdy akcja jest zabójcza, −50 za powtórzenie. | Pozwalają reagować na stan bez rozbudowanego drzewa zachowań. |
| Pamięć | Poprzednia akcja, liczba użyć, aktywna faza i akcje jednorazowe. | Zapobiega monotonii i obsługuje sekwencje. |
| Fallback | Podstawowy atak albo słaba akcja obronna. | Gwarantuje legalny ruch przy niespełnionych warunkach. |

### 7.3. Ekonomia akcji przeciwnika
REKOMENDACJA: zwykły przeciwnik wykonuje jedną zaplanowaną akcję na turę. AGI nie określa liczby użyć skilla; może wpływać na inicjatywę, częstotliwość akcji specjalnej albo osobne parametry tempa. Multiatak jest właściwością konkretnego skilla i jest pokazany jako jedna intencja, np. „2 × 4 obrażenia”. Przeciwnicy nie muszą płacić kolorową maną ani AP gracza; ograniczają ich warunki, cooldowny, wzorce oraz fazy.

### 7.4. Przykładowe archetypy
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

### 7.4a. Kotwice balansu walki (playtest)
REKOMENDACJA z bieżącego tuningu (wrzesień 2026):
- Podstawowy wróg piętra 1 (goblin, debug, Leszinio, RudoBat): około **30 Max HP** (±5) przy **Constitution 3** i progu +10 HP w drzewku Kondycji.
- Obrażenia basic attack z matchy: bonus za długość liczy tylko kafelki **powyżej matcha 3** (`BattleConfig`); supermatch i dodatkowy atak na kaskadzie bez zmian.
- Po korekcie dmg z matchy TTK wydłużył się — HP wrogów i powyższe skille (Odnowienie, Iskra, Garda wroga) są pierwszą odpowiedzią balansową, nie cofnięciem formuły matchy.

### 7.5. Bossowie i fazy
Boss korzysta z tego samego planera, ale każda faza może mieć własną pulę akcji, priorytety i regułę wejścia. Zmiana fazy może wymusić akcję przejściową, wyczyścić wybrane cooldowny lub odblokować nowy finisher. Gracz powinien widzieć zmianę fazy oraz pierwszą intencję nowego zestawu przed możliwością jej wykonania.
W pierwszym prototypie wystarczą progi 60% i 30% HP. Boss nie powinien wykonywać dodatkowej akcji natychmiast po przekroczeniu progu, chyba że przejście samo jest wcześniej zapowiedzianą mechaniką.

### 7.6. Model techniczny
- SkillDefinition przechowuje koszt, cooldown i efekty wspólne dla gracza oraz przeciwnika.
- EnemyActionDefinition opakowuje skill w dane AI: intencję, priorytet, warunki i ograniczenia powtórzeń.
- EnemyDecisionContext jest niemutowalnym snapshotem HP, Shield, statusów, numeru tury, poprzedniej akcji i fazy.
- EnemyActionPlanner pozostaje czystą klasą bez zależności od MonoBehaviour, dzięki czemu można testować decyzje bez uruchamiania sceny.
- EnemyBattleCharacter przechowuje plannedAction, wykonuje ją w swojej turze i dopiero potem planuje następną.
- UI intencji korzysta z tego samego obliczenia wartości co rzeczywisty BattleEffect, aby opis i wynik nie mogły się rozjechać.

### 7.7. Minimalny zakres AI
- Jedna akcja przeciwnika na turę oraz usunięcie liczby ataków zależnej bezpośrednio od AGI.
- Planowanie pierwszej akcji przed pierwszą turą gracza i kolejnej po każdym ruchu przeciwnika.
- Panel intencji pokazujący nazwę, ikonę, cel i przewidywany efekt.
- Warunki HP, statusu, cooldownu, poprzedniej akcji i numeru tury.
- Deterministyczne rozstrzyganie remisów i bezwarunkowy fallback.
- Dwa zestawy testowe: Wojownik i Podpalacz; Łowca jako następny test multiataku i progu HP.

### 7.8. Propozycje nowych skilli i zestawów przeciwników
Poniższe skille są propozycjami designu. Parametry, których nie podano, pozostają do ustalenia.

| Użytkownik | Skill | Efekt | Czas / skalowanie |
| --- | --- | --- | --- |
| Do przypisania | Rage | Nakłada status Rage: +X obrażeń do ataku podstawowego. | Y tur; X i Y do ustalenia. |
| Leszinio (leszninio) | Regeneracja | Status regeneracji: +2 HP na turę, łącznie do 10 HP. | 5 tur. |
| Minotaur | Defensywna postawa | Status redukujący otrzymywane obrażenia o 3 × rangę. | 5 tur; ranga skaluje wartość redukcji. |
| Minotaur | Headbutt | Zadaje 15 × poziom Minotaura obrażeń. | Jednorazowy cios; typ obrażeń do ustalenia. |

Z docelowego zestawu Minotaura usuwamy Guard Up (EnemyGuard) oraz Ember (EnemyEmber). Usunięcie dotyczy wyłącznie Minotaura. Skille pozostają dostępne innym przeciwnikom; nie usuwamy efektu GuardUp, legacy skilla GuardUp ani statusu FireSparkEmber używanego przez gracza.
OTWARTE: wartości X i Y oraz użytkownicy Rage; koszty i cooldowny nowych skilli; moment ticku regeneracji Leszinia; odświeżanie i stackowanie nowych statusów; kolejność naliczania redukcji względem Shield i innych modyfikatorów. Dla Minotaura przyjmujemy 3 × rangę redukcji przez stałe 5 tur; sposób wyznaczania rangi i poziomu wymaga doprecyzowania.

## 8. Zabezpieczenia przebiegu walki

- Zwroty staminy wymagają trudnego warunku i wspólnego limitu na Resolve; dodatkowy limit na turę należy ustalić w playteście.
- Dobieranie i zwroty staminy są rozliczane dopiero po pełnym zakończeniu kaskad. Dopiero wtedy gra sprawdza możliwość kontynuacji tury.
- Undo działa tylko na ostatniej karcie i tylko przed Resolve.
- Losowe karty muszą być deterministyczne w podglądzie albo mieć słowo kluczowe Finale.
- Plansza przewidywana i rzeczywista wykonują identyczne komendy.
- System zawsze sprawdza legalne ruchy i przetasowuje planszę, gdy ruchów brakuje.
- Karty i runy nie powinny zapewniać pewnych efektów o skali skilla bez porównywalnego kosztu.

## 9. Minimalny zakres prototypu walki

| Element | Proponowane minimum |
| --- | --- |
| Plansza | 8×8, pięć kolorów i automatyczne przetasowanie przy braku ruchów. |
| Kolejka / Resolve | Plansza przewidywana, wykonanie kart w kolejności, osobne Resolve i End Turn oraz Undo ostatniej karty przed Resolve. |
| Raport Resolve | Grupy, największy match, głębokość kaskady, kolory, zniszczenia kartami i aktywowane efekty. |
| Skille | Okno użycia między sekwencjami oraz rozliczenie kosztu, cooldownu i efektu zgodnie z PlayerProgression. |
| Przeciwnicy / AI | Jedna intencja na turę, planner warunków i priorytetów, Wojownik i Podpalacz jako zestawy testowe. |

### 9.1. Pytania dla playtestu

- Czy Resolve jest satysfakcjonującą wypłatą zaprogramowanej sekwencji?
- Ile Resolve gracz wykonuje średnio w turze?
- Czy plansza przewidywana jasno pokazuje kolejność kart, cele i pewne matche?
- Czy Undo ostatniej karty wystarcza do poprawiania pomyłek?
- Czy intencja przeciwnika daje realną możliwość odpowiedzi?
- Czy AI pozostaje czytelne, gdy reaguje na HP, Shield i statusy?
- Czy jedna akcja przeciwnika na turę daje właściwe tempo, a multiataki pozostają wyjątkowe?
- Czy osłabienie zapowiedzianej akcji jest wystarczająco satysfakcjonujące?

## 10. Otwarte decyzje walki

| Temat | Pytanie do rozstrzygnięcia |
| --- | --- |
| Długość kolejki | Czy ogranicza ją wyłącznie ręka i stamina, czy także twardy limit kart przed Resolve? |
| End Turn | Czy niepusta kolejka wykonuje Resolve & End Turn i świadomie traci świeżo zdobytą staminę? |
| Podgląd | Które wyniki pokazywać jako pewne, a których nie przewidywać z powodu refillów i losowych kaskad? |
| Karty losowe | Czy wszystkie używają Finale, czy część ustala seed podczas dodawania do kolejki? |
| Rewind | Jaką funkcję zachowuje przy darmowym Undo ostatniej karty przed Resolve? |
| Pętle staminy | Jaki wspólny limit zwrotów na Resolve i turę zachowuje kontynuację bez nieskończonych tur? |
| AI i intencje | Jak wcześnie planować akcję, ile informacji pokazywać i czy boss może zmieniać zapowiedź przy przejściu fazy? |

## Aneks A. Generowanie dungeonu jako grafu

> Status: zaakceptowany kierunek koncepcyjny. Parametry liczbowe pozostają robocze i wymagają playtestu.

Dungeon jest generowanym grafem logicznym. Węzeł reprezentuje walkę, nagrodę albo usługę, a krawędź możliwy wybór dalszej trasy. Pozycje na mapie są nadawane dopiero po utworzeniu i zweryfikowaniu poprawnego grafu. Ekonomiczne funkcje węzłów, rozwój buildu, nagrody za bossów i zasady porażki znajdują się w [PlayerProgression.md](PlayerProgression.md).

### A.1. Struktura wyprawy i piętra

- Wyprawa składa się roboczo z trzech pięter i trwa około 30–45 minut.
- Jedno piętro zawiera 6–8 warstw decyzji, około 12–18 widocznych węzłów oraz jednego bossa. Gracz odwiedza tylko część z nich.
- Każda warstwa oferuje zwykle 2–3 dalsze możliwości. Gałęzie mogą ponownie się łączyć.
- Ruch postępuje w kierunku bossa; po wyborze gałęzi gracz nie wraca swobodnie do nieodwiedzonych alternatyw.
- Boss i główne typy węzłów są widoczne na grafie.

### A.2. Etapy generowania grafu

1. Ustal seed wyprawy, numer piętra, temat środowiska, pulę przeciwników oraz bossa.
2. Zbuduj szkielet Start – obowiązkowe warstwy postępu – Boss.
3. Dodaj boczne gałęzie i ponowne połączenia.
4. Nadaj gałęziom tożsamość wynikającą z typów węzłów.
5. Rozmieść wymagane punkty, a następnie elastyczne walki i nagrody.
6. Wylosuj zawartość spotkań z puli właściwej dla piętra i poziomu trudności.
7. Zweryfikuj osiągalność bossa, długość tras i rozmieszczenie usług.
8. Nadaj pozycje wizualne dopiero poprawnemu grafowi.

### A.3. Typy węzłów

Generator obsługuje: Start, Walkę, Elitę, Skrzynię, Sklep kart, Kuźnię tile’i, opcjonalną Kapliczkę / wydarzenie oraz Bossa. Ich nagrody i wpływ na progresję są zdefiniowane w PlayerProgression.

### A.4. Reguły topologii

- Każda trasa prowadzi do bossa i ma porównywalną liczbę kroków.
- Dwa węzły usługowe tego samego typu nie powinny występować bezpośrednio po sobie.
- Ślepa odnoga jest dopuszczalna wyłącznie jako świadomy koszt dojścia do wyjątkowej nagrody.
- Generator nie oferuje obok siebie dwóch węzłów wykonujących praktycznie tę samą funkcję.
- Rozgałęzienia przedstawiają różne wartości, a nie dwie kopie tego samego wyboru.

### A.5. Walidacja piętra

- Istnieje co najmniej jedna poprawna droga Start – Boss.
- Każdy węzeł jest osiągalny albo jest świadomie oznaczoną odnogą.
- Główne trasy mają porównywalną liczbę kroków i oczekiwaną wartość.
- Nie występują niedozwolone serie węzłów usługowych ani wybory między duplikatami funkcji.
- Boss ma poprawne spotkanie.
- Pozycje wizualne zachowują czytelny kierunek postępu i ograniczają przecięcia krawędzi.
- Seed odtwarza identyczny graf do testów i diagnozowania błędów.

### A.6. Minimalny zakres pierwszej wersji

Pierwsza wersja generatora obsługuje Start, zwykłą walkę, trudniejszą walkę, skrzynię, sklep kart, kuźnię i bossa. Piętro może mieć około 12 węzłów oraz 6 decyzji przed bossem. Kapliczki, wydarzenia, relikty i dodatkowe modyfikatory są rozszerzeniami.

