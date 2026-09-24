# ElevenLabs CLI — efekty dźwiękowe

CLI jest zainstalowane lokalnie w tym katalogu, niezależnie od pakietów Unity.

W PowerShellu:

```powershell
cd Tools/ElevenLabs
npm.cmd ci
npm.cmd run sfx -- --text "Short metallic sword clash, sharp impact, no voices" --output sword_clash.mp3
```

Przed generowaniem zapisz swój klucz API w lokalnym pliku `Tools/ElevenLabs/.env`:

```dotenv
ELEVENLABS_API_KEY=tu_wstaw_klucz
```

Plik `.env` jest ignorowany przez Git. Nie wklejaj klucza do rozmowy ani nie zapisuj go w repozytorium. `npm run auth` loguje CLI przez przeglądarkę, ale test wywołania SFX wykazał, że samo logowanie nie przekazuje klucza API do tego endpointu. Plik MP3 można przenieść do `Assets` i zaimportować w Unity.

Parametry komendy sprawdzisz przez:

```powershell
npm.cmd run sfx -- --help
```

Pełną listę SFX z `Docs/M3p_miejsca_na_dzwiek.md` odtworzysz poleceniem:

```powershell
node generate_project_sfx.mjs
```

Skrypt pomija istniejące pliki MP3 i dopisuje do dokumentu tylko ścieżki do faktycznie zapisanych próbek.

Nie zapisuj klucza API w repozytorium.
