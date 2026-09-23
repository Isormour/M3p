# Kuźnia kart — elementy mockupu

Gotowa paczka sześciu osobnych grafik dla wskazanego mockupu z trzema kolumnami. Brakujące elementy odtworzono narzędziem image_gen, usuwając tekst i odtwarzając fragmenty zasłonięte kartą. To czyste warstwy do składania UI, nie wycinki piksel w piksel. Następnie przycięto zbędne marginesy, zachowując kanał alfa.

## Nowe grafiki

| Plik względem tego folderu | Rozmiar | Zastosowanie |
| --- | --- | --- |
| Frames/header-empty.png | 2017 × 460 | Pusty nagłówek z emblematem młota; tekst nakładany osobno |
| Frames/panel-outer-frame.png | 1749 × 802 | Rama główna z przezroczystym środkiem; 9-slice |
| Backgrounds/forge-stage-empty.png | 1122 × 1402 | Wnętrze kuźni i puste kowadło w środkowej kolumnie; tło nieprzezroczyste |
| Forge/forge-building-badge.png | 1147 × 1184 | Miniatura budynku na przezroczystym tle |
| Buttons/craft-normal.png | 1770 × 455 | Pusty pomarańczowy przycisk tworzenia; 9-slice |
| Buttons/recipe-selected.png | 1934 × 495 | Zaznaczona receptura lub aktywna zakładka; 9-slice |

Grafiki mają ustawienia importu Unity: Sprite (2D and UI), Single, Full Rect, bez mipmap, Clamp, alfa i bez kompresji. Pliki .meta są częścią paczki. Nagłówek, budynek i wnętrze używają Image Type = Simple. W nagłówku zachowaj proporcje, aby nie rozciągać emblematu młota.

Granice 9-slice w pikselach, w kolejności lewo / dół / prawo / góra:

- Rama: 160 / 105 / 160 / 150.
- Przycisk craftowania: 265 / 120 / 265 / 120.
- Zaznaczenie receptury: 180 / 120 / 180 / 120.

Dla Image Type = Sliced dopasuj Pixels Per Unit Multiplier do docelowego rozmiaru. Podgląd dla Canvas 1672 × 941 używa ramy 1340 × 714, przycisku 355 × 97 oraz zaznaczenia 315 × 64. Przy PPU sprite'a = 100 i Canvas reference PPU = 100 odpowiadają im mnożniki około 1.82, 5 i 5.88. Raycast Target włącz tylko dla przycisków; wyłącz dla ozdób i tła.

## Ponowne użycie istniejących grafik

Nie trzeba kopiować poniższych plików do CraftCard. Używaj istniejących assetów i ich GUID-ów.

| Element interfejsu | Istniejący plik lub folder |
| --- | --- |
| Panel receptur oraz panel szczegółów | ../CraftTile/Frames/enchantment-panel.png |
| Zwykła receptura, wiersz kosztu i nieaktywna zakładka | ../CraftTile/Buttons/effect-normal.png |
| Niebieskie zaznaczenie, gdy potrzebne | ../CraftTile/Buttons/effect-selected.png |
| Niebieski przycisk, gdy potrzebny | ../CraftTile/Buttons/apply-normal.png |
| Tło całego ekranu | ../CraftTile/Backgrounds/workshop-background.png |
| Ramka, dolne pole, separator i medalion karty | Assets/_Project/Graphics/Cards/ |
| Podgląd karty i miniatury receptur | Assets/_Project/Graphics/Cards/Artworks/ |
| Ikony odłamków | Assets/_Project/Graphics/UI/Common/Tiles/rune-*.png |
| Podstawa przycisku zamknięcia | Assets/_Project/Graphics/UI/Common/fantasy-button-9slice-burgundy.png |
| Separator i ozdoby | Assets/_Project/Graphics/UI/DwarvenFrames_v2_NoRunes/ |

CraftTile ma już ustawienia Sprite Multiple dla paneli i przycisków: wybierz istniejący nazwany sprite wewnątrz tekstury, aby zachować przygotowany obszar wycięcia. Nie zastępuj ich pełną teksturą z szerokimi przezroczystymi marginesami. Istniejące sloty i ramki kafelków pozostają dostępne w CraftTile; nie są potrzebne do podstawowego układu tego mockupu. Budynek forge-empty.png przedstawia prasę do kafelków, dlatego kuźnia kart dostała własną miniaturę zgodną z referencją.

## Kolejność warstw

1. Tło warsztatu lub aktualny widok mapy, opcjonalnie przyciemniony.
2. Wnętrze kuźni w środkowej kolumnie i dwa boczne panele.
3. Rama zewnętrzna, nagłówek, miniatura budynku.
4. Lista receptur, osobno grafiki przycisków, miniatury i teksty.
5. Istniejący widok karty nad kowadłem: ilustracja, dolne pole, ramka, separator, tekst i koszt.
6. Nazwa, opis i lista kosztów; przycisk craftowania i jego napis.
7. Nawigacja i zamknięcie.

Nazwy, opisy, ilości i oznaczenie X nakładaj jako tekst Unity/TextMeshPro. Złoto i pył w oryginalnym mockupie były przykładami; w podglądzie paczki użyto istniejących ikon odłamków. Docelowe koszty pobieraj z danych CraftCost, a nie z podglądu. Warianty hover, pressed i disabled można obsłużyć istniejącym Color Tint; paczka nie zawiera dodatkowych bitmap tych stanów.

## Podglądy i weryfikacja

- output/card-crafting-ui/elements-preview.jpg — sześć nowych elementów na szachownicy.
- output/card-crafting-ui/assembly-preview.jpg — ekran złożony z rzeczywistych plików tej paczki i istniejących assetów projektu.
- output/card-crafting-ui/source-mockup.png — zachowana referencja.
- output/card-crafting-ui/manifest.json — ścieżki, GUID-y, rozmiary, granice i pozycje w układzie.
- output/card-crafting-ui/generation.json — pełne prompty oraz źródła generacji image_gen.

Sprawdzono odczyt PNG, przezroczyste krawędzie, przezroczysty środek ramy, poprawność granic 9-slice oraz złożony podgląd. Przy zaznaczeniu receptury zachowano oryginalną miękką alfę generatora, której maksimum wynosi 254/255. W ramach przygotowania tej paczki nie zmieniano wspólnych grafik, scen, prefabów ani kodu gry. Podgląd jest kompozycją poza Unity; nie stanowi potwierdzenia wyglądu w uruchomionej grze.
