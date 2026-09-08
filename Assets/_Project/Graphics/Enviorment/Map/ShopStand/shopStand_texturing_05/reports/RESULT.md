# MaterialDetailBake — Smooth

Poprzedni wariant został odrzucony przez użytkownika jako mocno ziarnisty. Zwiększony gradient obrazu obejmował szum i nie był właściwym kryterium jakości. Nowy wariant usuwa drobne ziarno przy zachowaniu układu kamieni, głównych pęknięć, wsporników i ozdób.

Wynik: `shopStand_MaterialDetailBake_Smooth_BaseColor.png`, 4096 × 4096, RGB PNG sRGB. Kopia `shopStand_MaterialDetailBake_Smooth.blend` ma teksturę spakowaną i podłączoną do Base Color. W Unity potrzebna jest tylko ta jedna mapa.

Naprawa działała na poprzednim bake'u: wrapper fix-texture, lokalny NAFNet strength 1.0, finish nafnet, jawnie maskowana naprawa nowych punktowych artefaktów, non-local means h10/color10 i odzyskanie wyłącznie średniej skali konturów (sigma 2.2–6, gain 0.22). Nie dodawano nowych detali AI, nie przerysowywano atlasu ani nie zmieniano siatki lub UV. Poprzednie trzy generowane kafle były już częścią wejściowego bake'u.

Pierwszy profil clean-sharp odrzucono: przywracał surową wysoką częstotliwość, pozostawiał ziarno i ujawnił kolorowe punkty sieci. Zachowano go w reports/first_pass jako diagnostykę. W wybranym wariancie nie ma globalnego unsharp ani ponownego dodawania surowego ziarna. Maska punktowych napraw jest zapisana w masks/NeuralSpeckRepair.png.

Kontrola: sygnał wysokiej częstotliwości (RMS reszty po filtrze 1.4 px) w tych samych wnętrzach kamienia spadł do 19.49% poprzedniej wartości. Kontrast większych struktur wyniósł 100.35% poprzedniego; średnia jasność zmieniła się z 0.16870 do 0.16963. Ten pomiar jest wskaźnikiem ziarna, a nie dowodem zachowania każdego mikropęknięcia. Część najdrobniejszej faktury została świadomie usunięta zgodnie z korektą użytkownika.

Brak zmian wymiarów i położenia pikseli. Zero zmienionych pikseli poza dozwoloną maską geometrii UV, z zachowaniem granic szwów, otworów i istniejącego paddingu. Nie deklarujemy identycznego RGB wszystkich konturów poprzedniej wersji: wewnątrz wysp odszumienie zmienia ich drobną fakturę. Czytelność głównych kształtów oceniono na modelu. Sprawdzono osiem kierunków przed/po w 768 px oraz osiem kierunków przed/po w 192 px z filtrowaniem tekstury. Zbliżenie 1:1 porównuje oryginał, poprzedni bake i nowy wariant.

Workflow poprawiono: najpierw oczyszczenie ziarnistej bazy, potem delikatna struktura w średniej skali; brak normalizacji reszty szumowej do stałego kontrastu; oddzielne sprawdzanie ziarna i większych struktur. Historyczne skrypty pierwszego bake'u pozostają w shopStand_texturing, a bieżąca korekta i run_smooth.ps1 w D:/Unity/3dAIModeling/shopStand_texturing_02.

Nie nadpisano modelu, tekstury wejściowej ani aktywnych materiałów Unity. Nowa wersja trafia do osobnego katalogu shopStand_texturing_05. Do podmiany wybierz plik z członem Smooth; wcześniejszy katalog _04 zawiera odrzucony, bardziej ziarnisty wariant.
