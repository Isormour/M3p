# Ramki i ozdoby UI — DwarvenFrames v2 — bez run

Siedem osobnych grafik PNG odtworzonych ze wskazanego wzoru przez wbudowane narzędzie image_gen. Są to rekonstrukcje elementów UI, nie wycinki piksel w piksel. Bez podpisów, liczb i ilustracji. Każdy plik zachowuje oryginalny kanał alfa generatora.

| Plik | Rozmiar | Zastosowanie |
| --- | --- | --- |
| card-frame.png | 1024 × 1536 | Karta: przezroczyste okno ilustracji, ciemne pole tytułu i opisu |
| skill-bar.png | 2172 × 724 | Pusty pasek skilla / podstawa przycisku |
| panel-wide.png | 1774 × 887 | Panel statystyk, przeciwnika lub zasobów |
| cost-badge.png | 1254 × 1254 | Pusty romb na koszt/liczbę |
| corner-ornament.png | 1254 × 1254 | Osobna ozdoba narożnika |
| icon-frame.png | 1254 × 1254 | Ramka z przezroczystym środkiem na ikonę |
| divider-ornament.png | 1971 × 798 | Ozdobny separator |

## Unity

Pliki znajdują się w Assets/_Project/Graphics/UI/DwarvenFrames_v2_NoRunes.
Dołączone ustawienia: Sprite (2D and UI), Single, Full Rect, alpha, bez mipmap i bez kompresji.
Panel i pasek skilla mają wstępne granice 9-slice. Dla nich ustaw Image Type = Sliced; dopasuj Pixels Per Unit Multiplier do docelowej wielkości UI. Pozostałe elementy używają Simple i zachowania proporcji.
Grafiki zawierają przezroczyste marginesy, szczególnie separator. Wymiary powyżej obejmują całe płótno.

Ilustrację karty umieść pod ramką; tekst, ikonę i znacznik kosztu umieść w osobnych warstwach nad nią.
Grafik nie podłączano do istniejących scen ani prefabów.
Sprawdzono pliki, kanał alfa i ustawienia importu; wygląd po imporcie w edytorze Unity nie był testowany.

Pełne prompty i ścieżki źródłowe: output/ui-dwarven-frames/manifest-no-runes.json.

