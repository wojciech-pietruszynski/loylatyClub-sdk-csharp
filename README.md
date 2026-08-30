# LoyaltyClub .NET SDK

SDK w czystym C# do API programu lojalnościowego **LoyaltyClub** — obsługuje integrację
kasową (`/api/store/**`) oraz e-commerce (`/api/ecom/**` i `/api/coupon/**`).

Port 1:1 biblioteki [`loyaltyClub-java-sdk`](../loyaltyClub-java-sdk): te same endpointy,
nagłówki, semantyka ponowień, walidacja lokalna, mapowanie błędów i kontrakt JSON.

- **.NET 8**, jeden projekt, brak frameworka aplikacyjnego
- transport: `System.Net.Http.HttpClient` z BCL
- JSON: `System.Text.Json` z BCL
- **zero zależności NuGet w runtime** (odpowiednik Jacksona jest w bibliotece standardowej)
- automatyczne logowanie i odświeżanie tokenu JWT dla sklepu
- ponowienia z wykładniczym backoffem, wyłącznie dla operacji bezpiecznych do powtórzenia
- walidacja żądań po stronie klienta, zanim pójdzie round-trip po HTTP 400

> **Jak to uruchomić w VS Code:** [`URUCHOMIENIE-VSCODE.md`](URUCHOMIENIE-VSCODE.md).

---

## Dokumentacja techniczna

