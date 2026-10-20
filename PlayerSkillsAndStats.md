# M3P — skille gracza, drzewka rozwoju i statystyki bojowe

_Wyodrębniono 17 października 2026 z [PlayerProgression.md](PlayerProgression.md), do którego odsyła [M3P_System_progresji_v0.8.md](M3P_System_progresji_v0.8.md)._

Dokument zbiera drzewka rozwoju, statystyki bojowe, odblokowywanie skilli, loadout oraz docelowy katalog 31 skilli gracza. Uwzględnia korekty formuł z 17 października 2026, usunięcie Recyklingu oraz dodanie Duchowego naznaczenia i Transmutacji duszy. Zachowuje status założeń i propozycji; nie stanowi weryfikacji zgodności z aktualną implementacją.

| Oznaczenie | Znaczenie |
| --- | --- |
| ZAŁOŻENIE | Element wynikający bezpośrednio z obecnej koncepcji projektu. |
| REKOMENDACJA | Proponowana wersja robocza do zweryfikowania prototypem. |
| OTWARTE | Decyzja lub wartość wymagająca testów i doprecyzowania. |

## 1. Założenia postaci

Gra nie wymaga klas ani wyboru bohatera. Modele postaci mogą być kosmetyczne, ale nie mają osobnych statystyk, drzewek ani ekskluzywnych pul. Mag, Wojownik i Cień są grupami katalogu skilli. Nowy profil rozpoczyna z trzema skillami opisanymi w §4.1; każdy zajmuje normalny slot loadoutu i może zostać później wymieniony.

## 2. Doświadczenie i poziomy

ZAŁOŻENIE: postać zdobywa EXP i awansuje na kolejne poziomy. Każdy poziom przyznaje **1 punkt rozwoju**, który gracz wydaje na podniesienie poziomu wybranego drzewka.

REKOMENDACJA: podstawowy EXP jest przyznawany za zwycięstwo. Dodatkowy EXP z run i tile’i nalicza się dopiero po zakończeniu wygranej walki; porażka lub ucieczka nie zachowuje premii. Ogranicza to farmienie jednej walki przez celowe przedłużanie starcia.

| Źródło dodatkowego EXP | Warunek | Nagroda i ograniczenie |
| --- | --- | --- |
| Runa Wiedzy | Match 4+ w Resolve z kartą wyposażoną w runę. | +1 EXP po walce; maks. 3 aktywacje na walkę. |
| Tile Doświadczenia | Tile zostaje zbity. | +1 EXP po zwycięstwie; po aktywacji zwykły tile do końca walki. |
| Tile Mędrca | Tile bierze udział w matchu 5+. | +2 EXP; rzadszy wariant. |

OTWARTE: bazowy EXP za walkę, krzywa kosztu kolejnych poziomów, level cap oraz zasady respecu.

## 3. Drzewka rozwoju i statystyki bojowe

ZAŁOŻENIE: drzewko rozwoju i statystyka bojowa są osobnymi pojęciami. Gracz wydaje punkty rozwoju na poziomy drzewek. Odblokowane w nich perki zwiększają parametry bazowe albo przyznają statystyki bojowe odczytywane przez formuły skilli.

Każde drzewko posiada nazwę i własny poziom, np. `Strength Level`, `Constitution Level`, `Intelligence Level` i `Agility Level`. Skróty STR, CON, INT i AGI nie oznaczają już wartości używanych bezpośrednio przez skille. Każdy wydany punkt podnosi wybrane drzewko o 1, do pułapu **30**. Perk odblokowany na danym poziomie zostaje i sumuje się z wcześniejszymi; wyższy próg nie zastępuje niższego.

Baza przed perkami: obrażenia bazowego ataku **1**, maksymalne HP **20**, ręka **3**, maksymalne AP **2** i `Power` **1**. Nowa postać zaczyna z 1. poziomem każdego z czterech obecnych drzewek.

Poniższa tabela opisuje dotychczasowy wariant debugowy. Propozycję nowego balansu ośmiu rozpisanych drzewek oraz założenie 12 drzewek opisuje §3.3.

| Drzewko | Dane poziomu | Progi | Obecny efekt progu |
| --- | --- | --- | --- |
| Strength | `PerkTree_Strength`, `Strength Level` | 2; 3; potem każdy nieparzysty od 5 do 29 | +1, potem +4, potem +5 obrażeń bazowego ataku |
| Constitution | `PerkTree_Constitution`, `Constitution Level` | 2; 3; potem każdy nieparzysty od 5 do 29 | +5 HP, potem +10 HP, potem +25 HP |
| Intelligence | `PerkTree_Intelligence`, `Intelligence Level` | każdy parzysty od 2 do 30 | +1 karta na ręce |
| Agility | `PerkTree_Agility`, `Agility Level` | każdy parzysty od 2 do 30 | +1 maksymalne AP |

Przy `Strength Level` 1 bazowy atak ma 1 obrażenie, przy 2 ma 2, przy 3 ma 6, a od 5 rośnie o 5 na każdym nieparzystym progu. Przy poziomie 29 i 30 wynosi 71. `Constitution Level` 1 daje 20 HP, 2 daje 25 HP, 3 daje 35 HP, a 29 i 30 dają 360 HP. Ręka wynosi `3 + floor(Intelligence Level / 2)`, więc startowo 3, a przy poziomie 30 aż 18. Maksymalne AP wynoszą `2 + floor(Agility Level / 2)`, więc startowo 2, a przy poziomie 30 aż 17.

Drzewka nie zawierają wyboru jednego z dwóch talentów. Katalog talentów jest pusty.

### 3.1. Drzewka jako filtr wyniku planszy

REKOMENDACJA: drzewka zmieniają względną wartość profilu Resolve. Te same karty mogą dzięki temu wspierać różne buildy zależnie od rozwoju postaci, tile decku i skilli.

| Drzewko | Preferowany profil | Naturalna synergia | Możliwe przełamanie talentem |
| --- | --- | --- | --- |
| Strength | Wiele osobnych grup i kilka krótkich Resolve. | Każda grupa może wykonywać osobny fizyczny atak. | Heavy Blow premiujący największy match. |
| Constitution | Aktywacja defensywnych enchantów. | Większe HP wspiera leczenie, Shield i pojemność obrony. | Konwersja obrony w ofensywę. |
| Intelligence | Super match, monokolor albo spektrum. | Większa ręka ułatwia przygotowanie many i statusów pod skille. | Mag kaskad wzmacniany liczbą fal. |
| Agility | Dłuższe kolejki lub więcej sekwencji. | Więcej staminy/AP zwiększa liczbę instrukcji przed Resolve. | Premia za krótkie, precyzyjne sekwencje. |

