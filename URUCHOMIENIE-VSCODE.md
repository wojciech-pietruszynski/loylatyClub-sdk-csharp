# Uruchomienie w Visual Studio Code

Instrukcja krok po kroku dla tego projektu na Windows 11.

---

## 0. Stan wyjściowy na tej maszynie

Sprawdzone dla `C:\Users\pietr`:

```
C:\Program Files\dotnet\dotnet.exe   ← jest
runtime Microsoft.NETCore.App 8.0.24 ← jest
runtime Microsoft.NETCore.App 10.0.10← jest
katalog C:\Program Files\dotnet\sdk  ← BRAK
```

`dotnet --version` kończy się komunikatem **„No .NET SDKs were found”**. Są tylko runtime'y,
czyli można *uruchamiać* gotowe aplikacje, ale nie da się nic *zbudować*. Dlatego krok 1
jest obowiązkowy — bez niego `dotnet build` i `dotnet test` nie zadziałają.

---

## 1. Instalacja .NET SDK 8

Wybierz jedną drogę.

**A. winget (najprościej, PowerShell):**

```powershell
winget install Microsoft.DotNet.SDK.8
```

**B. Instalator ze strony Microsoftu:**

<https://dotnet.microsoft.com/download/dotnet/8.0> → *SDK 8.0.x* → *Windows x64 Installer*.

Po instalacji **zamknij i otwórz na nowo terminal** (albo cały VS Code), żeby odświeżyła się
zmienna `PATH`, a potem sprawdź:

```powershell
dotnet --version      # oczekiwane: 8.0.xxx
dotnet --list-sdks
```

> Projekt celuje w `net8.0`. Nowszy SDK (np. 10) też zbuduje ten kod, bo SDK jest wstecznie
> zgodne — wymagany jest wtedy zainstalowany runtime 8, który na tej maszynie już jest.

---

## 2. Rozszerzenia VS Code

Otwórz *Extensions* (Ctrl+Shift+X) i zainstaluj:

| Rozszerzenie | Identyfikator | Do czego |
|---|---|---|
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | obsługa solution, Test Explorer, debugger |
| C# | `ms-dotnettools.csharp` | wciągane automatycznie przez C# Dev Kit |
| EditorConfig | `editorconfig.editorconfig` | styl kodu z `.editorconfig` |

VS Code sam je zaproponuje — plik `.vscode/extensions.json` w repozytorium zawiera te
rekomendacje.

---

## 3. Otwarcie projektu

```powershell
code "C:\Users\pietr\IdeaProjects\loaltyClub-sdk-c#"
```

Otwórz **katalog projektu**, nie katalog nadrzędny — inaczej C# Dev Kit nie znajdzie
`LoyaltyClub.Sdk.sln`.

Po otwarciu, na dole po prawej, poczekaj aż zniknie pasek *„Loading projects…”*. W panelu
*Solution Explorer* powinny pojawić się trzy projekty:

```
LoyaltyClub.Sdk          (biblioteka)
LoyaltyClub.Sdk.Tests    (49 testów xUnit)
LoyaltyClub.Sdk.Demo     (program konsolowy)
```

---

## 4. Pobranie pakietów i budowanie