Pełne opracowanie kontraktu API, decyzji architektonicznych, modelu niezawodności i zakresu
weryfikacji znajduje się w wersji dla Javy:
[`../loyaltyClub-java-sdk/docs/dokumentacja-sdk.html`](../loyaltyClub-java-sdk/docs/dokumentacja-sdk.html).
Ponieważ zachowanie jest 1:1, dokument obowiązuje także dla tej biblioteki — różnice
językowe zebrano niżej w sekcji [Mapowanie Java → C#](#mapowanie-java--c).

---

## Instalacja

```bash
dotnet build -c Release
```

Referencja projektowa w aplikacji hosta:

```xml
<ProjectReference Include="..\loaltyClub-sdk-c#\src\LoyaltyClub.Sdk\LoyaltyClub.Sdk.csproj" />
```

albo pakiet NuGet zbudowany lokalnie:

```bash
dotnet pack src/LoyaltyClub.Sdk/LoyaltyClub.Sdk.csproj -c Release
```

Wymagania: **.NET SDK 8.0+**.

---

## Store — API kasowe

Rola `STORE`. SDK loguje się przez `POST /api/store/auth/login` i samo wymienia token
przed upływem ważności (backend daje 15 minut) oraz po odpowiedzi 401.

```csharp
using StoreClient store = StoreClient.Builder()
    .BaseUrl("http://localhost:8089")
    .Credentials("kasa-01", "haslo")
    .DefaultCountryCode("PL")
    .Build();

StoreTransactionResponse sale = store.RegisterSale(StoreSaleRequest.Builder()
    .CustomerNumber("CUST-000123")
    .SourceTransactionNumber("POS-2026-08-28-0001")
    .TotalAmount(59.98m)
    .PurchaseTimestamp(DateTime.Now)
    .Item(StoreTransactionItem.Builder()
        .CartPosition("1")
        .Ean("5901234123457")
        .Name("Kawa ziarnista 1 kg")
        .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").ProductClass("COFFEE").Build())
        .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
        .Build())
    .Build());

Console.WriteLine($"{sale.Points} pkt, dostępne od {sale.AvailableFrom}");

PointsBalance balance = store.GetPointsBalance("CUST-000123");
```

| Metoda | Endpoint |
|---|---|
| `Info()` | `GET /api/store` |
| `RegisterSale(request)` / `RegisterSale(countryCode, request)` | `POST /api/store/transactions/sale` |
| `RegisterReturn(request)` / `RegisterReturn(countryCode, request)` | `POST /api/store/transactions/return` |
| `GetPointsBalance(customerNumber)` | `GET /api/store/customers/{customerNumber}/points` |

Nagłówek `X-CountryCode` jest doklejany automatycznie — z `DefaultCountryCode` albo
z jawnego parametru. Kod kraju jest normalizowany (trim + wielkie litery) i sprawdzany
pod kątem limitu 3 znaków, tak jak robi to backend.

Zwrot dodatkowo wymaga numeru pierwotnej sprzedaży:

```csharp
store.RegisterReturn(StoreReturnRequest.Builder()
    .CustomerNumber("CUST-000123")
    .SourceTransactionNumber("POS-2026-08-28-0002")
    .SaleTransactionNumber("POS-2026-08-28-0001")
    .TotalAmount(59.98m)
    .Item(/* ... */)
    .Build());
```

Alternatywne uwierzytelnienie (backend akceptuje oba): `.BasicAuth("kasa-01", "haslo")`
albo `.BearerToken(token)` dla tokenu zdobytego poza SDK.

---

## E-commerce — API odczytowe i kupony

Rola `ECOM`. Backend **nie wystawia endpointu logowania dla tej roli**, więc domyślną drogą
jest HTTP Basic; `BearerToken(...)` zostaje dla integracji, które zdobywają JWT własnym kanałem.

```csharp
using EcomClient ecom = EcomClient.Builder()
    .BaseUrl("http://localhost:8089")
    .BasicAuth("ecom-shop", "haslo")
    .Build();

EcomCustomerProfile profile = ecom.GetCustomerProfile("CUST-000123");
PointsBalance balance = ecom.GetPointsBalance("CUST-000123");
IReadOnlyList<CustomerTransaction> history = ecom.GetTransactions("CUST-000123");
IReadOnlyList<CustomerCoupon> coupons = ecom.GetCoupons("CUST-000123");

// Kupony korzystają z tych samych poświadczeń i tej samej puli połączeń.
CouponValidationResponse validation = ecom.Coupons().Validate("PL-ABC123", "CUST-000123");
if (validation.IsValid)
{
    decimal? discount = validation.Definition?.CouponValue;
}

CouponRedeemResponse redeemed = ecom.Coupons().RedeemPoints(
    "order-2026-08-28-0042",                       // klucz idempotentności
    CouponRedeemRequest.Builder()
        .CustomerNumber("CUST-000123")
        .CouponTemplateId(3L)
        .Build());
```

| Metoda | Endpoint |
|---|---|
| `EcomClient.Info()` | `GET /api/ecom` |
| `GetPointsBalance(cn)` | `GET /api/ecom/customers/{cn}/points` |
| `GetCustomerProfile(cn)` | `GET /api/ecom/customers/{cn}/profile` |
| `GetTransactions(cn)` | `GET /api/ecom/customers/{cn}/transactions` |
| `GetCoupons(cn)` | `GET /api/ecom/customers/{cn}/coupons` |
| `CouponClient.RedeemPoints(key, request)` | `POST /api/coupon/redeem-points` |
| `CouponClient.Validate(code, cn)` | `GET /api/coupon/validate` |

`CouponClient` da się też zbudować samodzielnie, bez `EcomClient`:

```csharp
using CouponClient coupons = CouponClient.Builder()
    .BaseUrl("http://localhost:8089")
    .BasicAuth("ecom-shop", "haslo")
    .Build();
```

### Klucz idempotentności

`RedeemPoints` wymaga jawnego klucza, bo backend realnie po nim deduplikuje: powtórzenie
z tym samym kluczem zwraca ten sam kupon, zamiast pobrać punkty drugi raz. Dlatego klucz
musi być **stabilny dla jednej próby biznesowej** — identyfikator zamówienia albo akcji
w sklepie, nie świeży `Guid` przy każdej próbie. SDK celowo go nie generuje: wygenerowany
losowo klucz zamieniłby zabezpieczenie w atrapę.

### Walidacja kuponu to nie błąd HTTP

Kupon nieważny wraca jako **HTTP 200** z werdyktem w polu `Status`
(`CouponValidationStatus`). Sprawdzaj `validation.IsValid`, nie kod odpowiedzi.

---

## Obsługa błędów

Wszystkie wyjątki dziedziczą po `LoyaltyClubException`.

| Wyjątek | Kiedy |
|---|---|
| `LoyaltyClubValidationException` | żądanie odrzucone lokalnie, **przed** wysłaniem |
| `BadRequestException` (400) | walidacja modelu **oraz** błędy biznesowe backendu |
| `UnauthorizedException` (401) | brak lub nieważne poświadczenia |
| `ForbiddenException` (403) | rola bez dostępu do namespace'u |
| `NotFoundException` (404) | np. nieznany `customerNumber` |
| `ServerException` (5xx) | błąd backendu, po wyczerpaniu ponowień |
| `LoyaltyClubTransportException` | brak połączenia, przekroczony limit czasu |
| `LoyaltyClubSerializationException` | błąd (de)serializacji JSON |

Backend zwraca RFC 7807, więc szczegóły są dostępne wprost:

```csharp
try
{
    store.RegisterSale(request);
}
catch (BadRequestException e)
{
    Console.WriteLine(e.Detail);            // "sourceTransactionNumber must be unique"
    foreach (var (field, message) in e.FieldErrors) { /* {"items": "Items are required"} */ }
    int status = e.StatusCode;
    ProblemDetail? problem = e.ProblemDetail;
}
```

> **Uwaga na kontrakt backendu:** `GlobalExceptionHandler` mapuje każdy `RuntimeException`
> na **400**, więc błędy biznesowe (przekroczona kwota zwrotu, zwrot punktów wygasłych,
> duplikat numeru transakcji) przychodzą tym samym kodem, co błędy walidacji. Rozróżniaj je
> po `FieldErrors` (puste = błąd biznesowy) i po treści `Detail`.

---

## Ponowienia

Domyślnie 3 próby, backoff 200 ms → 2 s z jitterem, dla kodów `408, 425, 429, 500, 502, 503, 504`
oraz błędów wejścia-wyjścia.

Ponawiane są **wyłącznie operacje bezpieczne do powtórzenia**:

| Operacja | Ponawiana | Dlaczego |
|---|---|---|
| wszystkie `GET` | tak | odczyt bez efektów ubocznych |
| logowanie sklepu | tak | nie zmienia stanu biznesowego |
| `RedeemPoints` | tak | chroniona nagłówkiem `Idempotency-Key` |
| `RegisterSale` / `RegisterReturn` | **nie** | przy błędzie sieci nie wiadomo, czy transakcja została zapisana |

Po `LoyaltyClubTransportException` przy sprzedaży ponów żądanie **z tym samym**
`SourceTransactionNumber` — backend wymusza jego unikalność, więc duplikat skończy się
błędem 400 zamiast podwójnym naliczeniem punktów.

Własna polityka:

```csharp
StoreClient.Builder()
    .RetryPolicy(RetryPolicy.Builder()
        .MaxAttempts(5)
        .InitialBackoff(TimeSpan.FromMilliseconds(100))
        .MaxBackoff(TimeSpan.FromSeconds(5))
        .Build())
    // RetryPolicy.None() wyłącza ponawianie całkowicie
    .Build();
```

---

## Konfiguracja klienta

Wspólne dla obu klientów:

```csharp
StoreClient.Builder()
    .BaseUrl("https://loyalty.example.com")
    .ConnectTimeout(TimeSpan.FromSeconds(5))       // domyślnie 10 s
    .RequestTimeout(TimeSpan.FromSeconds(15))      // domyślnie 30 s
    .RetryPolicy(RetryPolicy.DefaultPolicy())
    .HttpClient(wlasnyHttpClient)                  // własna pula połączeń
    .JsonOptions(wlasneOpcjeJson)                  // własna konfiguracja System.Text.Json
    .DefaultHeader("X-Correlation-Id", "...")      // nagłówek doklejany do każdego żądania
    .UserAgent("moj-system/2.1")
    .Logger(TraceLoyaltyClubLogger.Instance)       // diagnostyka do System.Diagnostics.Trace
    .Build();
```

Klient jest **bezpieczny wątkowo** — twórz go raz na aplikację i współdziel; `HttpClient`
utrzymuje wtedy pulę połączeń. `Dispose()` zamyka pulę tylko wtedy, gdy SDK samo ją utworzyło —
`HttpClient` podany przez `HttpClient(...)` zostaje nietknięty.

---

## Walidacja po stronie klienta

Zanim żądanie pójdzie w sieć, SDK sprawdza to, co i tak sprawdzi backend:

- `CustomerNumber`, `SourceTransactionNumber`, `SaleTransactionNumber` — niepuste
- `Items` — niepusta lista, każda pozycja z `Ean`, `Name`, `Hierarchy.HierarchyCode`,
  `Price.Amount` (nieujemna) i `Price.Currency`
- `TotalAmount` — dodatnia i **równa sumie cen pozycji** po zaokrągleniu do 2 miejsc
  w trybie HALF_UP (`MidpointRounding.AwayFromZero`, dokładnie ta sama normalizacja,
  co w `StoreTransactionService`)
- `countryCode` — niepusty, maks. 3 znaki
- `idempotencyKey`, `CouponTemplateId`, `couponCode` — niepuste

Naruszenie kończy się `LoyaltyClubValidationException` **bez wywołania sieciowego**.

---

## Kompatybilność w przód

- nieznane pola w odpowiedziach są ignorowane (`JsonUnmappedMemberHandling.Skip`)
- nieznane wartości enumów mapują się na `UNKNOWN` zamiast wysadzać deserializację
  (`TransactionType`, `TransactionState`, `CouponValidationStatus`) — nowy werdykt kuponu
  traktuj jak odmowę
- `DateTime` bez strefy jedzie jako ISO-8601 bez offsetu, zgodnie z domyślną konfiguracją
  Jacksona po stronie Spring Boota

---

## Mapowanie Java → C#

| Java | C# | Uwagi |
|---|---|---|
| `HttpMethod` | `ApiHttpMethod` | zmiana nazwy, żeby nie kolidować z `System.Net.Http.HttpMethod` |
| `AutoCloseable` / `close()` | `IDisposable` / `Dispose()` | |
| `Optional<String> getDetail()` | `string? Detail` | |
| `BigDecimal` | `decimal` | HALF_UP = `MidpointRounding.AwayFromZero` |
| `LocalDateTime` | `DateTime` (`Kind = Unspecified`) | |
| `Instant` | `DateTimeOffset` | |
| `TypeReference<T>` | `Execute<T>` / `ExecuteRequired<T>` | |
| Lombok `@Builder` | ręcznie pisane klasy `XxxBuilder` | `Xxx.Builder()` jako fabryka |
| `@JsonEnumDefaultValue` | `[JsonEnumDefaultValue]` + `TolerantEnumConverterFactory` | |
| `@JsonAnyGetter` / `@JsonAnySetter` | `[JsonExtensionData]` | |
| `LinkedHashMap` (nagłówki, query) | `OrderedStringMap` | zachowuje kolejność wstawiania |
| `System.Logger` | `ILoyaltyClubLogger` | domyślnie cisza, `TraceLoyaltyClubLogger` do diagnostyki |
| `com.sun.net.httpserver` w testach | `MockApiServer` na `TcpListener` | bez rezerwacji URL-a w Windows |

Świadome różnice względem wersji dla Javy:

1. `Hierarchy.hierarchy` → `Hierarchy.HierarchyCode` — w C# właściwość nie może nazywać się
   tak jak typ; na drucie pole nadal nazywa się `hierarchy` (`[JsonPropertyName]`).
2. `CouponValidationStatus.isValid()` → metoda rozszerzająca `IsValid()`; na modelu
   `CouponValidationResponse.IsValid` pozostaje właściwością.
3. Metody klientów zwracają typy nienullowalne i rzucają `LoyaltyClubSerializationException`,
   gdy backend odpowie 2xx z pustym ciałem (Java zwracała wtedy `null`).
4. `User-Agent` domyślnie `loyaltyclub-dotnet-sdk/1.0`.
5. Builder ma dodatkowe `Logger(...)` — w Javie logowanie szło przez globalny `System.Logger`.

---

## Struktura projektu

```
loaltyClub-sdk-c#/
├── LoyaltyClub.Sdk.sln
├── src/LoyaltyClub.Sdk/            biblioteka
│   ├── Core/
│   │   ├── Http/                   HttpTransport, ApiRequest, ApiHttpMethod
│   │   ├── Auth/                   Basic, Bearer, baza tokenu z auto-odświeżaniem
│   │   ├── Retry/                  RetryPolicy
│   │   ├── Exceptions/             hierarchia wyjątków
│   │   ├── Models/                 PointsBalance, ServiceInfo, ProblemDetail
│   │   ├── Json/                   LoyaltyClubJson, TolerantEnumConverter
│   │   ├── Logging/                ILoyaltyClubLogger
│   │   └── Util/                   Validate, Uris, OrderedStringMap
│   ├── Store/                      StoreClient, StoreRequestValidator, StoreJwtAuthentication, modele
│   └── Ecom/                       EcomClient, CouponClient, modele
├── tests/LoyaltyClub.Sdk.Tests/    49 testów xUnit
└── samples/LoyaltyClub.Sdk.Demo/   program konsolowy z przykładem użycia
```

## Testy

```bash
dotnet test
```

49 testów na serwerze-atrapie opartym na `TcpListener` — bez dodatkowych zależności poza
xUnit. Pokrywają transport (ponowienia, odświeżanie tokenu, mapowanie błędów, kodowanie URI),
serializację JSON oraz obie integracje. To te same przypadki, co w wersji dla Javy.