Przykład: trzy matche po trzy tile’e mogą wykonać trzy ataki wspierane przez drzewko Strength, ale nie tworzą shardów. Jeden match dziewięciu tile’i wykonuje jeden atak, lecz daje dużą wypłatę progową, manę i shardy. Talenty mogą odwracać tę domyślną preferencję.

### 3.2. Statystyki bojowe używane przez skille

ZAŁOŻENIE: skill nie odczytuje bezpośrednio poziomu drzewka. W jego definicji znajduje się formuła wskazująca jedną lub więcej statystyk bojowych. Końcowa wartość statystyki jest sumą premii ze wszystkich drzewek oraz innych dozwolonych źródeł.

```text
CombatStat = wartość bazowa + perki drzewek + ekwipunek + statusy
Efekt skilla = wartość bazowa skilla + współczynnik × CombatStat
```

Kilka drzewek może rozwijać tę samą statystykę, a jedno drzewko może rozwijać kilka statystyk. Przykładowo drzewka Strength i Might mogą oba przyznawać `Power`, lecz oferować inne progi, perki warunkowe i profile gry. Pozwala to graczowi łączyć oba drzewka bez wiązania skilla z jedną nazwą drzewka.

Pierwszą zdefiniowaną statystyką bojową jest `Power`. Mocne Cięcie korzysta z niej według wzoru:

```text
Mocne Cięcie = 6 + 2 × Power obrażeń fizycznych
```

Przy bazowym `Power = 1` skill nadal zadaje 8 obrażeń na początku gry. Kolejne źródła `Power` utrzymują jego znaczenie wraz ze wzrostem HP przeciwników. Wartości bazowe pozostałych property i ich docelowe zastosowania w skillach opisuje §5.3; rozróżniono tam zatwierdzone korekty i robocze propozycje.

### 3.3. Balans perków — katalog drzewek

ZAŁOŻENIE: docelowy katalog zawiera **12 drzewek łącznie**, wliczając obecne cztery. Rozpisano **8 drzewek**: Siłę, Inteligencję, Kondycję, Zręczność, Żywioły, Might, Tactic i Faith. Nazwy, specjalizacje i perki pozostałych **4 drzewek** wymagają ustalenia.

ZAŁOŻENIE: **perki z tym samym efektem mają tę samą nazwę**, także na różnych progach. Każde odblokowanie stałej premii dodaje ją do wcześniejszych odblokowań. Przykładowo cztery odblokowania Ciężkiej ręki dają łącznie +4 do obrażeń podstawowych.

REKOMENDACJA: poniższe drzewka stanowią roboczy balans do testów, przygotowany 17 października 2026. Zachowują limit 30 poziomów i przyznają perki na poziomach 3, 6, 9, 12, 15, 18, 21, 24, 27 i 30. Dodatkowe AP i karty powinny być rzadkimi kamieniami milowymi; pozostałe progi rozwijają statystyki bojowe i premie warunkowe. Tabele opisują projekt, nie potwierdzają wdrożenia w assetach ani kodzie.

**Rozliczenie sekwencji (Resolve)** oznacza wykonanie jednej zaplanowanej kolejki kart wraz ze wszystkimi powstałymi dopasowaniami i kaskadami oraz naliczeniem ich efektów. W jednej turze można rozliczyć kilka sekwencji. Grupa oznacza osobne dopasowanie; trzy grupy po trzy kafelki to inny wynik niż jedno dopasowanie dziewięciu kafelków. Przy sprawdzaniu poniższych warunków liczą się również dopasowania z kaskad tej samej sekwencji.

#### 3.3.1. Siła (Strength)

Specjalizacja: obrażenia podstawowe, `Power` i wiele osobnych dopasowań w jednej sekwencji. Bonus do obrażeń podstawowych działa przy każdej grupie, dlatego jego pojedyncze przyrosty są małe.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Ciężka ręka | +1 do obrażeń podstawowych. |
| 6 | Krzepa | +1 `Power`. |
| 9 | Ciężka ręka | +1 do obrażeń podstawowych. |
| 12 | Krzepa | +1 `Power`. |
| 15 | Seria uderzeń | Przy co najmniej 2 grupach w jednej sekwencji pierwszy atak podstawowy tej sekwencji zadaje dodatkowe 2 obrażenia. Raz na sekwencję. |
| 18 | Ciężka ręka | +1 do obrażeń podstawowych. |
| 21 | Krzepa | +1 `Power`. |
| 24 | Ciężka ręka | +1 do obrażeń podstawowych. |
| 27 | Krzepa | +1 `Power`. |
| 30 | Przełamanie | Rozliczenie sekwencji z co najmniej 3 grupami nadaje status Przełamanie. Raz na turę. |

**Status Przełamanie:** następny fizyczny skill zadaje +4 obrażenia. Użycie takiego skilla zużywa status. Status nie kumuluje się i wygasa na koniec tury.

Przykład: wykonanie kolejki dwóch kart tworzy trzy osobne dopasowania. Po zakończeniu sekwencji gracz otrzymuje Przełamanie. Następne Mocne Cięcie zadaje dodatkowe 4 obrażenia i zużywa status.

Na poziomie 30 samo drzewko daje **+4 do obrażeń podstawowych i +4 `Power`**. Przy bazowym ataku 1 i `Power = 1`, bez innych źródeł premii, oznacza to **atak podstawowy 5**, **`Power = 5`** oraz **16 obrażeń Mocnego Cięcia** (`6 + 2 × 5`), przed premiami warunkowymi.

#### 3.3.2. Inteligencja (Intelligence)

Specjalizacja: `Arcane`, umiarkowane zwiększanie ręki i premie za jedno duże dopasowanie. `Arcane` wzmacnia skille, które wskazują tę statystykę w swojej formule. Bazowe `Arcane = 0`; konkretne formuły i ich status projektowy opisuje §5.3.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Wiedza tajemna | +1 `Arcane`. |
| 6 | Pojemny umysł | +1 karta do początkowej ręki w każdej turze. |
| 9 | Wiedza tajemna | +1 `Arcane`. |
| 12 | Wiedza tajemna | +1 `Arcane`. |
| 15 | Skupienie | Dopasowanie co najmniej 5 kafelków nadaje status Skupienie. Raz na turę. |
| 18 | Wiedza tajemna | +1 `Arcane`. |
| 21 | Pojemny umysł | +1 karta do początkowej ręki w każdej turze. |
| 24 | Wiedza tajemna | +1 `Arcane`. |
| 27 | Wiedza tajemna | +1 `Arcane`. |
| 30 | Natchnienie | Dopasowanie co najmniej 7 kafelków nadaje status Natchnienie. Raz na turę. |

**Status Skupienie:** następny magiczny skill zadający obrażenia otrzymuje +3 obrażenia do pierwszego trafienia. Trafienie zużywa status.

**Status Natchnienie:** użycie następnego magicznego skilla dobiera 1 kartę i zużywa status.

