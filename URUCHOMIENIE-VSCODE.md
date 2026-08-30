# Uruchomienie w Visual Studio Code

Instrukcja krok po kroku dla tego projektu na Windows 11.

---

## 0. Stan na tej maszynie — SDK jest już zainstalowany

Projekt został zbudowany i przetestowany na tym komputerze, więc krok instalacyjny masz
już za sobą:

```
C:\Users\pietr\.dotnet\sdk\8.0.424     ← SDK, zainstalowane w profilu użytkownika
C:\Program Files\dotnet\dotnet.exe     ← runtime 8.0.24 i 10.0.10 (były wcześniej)
```

`C:\Users\pietr\.dotnet` zostało dopisane do zmiennej `PATH` użytkownika, a `DOTNET_ROOT`
wskazuje ten sam katalog. **Zmienne środowiskowe czyta się przy starcie procesu**, więc żeby
`dotnet` był widoczny, trzeba raz zamknąć i otworzyć terminal albo całe VS Code. Sprawdzenie:

```powershell
dotnet --version      # oczekiwane: 8.0.424
dotnet --list-sdks
```

Wynik ostatniego pełnego przebiegu:

| Krok | Wynik |
|---|---|
| `dotnet restore` | OK, 3 projekty |
| `dotnet build` (Debug i Release) | **0 błędów, 0 ostrzeżeń** |
| `dotnet test` | **49 / 49 testów przechodzi**, 229 ms |
| `dotnet pack -c Release` | `LoyaltyClub.Sdk.1.0.0.nupkg` + `.snupkg` |

Instalacja siedzi w profilu użytkownika i nie wymagała uprawnień administratora. Żeby ją
usunąć, wystarczy skasować katalog `C:\Users\pietr\.dotnet` i wyczyścić dopisek w `PATH`.

---

## 1. Instalacja SDK na innej maszynie

Ten krok jest potrzebny dopiero przy przenoszeniu projektu na inny komputer.

**A. winget (instalacja systemowa, wymaga uprawnień administratora):**

```powershell
winget install Microsoft.DotNet.SDK.8
```

**B. Instalator ze strony Microsoftu:**

<https://dotnet.microsoft.com/download/dotnet/8.0> → *SDK 8.0.x* → *Windows x64 Installer*.

**C. Skrypt do profilu użytkownika (bez uprawnień administratora)** — dokładnie tak
zainstalowano SDK tutaj:

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Channel 8.0 -InstallDir "$env:USERPROFILE\.dotnet"
```

> Projekt celuje w `net8.0`. Nowszy SDK (np. 10) też zbuduje ten kod, bo SDK jest wstecznie
> zgodne — wymagany jest wtedy zainstalowany runtime 8.

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

Oczekiwany wynik: `Kompilacja powiodła się. Ostrzeżenia: 0, Liczba błędów: 0`.

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
i wykonuje kolejno: `Info()`, rejestrację sprzedaży, odczyt salda, a potem stronę e-commerce.

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
$env:LOYALTYCLUB_STORE_USER = "twoj-uzytkownik"
$env:LOYALTYCLUB_STORE_PASSWORD = "twoje-haslo"
dotnet run --project samples/LoyaltyClub.Sdk.Demo
```

Kody wyjścia demo — każdy odpowiada jednej gałęzi obsługi błędów SDK:

| Kod | Znaczenie |
|---|---|
| `0` | wszystko się udało |
| `2` | `LoyaltyClubValidationException` — żądanie odrzucone lokalnie, bez wywołania sieciowego |
| `3` | `LoyaltyClubApiException` — backend odpowiedział kodem błędu (np. 401 przy złych poświadczeniach) |
| `4` | `LoyaltyClubTransportException` — backend nieosiągalny |

> Przy uruchomieniu na tej maszynie demo zwróciło **3** z komunikatem
> `Backend odpowiedzial HTTP 401: Unauthorized` — backend na porcie 8089 **działa** i odpowiada
> poprawnym dokumentem RFC 7807, tylko domyślne poświadczenia `kasa-01` / `haslo` są nieprawidłowe.
> Podstaw własne przez zmienne środowiskowe z tabeli powyżej.

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

**`No .NET SDKs were found`** — terminal ma jeszcze stary `PATH`. Zamknij VS Code i otwórz
ponownie. Doraźnie w bieżącej sesji:

```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
```

**`The current .NET SDK does not support targeting .NET 8.0`** — zainstalowany SDK jest
starszy niż 8. Zainstaluj SDK 8 lub nowszy.

**Znak `#` w nazwie katalogu** — `loaltyClub-sdk-c#` zawiera `#`. MSBuild, NuGet i git radzą
sobie z tym poprawnie (sprawdzone: restore, build, test, pack, push), ale w PowerShell
**zawsze cytuj ścieżkę** (`"...\loaltyClub-sdk-c#"`), bo poza cudzysłowem `#` zaczyna komentarz
i reszta ścieżki zostanie zignorowana. W plikach projektu nie ma żadnych ścieżek bezwzględnych,
więc zmiana nazwy katalogu niczego nie zepsuje.

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