W terminalu VS Code (Ctrl+`, terminal PowerShell):

```powershell
dotnet restore
dotnet build
```

Albo z palety poleceń: **Ctrl+Shift+P → Tasks: Run Task → `restore` / `build`**.
`build` jest zadaniem domyślnym, więc działa też skrót **Ctrl+Shift+B**.

Oczekiwany wynik: `Build succeeded. 0 Warning(s) 0 Error(s)`.

---

## 5. Uruchomienie testów

```powershell
dotnet test
```

Albo **Ctrl+Shift+P → Tasks: Run Task → `test`**.

W GUI: ikona **Testing** (kolba) na lewym pasku → *Run All Tests*. Test Explorer pokazuje
49 testów pogrupowanych w pięć klas:

| Klasa | Testów | Zakres |
|---|---|---|
| `HttpTransportTests` | 12 | ponowienia, odświeżanie tokenu po 401, mapowanie błędów, kodowanie URI |
| `StoreClientTests` | 13 | logowanie JWT, `X-CountryCode`, walidacja lokalna, kontrakt ciała żądania |
| `EcomClientTests` | 15 | profil, saldo, historia, kupony, `Idempotency-Key`, werdykt walidacji |
| `LoyaltyClubJsonTests` | 5 | serializacja dat, pomijanie `null`, `ProblemDetail` |
| `RetryPolicyTests` | 4 | backoff wykładniczy, limit górny, jitter |

Testy podnoszą serwer-atrapę na `127.0.0.1` z portem przydzielanym przez system, więc nie
wymagają uprawnień administratora ani działającego backendu. Windows Firewall może przy
pierwszym uruchomieniu wyświetlić pytanie o dostęp — to normalne, można je odrzucić:
ruch idzie wyłącznie przez pętlę zwrotną.

---

## 6. Uruchomienie programu demonstracyjnego

Demo (`samples/LoyaltyClub.Sdk.Demo`) łączy się z **działającym backendem LoyaltyClub**
i wykonuje kolejno: `info()`, rejestrację sprzedaży, odczyt salda, a potem stronę e-commerce.

```powershell
dotnet run --project samples/LoyaltyClub.Sdk.Demo
```

Albo **F5** — konfiguracja *Demo (LoyaltyClub.Sdk.Demo)* z `.vscode/launch.json` uruchamia
program pod debuggerem, z zatrzymywaniem na breakpointach.

Parametry przekazuje się zmiennymi środowiskowymi (są też ustawione w `launch.json`):

| Zmienna | Domyślnie |
|---|---|
| `LOYALTYCLUB_BASE_URL` | `http://localhost:8089` |
| `LOYALTYCLUB_CUSTOMER` | `CUST-000123` |
| `LOYALTYCLUB_STORE_USER` / `LOYALTYCLUB_STORE_PASSWORD` | `kasa-01` / `haslo` |
| `LOYALTYCLUB_ECOM_USER` / `LOYALTYCLUB_ECOM_PASSWORD` | `ecom-shop` / `haslo` |

```powershell
$env:LOYALTYCLUB_BASE_URL = "http://localhost:8089"
$env:LOYALTYCLUB_STORE_PASSWORD = "twoje-haslo"
dotnet run --project samples/LoyaltyClub.Sdk.Demo
```

Bez uruchomionego backendu demo kończy się kodem wyjścia `4` i komunikatem
*„Backend nieosiągalny”* — to zachowanie zamierzone, nie błąd kompilacji.

---

## 7. Użycie SDK we własnym projekcie

```powershell
dotnet new console -o MojaIntegracja
cd MojaIntegracja
dotnet add reference "C:\Users\pietr\IdeaProjects\loaltyClub-sdk-c#\src\LoyaltyClub.Sdk\LoyaltyClub.Sdk.csproj"
```

albo przez pakiet:

```powershell
dotnet pack "C:\Users\pietr\IdeaProjects\loaltyClub-sdk-c#\src\LoyaltyClub.Sdk\LoyaltyClub.Sdk.csproj" -c Release
dotnet add package LoyaltyClub.Sdk --source "C:\Users\pietr\IdeaProjects\loaltyClub-sdk-c#\src\LoyaltyClub.Sdk\bin\Release"
```

Minimalny przykład jest w [`README.md`](README.md).

---

## Rozwiązywanie problemów

**`No .NET SDKs were found`** — nie wykonano kroku 1 albo terminal ma stary `PATH`.
Zamknij VS Code i otwórz ponownie.

**`The current .NET SDK does not support targeting .NET 8.0`** — zainstalowany SDK jest
starszy niż 8. Zainstaluj SDK 8 lub nowszy.

**Znak `#` w nazwie katalogu** — `loaltyClub-sdk-c#` zawiera `#`. MSBuild i NuGet radzą sobie
z tym poprawnie, ale w PowerShell **zawsze cytuj ścieżkę** (`"...\loaltyClub-sdk-c#"`),
bo poza cudzysłowem `#` zaczyna komentarz i reszta ścieżki zostanie zignorowana.
Gdyby jakieś narzędzie zewnętrzne się na tym wywróciło, wystarczy zmienić nazwę katalogu —
w plikach projektu nie ma żadnych ścieżek bezwzględnych.

**Test Explorer pusty** — poczekaj na zakończenie ładowania projektów, potem
**Ctrl+Shift+P → Test: Refresh Tests**. Jeśli dalej pusto, sprawdź, że w `.vscode/settings.json`
`dotnet.defaultSolution` wskazuje `LoyaltyClub.Sdk.sln`.

**Test wisi 5 sekund i zgłasza „Zadne zadanie nie dotarlo do serwera-atrapy”** — zapora
zablokowała połączenie zwrotne albo port został przechwycony. Uruchom testy ponownie;
serwer-atrapa za każdym razem bierze wolny port od systemu.

**Zmiany w kodzie nie są widoczne po `dotnet run`** — usuń katalogi pośrednie:

```powershell
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
dotnet build
```