Oba statusy są przyznawane po zakończeniu sekwencji, nie kumulują się i wygasają na koniec tury. Dopasowanie 7+ może przyznać oba statusy, jeżeli odpowiednie perki są odblokowane i nie wykorzystały jeszcze swoich limitów w tej turze. Warunek dotyczy rozmiaru pojedynczego dopasowania, a nie sumy kafelków z kilku grup.

Na poziomie 30 samo drzewko daje **+6 `Arcane` i +2 karty do początkowej ręki w każdej turze**. Przy bazowej ręce 3, bez innych źródeł premii, oznacza to **rękę 5 kart**, zamiast 18 w wariancie debugowym. Dobieranie ze statusu Natchnienie jest osobną premią w trakcie tury.

#### 3.3.3. Kondycja (Constitution)

Specjalizacja: maksymalne HP, `Guard` i `Recovery`, uzupełnione statusami pomagającymi przetrwać przy niskim zdrowiu. `Guard` rozwija skille zapewniające Shield, a `Recovery` skille leczące, zgodnie z ich formułami. Samo zdobycie tych statystyk nie przyznaje automatycznie tarczy ani leczenia. Bazowe `Guard = 0` i `Recovery = 0`; konkretne formuły i ich status projektowy opisuje §5.3.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Wytrzymałość | +10 maksymalnego HP. |
| 6 | Solidna obrona | +1 `Guard`. |
| 9 | Wytrzymałość | +10 maksymalnego HP. |
| 12 | Witalność | +1 `Recovery`. |
| 15 | Hart | Po otrzymaniu obrażeń, jeśli masz najwyżej 50% HP, otrzymujesz status Hart. Raz na turę. |
| 18 | Wytrzymałość | +10 maksymalnego HP. |
| 21 | Solidna obrona | +1 `Guard`. |
| 24 | Wytrzymałość | +10 maksymalnego HP. |
| 27 | Witalność | +1 `Recovery`. |
| 30 | Drugi oddech | Gdy otrzymane obrażenia po raz pierwszy w walce obniżą HP do 25% lub mniej, otrzymujesz status Drugi oddech. |

**Status Hart:** następne trafienie przeciwnika zadaje o 3 obrażenia mniej, minimalnie 0. Trafienie zużywa status. Status nie kumuluje się i wygasa na początku następnej tury gracza.

**Status Drugi oddech:** regeneruje 3 HP na początku każdej z trzech następnych tur gracza, łącznie 9 HP. Status może zostać przyznany tylko raz na walkę i nie chroni przed śmiertelnym trafieniem.

Na poziomie 30 samo drzewko daje **+40 maksymalnego HP, +2 `Guard` i +2 `Recovery`**. Przy bazowych 20 HP, bez innych źródeł premii, oznacza to **60 HP**. Statusy stanowią osobne premie warunkowe.

#### 3.3.4. Zręczność (Agility)

Specjalizacja: `Precision`, `Tempo` i rzadkie premie do AP. Perki warunkowe wspierają zarówno krótkie, precyzyjne zagrania, jak i dłuższe kolejki kart. Bazowe `Precision = 0` i `Tempo = 0`; zastosowania w konkretnych formułach skilli oraz ich status projektowy opisuje §5.3.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Precyzja | +1 `Precision`. |
| 6 | Zapas energii | +1 maksymalne AP. |
| 9 | Szybkość | +1 `Tempo`. |
| 12 | Precyzja | +1 `Precision`. |
| 15 | Celność | Sekwencja złożona z dokładnie jednej karty, która utworzy przynajmniej jedno dopasowanie, nadaje status Celność. Raz na turę. |
| 18 | Szybkość | +1 `Tempo`. |
| 21 | Precyzja | +1 `Precision`. |
| 24 | Zapas energii | +1 maksymalne AP. |
| 27 | Precyzja | +1 `Precision`. |
| 30 | Zryw | Rozliczenie sekwencji złożonej z co najmniej trzech kart nadaje status Zryw. Raz na turę. |

**Status Celność:** następny fizyczny skill zadający obrażenia otrzymuje +2 obrażenia do pierwszego trafienia. Trafienie zużywa status.

**Status Zryw:** następna karta kosztująca AP ma koszt obniżony o 1, minimalnie do 0. Zagranie jej zużywa status. Premia dotyczy wyłącznie kart.

Oba statusy są przyznawane po zakończeniu sekwencji, nie kumulują się i wygasają na koniec tury. Limit jednej aktywacji Zrywu na turę zapobiega pętli darmowych zagrań.

Na poziomie 30 samo drzewko daje **+4 `Precision`, +2 `Tempo` i +2 maksymalne AP**, czyli **4 AP przy bazowych 2**, bez innych źródeł premii. Pierwsze dodatkowe AP jest dostępne na poziomie 6, co daje wtedy 3 AP. Zryw pozwala dodatkowo oszczędzić 1 AP na turę po przygotowaniu dłuższej sekwencji.

#### 3.3.5. Żywioły

Specjalizacja: `Arcane`, `Recovery` i nakładanie Burn. Magia ofensywna przygotowuje mocniejsze leczenie. Burn jest statusem nakładanym na przeciwnika, a nie stałą statystyką bojową postaci.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Wiedza tajemna | +1 `Arcane`. |
| 6 | Witalność | +1 `Recovery`. |
| 9 | Rozniecenie | Pierwszy magiczny skill zadający obrażenia w turze nakłada dodatkowy 1 stack Burn na pierwszego trafionego przeciwnika. |
| 12 | Wiedza tajemna | +1 `Arcane`. |
| 15 | Żar życia | Trafienie magicznym skillem przeciwnika mającego Burn nadaje status Żar życia. Raz na turę. |
| 18 | Wiedza tajemna | +1 `Arcane`. |
| 21 | Witalność | +1 `Recovery`. |
| 24 | Rozniecenie | Pierwszy magiczny skill zadający obrażenia w turze nakłada dodatkowy 1 stack Burn na pierwszego trafionego przeciwnika. |
| 27 | Wiedza tajemna | +1 `Arcane`. |
| 30 | Płomień odnowy | Zużycie co najmniej 3 stacków Burn jednym skillem nadaje status Płomień odnowy. Raz na walkę. |

**Status Żar życia:** następny skill leczący przywraca dodatkowe 2 HP przy pierwszym leczeniu i zużywa status. Status nie kumuluje się i wygasa na koniec tury. Przeciwnik musi mieć Burn przed trafieniem, więc samo nałożenie go Roznieceniem nie wystarcza.

**Status Płomień odnowy:** regeneruje 3 HP na początku każdej z trzech następnych tur gracza, łącznie 9 HP.

