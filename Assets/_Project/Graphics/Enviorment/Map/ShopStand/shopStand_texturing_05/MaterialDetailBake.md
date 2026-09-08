# MaterialDetailBake

Workflow wzbogacania istniejącego Base Color przez oddzielne neutralne detale materiałowe, projekcję triplanarną na rzeczywistej powierzchni i bake do zachowanego UV. Wynik dla Unity: jedna tekstura albedo.

## Korekta po ocenie użytkownika: powierzchnia bez ziarna

Pierwszy wariant shopStand został odrzucony jako ziarnisty. Wzrost gradientu o około 3% nie był dowodem poprawy: wskaźnik obejmował szum. Poniższe zasady mają pierwszeństwo przed ustawieniami historycznego przykładu.

Najpierw oczyść ziarnistą bazę. Nowy detal powinien być delikatny i w średniej skali, bez gęstej, równomiernej mikropajęczyny. Nie normalizuj reszty wysokich częstotliwości do stałego kontrastu, bo wzmacnia to szum. Nie stosuj globalnego wyostrzania ani odzyskiwania surowego ziarna z obrazu wejściowego. Chroniony kontur oznacza główną granicę lub motyw, nie każdy drobny jasny piksel.

Kontrola jakości musi rozdzielać wysokoczęstotliwościowy sygnał we wnętrzach kamienia od kontrastu makrostruktur. Po odszumianiu oczekuj spadku pierwszego i zachowania drugiego; nie wymagaj mechanicznie zachowania 75% szumu. Porównuj przy skali 1:1 oraz przy rzeczywistej wielkości modelu na ekranie. Wariant z ziarnem należy odrzucić nawet wtedy, gdy przechodzi techniczne testy UV i paddingu.

Aktualna naprawa istniejącego bake'u jest w D:/Unity/3dAIModeling/shopStand_texturing_02. Używa wrappera fix-texture z NAFNet, potem lokalnej korekty punktowych artefaktów, NLM i odzyskania jedynie średniej skali konturów. Nie generuje ponownie kafli i nie przerysowuje atlasu. Dla samej naprawy zachowuje pikselowo istniejący padding i czarne granice maski UV. Pełny ponowny bake nadal wymaga paddingu po ostatniej operacji RGB.

## Niezmienniki

- Zachowuj układ kamieni, szczelin, okuć, ornamentów i istniejących motywów lawy. AI generuje wyłącznie osobne mikrodetale powierzchni; nigdy atlas ani nową interpretację widoków modelu.
- Nie zmieniaj oryginalnego modelu, UV ani plików wejściowych. Pracuj w nowym katalogu i na kopii.
- Maska głównych konturów ma blokadę RGB: w jej wnętrzu wynik jest identyczny z oryginałem. Konserwatywna maska jest zapisywana do kontroli.
- Materiał określaj z przypisań siatki, a przy jednym slocie z kontrolowanych masek atlasu. Nie uznawaj automatycznie całego obrzeża za metal.

## Etapy

1. Zbadaj evaluated mesh, UV, skalę, sloty i nakładanie wysp. Oryginalna tekstura zgodna z UV jest bazą struktury.
2. Wygeneruj osobno szary detal kamienia, metalu i lawy. Usuń makrogradient i uśrednij detal do neutralnej wartości. Sprawdź powtarzalność; implementacja używa lustrzanego okresowego kafla, zapewniając zgodne brzegi. Zachowaj oryginały generacji i prompty.
3. Na powierzchni w przestrzeni obiektu próbkuj trzy rzuty XYZ. Mieszaj wagami `abs(normal)^4`, z normalnymi uzgodnionymi na zdublowanych pozycjach. Zachowaj wspólny początek, skalę i orientację projekcji. Dla tkanin, włosów i kierunkowych rys wybierz UV lub kierunek styczny.
4. Wyznacz normalne, AO i podpisaną krzywiznę z siatki. W tej implementacji AO: 128 deterministycznych promieni półkuli cosinusowej na wierzchołek, BVH i interpolacja barycentryczna. Krzywizna: podpisana średnia odchyłka sąsiadów od płaszczyzny stycznej. Są to mapy zależne od gęstości low-poly, bez rzeźbionego high-poly. Normal jest mapą object-space do pracy, nie normalem tangent-space do Unity. Opcjonalny bevel/subdivision stosuj tylko na kopii do wypalania i sprawdzaj zgodność sylwetki; w próbie shopStand nie był potrzebny.
5. Zdefiniuj maski materiałów oraz konturów. Podgląd masek musi pokazać poprawne przypisanie. Maska lawy może modulować wyłącznie istniejące jasne pomarańczowe smugi; kolor nie dowodzi fizycznej emisji. Nie twórz nowych świecących obszarów.
6. Połącz delikatnie oryginalny RGB, detal powierzchni i geometrię. Skalarne mnożenie RGB zachowuje barwę. AO i krzywizna mają małą siłę, aby nie dublować mocno cieni już obecnych w obrazie. W opcjonalnym oczyszczeniu drobnego ziarna nie naruszaj blokady konturów.
7. Wypal do niezmienionego atlasu. Ostatnia operacja RGB to najbliższy kolor wyspy w pierścieniu paddingu (32 px przy 4K). Nie zeruj potem pikseli poza UV.
8. Porównaj osiem kierunków przed/po i pomniejszone widoki z filtrowaniem tekstury. Sprawdź zachowanie konturów, kontrastu, pokrycia UV i paddingu. Istniejące czarne szczeliny odróżniaj od nowych pustych pikseli przez porównanie z wejściem. Dokumentuj każdy wyjątek progu.

## Uruchomienie przykładu shopStand

Konfiguracja: `config.json`. Narzędzia w `scripts/`: `export_geometry.py` (Blender), `material_detail_bake.py` (Python z numpy, scipy, opencv), `render_qa.py` (Blender), `qa_summary.py` (Python z Pillow). `run.ps1` odtwarza przykład z zachowanych kafli, bez ponownego kosztu generacji AI.

Przed zastosowaniem do innego modelu utwórz nowy katalog roboczy, ustaw właściwy obiekt i ścieżki oraz przygotuj nowe maski materiałów. Wielokąty w `metal_regions_1600` dotyczą wyłącznie atlasu shopStand; nie są uniwersalnym segmentatorem. Skrypty przykładu zapisują nazwy wyników z prefiksem shopStand.

## Postacie

Rzutuj w pozycji spoczynkowej i wypal przed animacją. W czasie gry albedo ma korzystać z UV, aby detal podążał za deformacją. Chroń twarz, oczy, usta, tatuaże i szwy osobnymi maskami. AO między ruchomymi kończynami stosuj bardzo oszczędnie. Triplanar pasuje do porów i niekierunkowej struktury pancerza; nie zastępuje rzeźby anatomii ani kierunkowego ułożenia włókien. Jedno albedo jest wariantem stylizowanym; oddzielny normal i roughness pozostają przydatne, jeśli projekt wymaga reakcji na zmienne światło.

## Wynik i granice

Zastosowanie shopStand zachowuje 4096 × 4096, układ UV, siatkę i paletę. To korekta mikrostruktury, nie przebudowa projektu. Triplanar eliminuje zmianę fazy nowej warstwy na szwach UV, ale nie usuwa automatycznie wcześniejszych różnic koloru, rozciągnięć UV ani granic błędnej maski. Jakość AO i krzywizny jest ograniczona rozdzielczością siatki. Albedo nie daje prawdziwej emisji ani nowych fizycznych normalnych.