Na poziomie 30 samo drzewko daje **+4 `Arcane`, +2 `Recovery` i +2 stacki Burn nakładane pierwszym magicznym skillem ofensywnym w turze**. Oba odblokowania Rozniecenia wzmacniają tę samą aktywację na pierwszym trafionym przeciwniku; premia nie jest przyznawana osobno za każde trafienie skilla.

#### 3.3.6. Might

Specjalizacja: `Power` i maksymalne HP, z premiami do fizycznych skilli oraz walki przy niskim zdrowiu.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Krzepa | +1 `Power`. |
| 6 | Wytrzymałość | +10 maksymalnego HP. |
| 9 | Krzepa | +1 `Power`. |
| 12 | Wytrzymałość | +10 maksymalnego HP. |
| 15 | Furia | Gdy masz najwyżej 50% HP, aktywny jest status Furia. |
| 18 | Krzepa | +1 `Power`. |
| 21 | Wytrzymałość | +10 maksymalnego HP. |
| 24 | Krzepa | +1 `Power`. |
| 27 | Wytrzymałość | +10 maksymalnego HP. |
| 30 | Upór | Trafienie fizycznym skillem, gdy masz najwyżej 50% HP, nadaje status Upór. Raz na turę. |

**Status Furia:** +1 `Power`. Status znika, gdy HP wzrośnie powyżej 50%. Nie kumuluje się.

**Status Upór:** na początku następnej tury gracza przywraca 3 HP, po czym znika. Nie kumuluje się i nie chroni przed śmiertelnym trafieniem.

Na poziomie 30 samo drzewko daje **+4 `Power` i +40 maksymalnego HP**. Przy bazowym `Power = 1` i 20 HP, bez innych źródeł premii, oznacza to **5 `Power` i 60 HP**, a przy połowie zdrowia lub mniej **6 `Power` dzięki Furii**.

#### 3.3.7. Tactic

Specjalizacja: `Tempo` i maksymalny rozmiar ręki. Perki warunkowe ułatwiają wybór kart i przygotowanie następnej tury.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Szybkość | +1 `Tempo`. |
| 6 | Pojemny umysł | +1 karta do początkowej ręki w każdej turze. |
| 9 | Szybkość | +1 `Tempo`. |
| 12 | Szybkość | +1 `Tempo`. |
| 15 | Przegrupowanie | Na początku każdej tury otrzymujesz status Przegrupowanie. |
| 18 | Szybkość | +1 `Tempo`. |
| 21 | Pojemny umysł | +1 karta do początkowej ręki w każdej turze. |
| 24 | Szybkość | +1 `Tempo`. |
| 27 | Szybkość | +1 `Tempo`. |
| 30 | Rezerwa | Na początku każdej tury otrzymujesz status Rezerwa. |

**Status Przegrupowanie:** pozwala odrzucić jedną kartę z ręki i dobrać jedną w jej miejsce, bez kosztu AP. Wymiana zużywa status. Niewykorzystany status wygasa na koniec tury.

**Status Rezerwa:** na koniec tury pozwala zachować jedną niewykorzystaną kartę na następną turę. Zachowana karta zajmuje normalne miejsce w początkowej ręce: przy ręce 5 kart gracz dobiera wtedy 4. Status zostaje zużyty lub wygasa, jeśli gracz nie zachowa karty.

Na poziomie 30 samo drzewko daje **+6 `Tempo` i +2 do rozmiaru ręki**, czyli **5 kart przy bazowych 3**, bez innych źródeł premii. Dodatkowo pozwala wymienić jedną kartę na turę i zachować jedną na kolejną. Żaden z tych statusów nie kumuluje się.

#### 3.3.8. Faith

Specjalizacja: `Guard` i Dusze (`Soul`). Dusze są zasobem walki, a nie stałą statystyką bojową. Drzewko zapewnia ich początkowy zapas i nagradza wydawanie ich obroną.

| Poziom drzewka | Perk | Efekt |
| --- | --- | --- |
| 3 | Solidna obrona | +1 `Guard`. |
| 6 | Dar dusz | +1 Dusza na początku każdej walki. |
| 9 | Solidna obrona | +1 `Guard`. |
| 12 | Dar dusz | +1 Dusza na początku każdej walki. |
| 15 | Wiara | Użycie skilla zużywającego Dusze nadaje status Wiara. Raz na turę. |
| 18 | Solidna obrona | +1 `Guard`. |
| 21 | Dar dusz | +1 Dusza na początku każdej walki. |
| 24 | Solidna obrona | +1 `Guard`. |
| 27 | Dar dusz | +1 Dusza na początku każdej walki. |
| 30 | Sanktuarium | Zużycie co najmniej 3 Dusz jednym skillem nadaje status Sanktuarium. Raz na turę. |

**Status Wiara:** następny skill zapewniający Shield przyznaje dodatkowe 3 Shield przy pierwszym nałożeniu i zużywa status. Premia dotyczy kolejnego skilla. Status nie kumuluje się i wygasa na koniec tury.

**Status Sanktuarium:** na początku następnej tury gracza przyznaje 6 Shield, po czym znika. Nie kumuluje się.

Na poziomie 30 samo drzewko daje **+4 `Guard` i 4 Dusze na początku walki**. Dusze z Daru dusz są przyznawane jednorazowo na walkę; ich późniejsze generowanie zapewniają skille i enchanty. Statusem Sanktuarium można zostać nagrodzonym raz na turę, pod warunkiem zużycia wymaganej liczby Dusz.

## 4. Kolekcja i odblokowywanie skilli

ZAŁOŻENIE: gracz może posiadać **maksymalnie 4 drzewka skilli**, ale rozpoczyna rozgrywkę z **jednym drzewkiem**. Na początku rozgrywki pojawia się ekran wyboru **1 z 3 oferowanych drzewek skilli**. Wybrane drzewko staje się pierwszym drzewkiem gracza. Limit drzewek jest niezależny od liczby skilli w aktywnym loadoucie.

Stan docelowy: profil przechowuje kolekcję odblokowanych skilli, a do walki gracz wybiera **maksymalnie 5**. Wyposażone i odblokowane skille są zawsze dostępne podczas walki i nie są dobierane losowo jak karty. Nowy profil odblokowuje wyłącznie trzy skille z §4.1.

Źródłowy katalog zawierał 30 definicji gracza, pogrupowanych jako Mag, Wojownik i Cień — po 10 skilli. Po usunięciu Recyklingu i dodaniu dwóch skilli Dusz docelowy katalog liczy **31 skilli**: 10 Mag, 10 Wojownik i 11 Cień. Są to grupy katalogu, a nie obowiązkowe klasy postaci ani ekskluzywne pule.

### 4.1. Skille startowe

Nowy profil ma odblokowane i domyślnie wyposażone dokładnie trzy skille:

| Skill | Asset | Funkcja startowa |
| --- | --- | --- |
| Mocne Cięcie | `Player/Warrior/HeavySlash` | `6 + 2 × Power` obrażeń fizycznych; podstawowa opcja ofensywna. |
| Odnowienie | `Player/Mage/Renew` | Regeneracja: +2 HP na początku każdej tury gracza przez `4 + 2 × Recovery` tury; cooldown 4 tury. |
| Garda | `Player/Warrior/GuardStance` | `10 + 3 × Guard` Shield; cooldown 2 tury. |

Te trzy skille zajmują 3 z 5 miejsc początkowego loadoutu. Gracz może je później zdjąć lub zastąpić innymi odblokowanymi skillami. Wszystkie pozostałe skille z docelowego katalogu rozpoczynają jako zablokowane.

Legacy/testowe definicje `BasicAttackSkill`, `BasicHeal`, `GuardUp`, `ShieldBash` i `Ignite` nie należą do puli odblokowań za bossów. Wszystkie trzy skille startowe — `HeavySlash`, `Renew` i `GuardStance` — należą do docelowego katalogu 31 skilli. Pozostaje więc **28 skilli możliwych do odblokowania za bossów**, po wdrożeniu nowych definicji. Recykling jest usunięty z katalogu docelowego i nie należy do tej puli.

### 4.2. Nagroda za bossa

- Po pokonaniu bossa istniejący panel losuje **trzy różne skille** z puli nadal zablokowanych skilli i pokazuje je graczowi.
- Skille startowe, legacy/testowe definicje oraz skille już odblokowane nie mogą pojawić się w ofercie.
- Gracz wybiera jeden z trzech skilli i odblokowuje go **na stałe** w kolekcji profilu. Dwa niewybrane pozostają zablokowane i mogą wrócić do puli przy kolejnych bossach.
- Losowanie nie powinno powtarzać skilla w obrębie jednej oferty. Jeśli w puli zostały mniej niż trzy skille, panel pokazuje wszystkie pozostałe.
- Docelowo oferta może uwzględniać build: jeden skill zgodny z jego kolorami lub zasobami, jeden otwierający inny kierunek oraz jeden wybór nietypowy. Do pierwszej wersji wystarczy losowanie trzech różnych legalnych opcji.
- Po bossie gracz może od razu zmienić loadout przed następnym piętrem.
- Po odblokowaniu całego katalogu nagrodę może zastąpić wybór wariantu skilla, punkt mistrzostwa albo duża paczka shardów.

### 4.3. Rozwój i synergie

Crafting może służyć do ulepszania skilli lub odblokowywania alternatywnych wersji. Docelowa pula powinna rozszerzać silniki buildu: Fire/Burn, Shield, Combo/Focus, Spektrum, Blood/Souls oraz rękę / tempo.

Build Dusz może generować zasób przez Żniwo Cieni za tile’e zniszczone kartami, status Duchowe naznaczenie za każde trafienie oraz enchant tile’a **Dusza**. Enchant daje +1 Duszę, gdy jego tile zostanie zbity przez match albo bezpośrednio kartą, maksymalnie 3 Dusze łącznie z enchantów na Resolve; limit ten dotyczy enchantów. Dusze pozostają zasobem tymczasowym walki. Ostrze Dusz zamienia je w trafienia fizyczne, a Transmutacja duszy w leczenie; oba skille zużywają wszystkie zgromadzone Dusze.

Skille skalują się ze wskazanymi w ich formule statystykami bojowymi, a nie bezpośrednio z poziomem drzewka. Drzewka Strength, Constitution, Intelligence, Agility oraz przyszłe drzewka mogą przyznawać te statystyki przez swoje perki. Pierwszym zatwierdzonym powiązaniem jest Mocne Cięcie korzystające z `Power`, zgodnie z §3.2.

Domyślny koszt każdego skilla gracza wynosi **1 AP oraz podwojoną względem wcześniejszej wersji manę kolorów**. Konkretne koszty many, cooldowny i efekty opisuje katalog w §5; kosztów many już podwojonych nie należy podwajać ponownie.

## 5. Koszty, loadout i docelowy katalog skilli

Skille gracza są elementami aktywnego loadoutu, a nie kartami dobieranymi losowo. Do loadoutu można wybrać wyłącznie skille odblokowane w kolekcji profilu, maksymalnie pięć. Użycie skilla następuje wyłącznie między sekwencjami, po pełnym zakończeniu Resolve, i samo w sobie nie kończy tury.

| Element | Reguła pierwszego prototypu | Uzasadnienie |
| --- | --- | --- |
| Koszt podstawowy | 1 AP oraz dwukrotność wskazanej many kolorów. | Skill konkuruje o tempo tury i wymaga wcześniejszego przygotowania planszy. |
| Cooldown | Brak dla zwykłych wypłat; Zależny od skilla; Odnowienie: 4 tury, Garda gracza: 2 tury, Ognista Iskra: 0. | Mana ogranicza większość skilli; cooldown zabezpiecza efekty, które mogłyby blokować walkę. |
| Loadout | Maksymalnie 5 wyposażonych skilli. | Gracz buduje spójny zestaw zamiast mieć zawsze dostęp do całej kolekcji. |
| Skalowanie | Skill wskazuje własną formułę i statystyki bojowe. Nie odczytuje bezpośrednio poziomu drzewka. | Różne drzewka mogą rozwijać tę samą statystykę, a gracz może łączyć ich wkład. Szczegóły opisuje §3.2. |
| Atak bez many | `BasicAttackSkill` pozostaje definicją legacy/test i nie jest odblokowany na starcie ani oferowany przez bossów. | Darmowy atak za samo AP omija przygotowanie Match-3 i osłabia rolę kart. |

### 5.1. Rdzeń testów balansu (nie zestaw startowy)
Poniższy zestaw służy do testowania Fire/Burn, Bank Shield, leczenia w czasie i ekonomii ręki. Nie określa początkowych odblokowań profilu; te definiuje §4.1. Efekty uwzględniają docelowe korekty z 17 października 2026 i wymagają synchronizacji z implementacją.

| Skill (asset) | Koszt | Efekt | Rola |
| --- | --- | --- | --- |
| Ognista Iskra (FireSpark) | 1 AP + 6 czerwonych | `2 × (1 + Arcane)` obrażeń magicznych i 3 stacki Burn. | Obrażenia i przygotowanie Burn pod kolejne skille. |
| Zapłon (IgniteBurst) | 1 AP + 4 czerwone + 2 czarne | Zużywa wszystkie stacki Burn; proponowane obrażenia: `4 × zużyte stacki + Arcane`. | Finisher na Podpalenie z tile’i / Rozniecenia / wroga. |
| Garda (GuardStance) | 1 AP + 6 niebieskich | `10 + 3 × Guard` Shield. Cooldown: 2 tury. | Bank Shield gracza (Wojownik). |
| Uderzenie Tarczą (ShieldSlam) | 1 AP + 4 niebieskie + 2 żółte | Zużywa cały Shield; obrażenia równe wartości Shield, maks. 20. | Ofensywna wypłata Bank Shield. |
| Odnowienie (Renew) | 1 AP + 6 zielonych | Status regeneracji przez `4 + 2 × Recovery` tury: +2 HP na początku każdej tury gracza. Cooldown: 4 tury. | Podtrzymanie rozłożone w czasie, nie burst heal. |
| Impet (Momentum) | 1 AP + 4 żółte | Dobiera 1 kartę; przy ≥2 grupach w ostatnim Resolve dobiera drugą. Proponowany próg `Tempo ≥ 6` daje jeszcze jedną, maks. 3. Raz na turę. | Multimatch → kolejna sekwencja (Cień). |

ZAŁOŻENIE: każdy skill gracza kosztuje domyślnie 1 AP oraz dwukrotność many kolorów (patrz §5). Źródło prawdy dla skilli i ich skalowania stanowi niniejszy dokument; implementacja pozostaje do późniejszej synchronizacji.

### 5.2. Kierunki pełnego katalogu
Docelowa pula 31 skilli powinna rozszerzać silniki zamiast mnożyć warianty tych samych obrażeń. Każdy nowy skill powinien co najmniej generować zasób, konsumować przygotowany stan albo zmieniać wartość konkretnego profilu Resolve.

| Rodzina | Przykładowe skille | Profil Resolve / build |
| --- | --- | --- |
| Fire / Burn | Rozniecenie, Feniks, Pożoga | Czerwone matche, kaskady i kumulowanie statusu. |
| Shield | Mur, Odbicie, Twierdza | Multimatch, drzewko Constitution i świadome bankowanie obrony. |
| Combo / Focus | Flurry, Egzekucja, Perfekcyjny Cios | Wiele grup albo dokładnie jeden precyzyjny match. |
| Spektrum | Widmowy Rytuał, Pryzmat, Alchemia | Kilka kolorów w jednym Resolve lub określona kolejność kolorów. |
| Blood / Souls | Krwawy Pakt, Żniwo Cieni, Duchowe naznaczenie, Ostrze Dusz, Transmutacja duszy | Destroy, utrata HP, generowanie Dusz trafieniami oraz wydawanie ich na ofensywę albo leczenie. |
| Ręka / tempo | Impet, Grad ciosów, Szybkie Cięcia, Zabójcza Sekwencja | Długie kolejki, kilka Resolve, dobieranie i liczba trafień. Recykling usunięty z katalogu. |

### 5.3. Docelowy katalog skilli i formuły property

Docelowy katalog gracza: **10 Mag, 10 Wojownik i 11 Cień — łącznie 31 skilli**. Katalog uwzględnia usunięcie Recyklingu, zmianę nazwy Szerokiego Zamachu na **Grad ciosów**, zmianę nazwy Szybkiego Cięcia na **Szybkie Cięcia** oraz dodanie Duchowego naznaczenia i Transmutacji duszy. Żniwo Cieni pozostaje osobnym skillem.

Tabele są źródłem docelowych efektów, a nie raportem stanu assetów. **ZAŁOŻENIE** oznacza formułę lub zmianę podaną przez użytkownika; **REKOMENDACJA** zachowuje wcześniejszą propozycję skalowania, której użytkownik jeszcze nie zastąpił. Aktualizacja z 17 października 2026 dotyczy wyłącznie dokumentu.

Wartości bazowe używane przez formuły: `Power = 1`; `Arcane`, `Guard`, `Recovery`, `Precision` i `Tempo` mają bazę 0. `floor` oznacza zaokrąglenie w dół. Liczba grup, kolorów, kart i dodatkowych fal kaskady pochodzi z ostatniej w pełni rozliczonej sekwencji, jeżeli opis nie wskazuje inaczej. Burn, Shield i Dusze są statusami lub zasobami, nie property.

Każdy wpis w kolumnie kosztu zawiera manę po podwojeniu i wymaga dodatkowo **1 AP**. Dla dwóch nowych skilli koszt many i cooldown pozostają **OTWARTE**. W loadoucie mieści się maksymalnie 5 odblokowanych skilli (§4 i §5). Legacy/testowe `BasicAttackSkill`, `BasicHeal`, `GuardUp`, `ShieldBash` i `Ignite` pozostają poza katalogiem i pulą nagród.

| Mag — skill (property) | Koszt many + 1 AP | CD / limit | Efekt | Status |
| --- | --- | --- | --- | --- |
| Ognista Iskra (`Arcane`) | 6 czerwonych | 0 | Zadaje `2 × (1 + Arcane)` obrażeń magicznych i nakłada 3 stacki Burn. Nie nakłada osobnego statusu Iskra. | ZAŁOŻENIE |
| Rozniecenie (`Arcane`) | 4 czerwone | raz/turę | Nakłada `2 + floor(Arcane / 2)` stacków Burn. Bez obrażeń natychmiastowych i bez wcześniejszego limitu 6 stacków na użycie. | ZAŁOŻENIE |
| Zapłon (`Arcane`) | 4 czerwone + 2 czarne | — | Zużywa wszystkie stacki Burn. Zadaje `4 × zużyte stacki + Arcane` obrażeń magicznych. Wymaga co najmniej jednego stacka Burn. | REKOMENDACJA |
| Pożoga (`Arcane`) | 6 czerwonych + 2 czarne | 2 tury | Zadaje `10 + Arcane` obrażeń magicznych i natychmiast uruchamia jeden tick Burn. Nie zużywa Burn. | ZAŁOŻENIE |
| Chromatyczny Pocisk (`Arcane`) | 2 many z 3 różnych kolorów | — | Zadaje `min(15, (1 + Arcane) × liczba kolorów)` obrażeń magicznych. Najpierw dodaje 1 do `Arcane`, następnie mnoży przez liczbę kolorów. Limit 15 obejmuje cały wynik. | ZAŁOŻENIE |
| Łańcuch Błyskawic (`Arcane`, `Tempo`) | 4 żółte + 2 czarne | — | Zadaje `4 + 3 × Tempo × liczba dodatkowych fal kaskady + Arcane` obrażeń magicznych. Bez wcześniejszego limitu 16. | ZAŁOŻENIE |
| Lodowa Osłona (`Guard`) | 4 niebieskie + 2 czarne | 2 tury | Przyznaje `8 + 2 × Guard` Shield. Następny atak przeciwnika zadaje o `10 × (1 + Guard)` obrażeń mniej, minimalnie 0; jest to redukcja płaska, nie procentowa. | ZAŁOŻENIE |
| Konwergencja (`Arcane`, `Recovery`, `Guard`) | po 2 z pięciu kolorów | 4 tury | Zadaje `15 + Arcane` obrażeń magicznych, leczy `6 + Recovery` HP i przyznaje `6 + Guard` Shield. Przy ≥4 kolorach w sekwencji każdy efekt rośnie o 50%; wynik zaokrąglany w dół. | REKOMENDACJA |
| Odnowienie (`Recovery`) | 6 zielonych | 4 tury | Nakłada regenerację na `4 + 2 × Recovery` tury. Na początku każdej tury gracza leczy 2 HP; `Recovery` zwiększa czas, nie wartość ticka. Łącznie do `8 + 4 × Recovery` HP. | ZAŁOŻENIE |
| Transmutacja Many (`Tempo`) | 4 różnych kolorów | raz/turę | Zamienia do `min(6, 3 + floor(Tempo / 3))` many między dwoma kolorami. Zachowuje łączną ilość many. | REKOMENDACJA |

| Wojownik — skill (property) | Koszt many + 1 AP | CD / limit | Efekt | Status |
| --- | --- | --- | --- | --- |
| Garda (`Guard`) | 6 niebieskich | 2 tury | Przyznaje `10 + 3 × Guard` Shield. | ZAŁOŻENIE |
| Twierdza (`Guard`) | 4 niebieskie + 2 żółte | 2 tury | Przyznaje `min(16, 4 + 3 × Guard × liczba grup)` Shield. Limit 16 obejmuje cały wynik. | ZAŁOŻENIE |
| Uderzenie Tarczą (zasób Shield) | 4 niebieskie + 2 żółte | — | Zużywa cały Shield. Zadaje obrażenia fizyczne równe zużytej wartości, maks. 20. Bez dodatkowego skalowania z property. | ZAŁOŻENIE |
| Mocne Cięcie (`Power`) | 6 czerwonych | — | Zadaje `6 + 2 × Power` obrażeń fizycznych. | ZAŁOŻENIE |
| Grad ciosów (`Power`, `Tempo`) | 4 czerwone + 4 żółte | — | Wykonuje `liczba grup + Tempo` trafień. Każde zadaje `1 + floor(Power / 3)` obrażeń fizycznych. Zastępuje Szeroki Zamach; bez wcześniejszego limitu 5 trafień. | ZAŁOŻENIE |
| Szał Bitewny (`Power`, `Tempo`) | 4 czerwone + 2 żółte | — | Zadaje `4 + 3 × liczba wcześniejszych sekwencji w turze + (Power − 1) + floor(Tempo / 2)` obrażeń fizycznych. Bez limitu obrażeń z liczby wcześniejszych sekwencji. | ZAŁOŻENIE |
| Perfekcyjny Cios (`Precision`, `Power`) | 4 czerwone + 2 czarne | — | Zadaje `6 + 3 × Precision` obrażeń fizycznych. Jeśli sekwencja utworzyła dokładnie jedno dopasowanie bez kaskady, dodaje `4 × floor(Power / 2)` obrażeń. | ZAŁOŻENIE |
| Egzekucja (`Power`, `Precision`) | 6 czerwonych + 2 czarne | 2 tury | Zadaje `8 + Power + 2 × Precision` obrażeń fizycznych. Jeśli przeciwnik ma mniej niż 25% HP, podwaja cały wynik. | ZAŁOŻENIE |
| Riposta (`Guard`, `Power`) | 4 niebieskie + 2 czarne | 2 tury | Status Riposta: następne trafienie przeciwnika zadaje o `4 + 3 × Guard` obrażeń mniej, minimalnie 0. Następnie wykonuje kontrę za `6 + (Power − 1)` obrażeń fizycznych. Trafienie zużywa status. | ZAŁOŻENIE |
| Nieugięty (`Guard`, `Power`) | 6 żółtych + 2 niebieskie + 2 czarne | 4 tury | Przyznaje `12 + 3 × Guard` Shield i status wzmacniający następny atak o `(25 + 5 × Power)%`. Jeśli przy użyciu masz mniej niż 30% HP, dodatkowo otrzymujesz 1 AP. Premia dotyczy następnego ataku, nie tylko fizycznego skilla. | ZAŁOŻENIE |

| Cień — skill (property / zasób) | Koszt many + 1 AP | CD / limit | Efekt | Status |
| --- | --- | --- | --- | --- |
| Impet (`Tempo`) | 4 żółte | raz/turę | Dobiera 1 kartę, a przy ≥2 grupach w sekwencji 2 karty. Przy `Tempo ≥ 6` dobiera jeszcze jedną, maks. 3 łącznie. | REKOMENDACJA |
| Szybkie Cięcia (`Tempo`, `Precision`) | 4 czerwone + 2 czarne | — | Wykonuje `1 + Tempo` ataków, każdy za `2 + Precision` obrażeń fizycznych. Zastępuje nazwę Szybkie Cięcie. | ZAŁOŻENIE |
| Cios w Plecy (`Precision`) | 4 czerwone + 2 czarne | — | Zadaje `9 + Precision` obrażeń fizycznych albo `15 + Precision`, gdy przeciwnik zapowiada ofensywę. | REKOMENDACJA |
| Taniec Ostrzy (`Precision`, `Tempo`) | 4 czerwone + 4 czarne | — | Jedno trafienie fizyczne za `3 × liczba kart w ostatniej kolejce + Precision + Tempo` obrażeń. Bez limitu obrażeń z liczby kart. | ZAŁOŻENIE |
| Zabójcza Sekwencja (`Precision`, `Tempo`) | 6 czerwonych + 4 czarne | 3 tury | Zadaje `6 × liczba kart w kolejce + Precision + Tempo` obrażeń fizycznych. Bez wcześniejszego limitu 20 i dodatkowego bonusu za jedno dopasowanie bez kaskady. | ZAŁOŻENIE |
| Odsłonięcie Słabości (`Tempo`, `Precision`) | 4 żółte + 2 czarne | — | Nakłada na przeciwnika status zwiększający obrażenia następnych `2 + floor(Tempo / 2)` trafień o `3 + 2 × Precision` każde. Każde trafienie zużywa jeden ładunek. | ZAŁOŻENIE |
| Krwawy Pakt (`Arcane`, `Tempo`) | 4 zielone | raz/turę | Tracisz `5 + floor(Arcane / 4)` HP i otrzymujesz `2 + floor(Tempo / 4)` AP. Utrata HP nie może zabić. Po koszcie 1 AP zysk netto wynosi `1 + floor(Tempo / 4)` AP. | ZAŁOŻENIE |
| Żniwo Cieni (Dusze) | 4 zielone | — | Otrzymujesz 1 Duszę za każdy kafelek zniszczony bezpośrednio kartą w ostatniej sekwencji, maks. 6. Pozostaje osobnym skillem obok Duchowego naznaczenia. | ZAŁOŻENIE |
| Duchowe naznaczenie (Dusze) | OTWARTE | OTWARTE | Nadaje rzucającemu status na jedną turę: każde trafienie atakiem podstawowym lub skillem generuje 1 Duszę. Każde trafienie wielokrotnego ataku liczy się osobno. | ZAŁOŻENIE |
| Ostrze Dusz (Dusze) | 4 czerwone + 2 czarne | — | Zużywa wszystkie Dusze. Każda zużyta Dusza wykonuje jedno fizyczne trafienie za 4 obrażenia. Bez skalowania z `Power` i bez wcześniejszego limitu 4 Dusz. | ZAŁOŻENIE |
| Transmutacja duszy (Dusze) | OTWARTE | OTWARTE | Zużywa wszystkie Dusze i leczy `2 × liczba zużytych Dusz` HP. Bez dodatkowego skalowania z `Recovery`. | ZAŁOŻENIE |

| Status | Czas | Tick | Uwagi |
| --- | --- | --- | --- |
| Podpalenie (Burn) | 2 tury, stackowalne | 1 obrażenie/stack na turę wroga | Ognista Iskra, Rozniecenie, tile Stack Burn, EnemyEmber; Zapłon zużywa stacki, a Pożoga uruchamia dodatkowy tick. |
| Odnowienie (RenewRegen) | `4 + 2 × Recovery` tury | +2 HP/tura gracza | `Recovery` zwiększa czas trwania; wartość ticka pozostaje stała. |
| Duchowe naznaczenie | 1 tura; do końca bieżącej tury gracza | +1 Dusza za każde trafienie | Status rzucającego. Trafienia wielokrotne liczą się osobno; ticki Burn i innych efektów okresowych nie są trafieniami ataku. |

Osobny status Iskra (`FireSparkEmber`) pochodzi z wcześniejszej wersji Ognistej Iskry i nie jest już częścią jej docelowego efektu. Dotychczasowe assety mogą nadal go zawierać do czasu wdrożenia zmian.

OTWARTE: koszt many i cooldown Duchowego naznaczenia oraz Transmutacji duszy; szczegółowe zasady odnawiania i kumulowania statusu Duchowego naznaczenia; zakres pojęcia „następny atak” dla wielokrotnych trafień Nieugiętego. Nowym skillom nie dopisano nieuzgodnionych limitów użyć.

### 5.4. Historia implementacji i powiązania assetów

Poniższe akapity opisują wcześniejszy stan implementacji. Docelowe efekty po korektach z 17 października 2026 określa §5.3; w razie różnicy jego formuły mają pierwszeństwo. Aktualizacja dokumentu nie zmienia kodu, assetów ani zapisów profili.

Odnowienie (Renew) zastępuje natychmiastowe +10 HP z BasicHeal statusem Odnowienie (RenewRegen). Status trwa 4 tury i leczy +2 HP na początku każdej swojej tury, łącznie do 8 HP. Cooldown wzrasta z 3 do 4 tur; koszt docelowy wynosi 1 AP + 6 zielonych. Nowe assety: RenewRegen, RenewHealTick i RenewRegenApply.
Ognista Iskra (FireSpark) zadaje 2 obrażenia magiczne zamiast 6 i nakłada Iskrę (FireSparkEmber): 2 obrażenia na turę przez 4 tury. Iskra jest osobnym statusem i nie stackuje się z globalnym Burn. Łączny bazowy efekt przy wszystkich tickach: 2 + 8 = 10 obrażeń. Cooldown 0 pozostaje bez zmian, a koszt docelowy wynosi 1 AP + 6 czerwonych. Nowe assety: FireSparkEmber i FireSparkDotTick; FireSparkBurn wskazuje na Iskrę zamiast Burn.
Garda wroga (EnemyGuard) zachowuje cooldown 3, a jej efekt GuardUp daje 5 Shield zamiast 10. Ten sam efekt wykorzystuje legacy skill GuardUp (gracz/test), więc również daje 5 Shield. Garda gracza (GuardStance) korzysta z osobnego efektu GuardStanceShield i zachowuje 10 Shield oraz cooldown 2; jej koszt docelowy to 1 AP + 6 niebieskich.
Mocne Cięcie otrzymuje skalowanie `6 + 2 × Power`; przy bazowym `Power = 1` zachowuje początkowe 8 obrażeń. Efekty pozostałych 26 skilli gracza pozostają bez zmian do czasu przypisania im statystyk bojowych i współczynników. Koszty many wszystkich 30 skilli gracza pozostają podwojone względem wcześniejszej wersji, natomiast koszt AP każdego skilla wynosi 1.

## 6. Otwarte decyzje i uwagi do źródeł

| Temat | Pytanie do rozstrzygnięcia |
| --- | --- |
| Krzywa EXP | Ile EXP daje walka, jak rośnie koszt poziomu i czy istnieje level cap? |
| Talenty | Drzewka dają automatyczne, sumujące się perki. Czy zastąpić je wyborem na progach 5/10/15? |
| Respec | Czy można resetować punkty rozwoju i poziomy drzewek; jeśli tak, jakim kosztem? |
| Statystyki bojowe | Czy zaakceptować pozostałe robocze formuły z §5.3 oraz jakie limity property i efektów przyjąć dla docelowych 12 drzewek? |
| Wspólne źródła | Jak ograniczyć kumulowanie jednej statystyki z kilku drzewek, ekwipunku i statusów, aby zachować sens różnych buildów? |
| Skille | Czy koszt 1 AP i podwojonej many zapewnia właściwą częstotliwość? Jak rozwijać warianty i zastąpić nagrodę bossa po ukończeniu kolekcji? |

Źródło podaje wybór jednego z trzech drzewek skilli na start i limit czterech drzewek, a równocześnie trzy stałe skille startowe z grup Mag i Wojownik oraz odblokowywanie pojedynczych skilli po bossie. Nie określa struktury drzewek skilli ani powiązania tych reguł. Zachowano wszystkie zapisy bez dopisywania brakujących zależności. Drzewka rozwoju i oddzielne statystyki bojowe są opisane w §3.

W tabeli rekomendacji §3.1 pojawiają się możliwe synergie i talenty. Obecne perki podnoszą bazowy atak, maksymalne HP, rękę i maksymalne AP, a katalog talentów jest pusty. Nowy model z §3.2 pozwala tym oraz przyszłym drzewkom dodatkowo przyznawać statystyki bojowe używane przez formuły skilli.

Zakres wyodrębnienia: [PlayerProgression.md](PlayerProgression.md), §§2–3, §7 oraz §14.4 (z pominięciem tabeli skilli przeciwników); pytania z §13. Reguły przebiegu walki i okna użycia skilli opisuje [M3P_System_progresji_v0.8.md](M3P_System_progresji_v0.8.md), §6.
