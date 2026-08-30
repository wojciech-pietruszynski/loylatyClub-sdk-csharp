using System.Text.Json;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Retry;
using LoyaltyClub.Sdk.Store;
using LoyaltyClub.Sdk.Store.Models;
using LoyaltyClub.Sdk.Tests.Core;
using Xunit;

namespace LoyaltyClub.Sdk.Tests.Store;

public class StoreClientTests : IDisposable
{
    private readonly MockApiServer _server = MockApiServer.Start();

    public void Dispose() => _server.Dispose();

    private StoreClient ClientWithJwtLogin() =>
        StoreClient.Builder()
            .BaseUrl(_server.BaseUrl())
            .Credentials("kasa-01", "haslo")
            .DefaultCountryCode("pl")
            .RetryPolicy(RetryPolicy.None())
            .RequestTimeout(TimeSpan.FromSeconds(5))
            .Build();

    private void EnqueueLogin()
    {
        long expiresAt = DateTimeOffset.UtcNow.AddSeconds(900).ToUnixTimeMilliseconds();
        _server.EnqueueJson(200,
            $$"""{"token":"jwt-store-token","expiresAt":{{expiresAt}},"role":"STORE","country":null}""");
    }

    private static StoreSaleRequest SampleSale() =>
        StoreSaleRequest.Builder()
            .CustomerNumber("CUST-000123")
            .SourceTransactionNumber("POS-2026-0001")
            .TotalAmount(59.98m)
            .PurchaseTimestamp(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Unspecified))
            .Item(StoreTransactionItem.Builder()
                .CartPosition("1")
                .Ean("5901234123457")
                .Name("Kawa ziarnista 1 kg")
                .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").ProductClass("COFFEE").Build())
                .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
                .Build())
            .Build();

    private static JsonElement Json(string body) => JsonDocument.Parse(body).RootElement;

    [Fact(DisplayName = "loguje sie raz i uzywa tokenu Bearer w kolejnych wywolaniach")]
    public void LogsInOnceAndReusesToken()
    {
        EnqueueLogin();
        _server.EnqueueJson(200,
            """{"customerNumber":"CUST-000123","pendingPoints":10,"availablePoints":50,"expiredPoints":1}""");
        _server.EnqueueJson(200,
            """{"customerNumber":"CUST-000123","pendingPoints":10,"availablePoints":50,"expiredPoints":1}""");

        using (StoreClient client = ClientWithJwtLogin())
        {
            client.GetPointsBalance("CUST-000123");
            PointsBalance balance = client.GetPointsBalance("CUST-000123");
            Assert.Equal(50, balance.AvailablePoints);
        }

        MockApiServer.RecordedRequest login = _server.TakeRequest();
        Assert.Equal("POST", login.Method);
        Assert.Equal("/api/store/auth/login", login.Path);
        Assert.Contains("\"username\":\"kasa-01\"", login.Body);

        Assert.Equal("Bearer jwt-store-token", _server.TakeRequest().Header("Authorization"));
        Assert.Equal("Bearer jwt-store-token", _server.TakeRequest().Header("Authorization"));
        // Trzy zadania w sumie: jedno logowanie i dwa odczyty salda — token nie byl pobierany ponownie.
        Assert.Equal(0, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "po HTTP 401 loguje sie ponownie i powtarza wywolanie")]
    public void ReLoginsAfterUnauthorized()
    {
        EnqueueLogin();
        _server.EnqueueEmpty(401);
        long expiresAt = DateTimeOffset.UtcNow.AddSeconds(900).ToUnixTimeMilliseconds();
        _server.EnqueueJson(200, $$"""{"token":"jwt-swiezy","expiresAt":{{expiresAt}},"role":"STORE"}""");
        _server.EnqueueJson(200, """{"customerNumber":"CUST-000123","availablePoints":7}""");

        using (StoreClient client = ClientWithJwtLogin())
        {
            Assert.Equal(7, client.GetPointsBalance("CUST-000123").AvailablePoints);
        }

        _server.TakeRequest();
        Assert.Equal("Bearer jwt-store-token", _server.TakeRequest().Header("Authorization"));
        Assert.Equal("/api/store/auth/login", _server.TakeRequest().Path);
        Assert.Equal("Bearer jwt-swiezy", _server.TakeRequest().Header("Authorization"));
    }

    [Fact(DisplayName = "rejestracja sprzedazy wysyla naglowek X-CountryCode i cialo zgodne z kontraktem backendu")]
    public void RegisterSaleSendsCountryHeaderAndBody()
    {
        EnqueueLogin();
        _server.EnqueueJson(200,
            """
            {"transactionId":1001,"customerId":7,"customerNumber":"CUST-000123","type":"SALE",
             "state":"PENDING","points":59,"amount":59.98,"pointsPerCurrency":1.00,
             "purchaseTimestamp":"2026-08-28T12:00:00","availableFrom":"2026-09-11T12:00:00",
             "expiresAt":"2027-08-28T12:00:00"}
            """);

        using (StoreClient client = ClientWithJwtLogin())
        {
            StoreTransactionResponse response = client.RegisterSale(SampleSale());

            Assert.Equal(1001L, response.TransactionId);
            Assert.Equal(TransactionType.SALE, response.Type);
            Assert.Equal(TransactionState.PENDING, response.State);
            Assert.Equal(59, response.Points);
            Assert.Equal(new DateTime(2026, 9, 11, 12, 0, 0), response.AvailableFrom);
        }

        _server.TakeRequest();
        MockApiServer.RecordedRequest sale = _server.TakeRequest();
        Assert.Equal("POST", sale.Method);
        Assert.Equal("/api/store/transactions/sale", sale.Path);
        // Builder dostal "pl" — backend oczekuje wielkich liter, wiec SDK normalizuje kod kraju.
        Assert.Equal("PL", sale.Header("X-CountryCode"));
        Assert.Equal("application/json", sale.Header("Content-Type"));

        JsonElement body = Json(sale.Body);
        Assert.Equal("CUST-000123", body.GetProperty("customerNumber").GetString());
        Assert.Equal("POS-2026-0001", body.GetProperty("sourceTransactionNumber").GetString());
        Assert.Equal("59.98", body.GetProperty("totalAmount").GetRawText());
        Assert.Equal("2026-08-28T12:00:00", body.GetProperty("purchaseTimestamp").GetString());

        JsonElement firstItem = body.GetProperty("items")[0];
        Assert.Equal("5901234123457", firstItem.GetProperty("ean").GetString());
        Assert.Equal("FOOD", firstItem.GetProperty("hierarchy").GetProperty("hierarchy").GetString());
        Assert.Equal("PLN", firstItem.GetProperty("price").GetProperty("currency").GetString());
    }

    [Fact(DisplayName = "rejestracja zwrotu wysyla numer transakcji sprzedazy")]
    public void RegisterReturnSendsSaleTransactionNumber()
    {
        EnqueueLogin();
        _server.EnqueueJson(200, """{"transactionId":1002,"type":"RETURN","state":"AVAILABLE","points":-59}""");

        using (StoreClient client = ClientWithJwtLogin())
        {
            StoreTransactionResponse response = client.RegisterReturn("DE", StoreReturnRequest.Builder()
                .CustomerNumber("CUST-000123")
                .SourceTransactionNumber("POS-2026-0002")
                .SaleTransactionNumber("POS-2026-0001")
                .TotalAmount(59.98m)
                .Item(StoreTransactionItem.Builder()
                    .Ean("5901234123457")
                    .Name("Kawa ziarnista 1 kg")
                    .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").Build())
                    .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
                    .Build())
                .Build());

            Assert.Equal(TransactionType.RETURN, response.Type);
            Assert.Equal(-59, response.Points);
        }

        _server.TakeRequest();
        MockApiServer.RecordedRequest returnRequest = _server.TakeRequest();
        Assert.Equal("/api/store/transactions/return", returnRequest.Path);
        Assert.Equal("DE", returnRequest.Header("X-CountryCode"));
        Assert.Equal("POS-2026-0001", Json(returnRequest.Body).GetProperty("saleTransactionNumber").GetString());
    }

    [Fact(DisplayName = "nieznana wartosc enuma z nowszego backendu nie wysadza deserializacji")]
    public void UnknownEnumValueFallsBackToUnknown()
    {
        EnqueueLogin();
        _server.EnqueueJson(200, """{"transactionId":1003,"type":"LOYALTY_BONUS","state":"FROZEN"}""");

        using StoreClient client = ClientWithJwtLogin();
        StoreTransactionResponse response = client.RegisterSale(SampleSale());

        Assert.Equal(TransactionType.UNKNOWN, response.Type);
        Assert.Equal(TransactionState.UNKNOWN, response.State);
    }

    [Fact(DisplayName = "odrzuca lokalnie paragon, w ktorym kwota nie zgadza sie z suma pozycji")]
    public void RejectsTotalAmountMismatchLocally()
    {
        using (StoreClient client = ClientWithJwtLogin())
        {
            StoreSaleRequest request = StoreSaleRequest.Builder()
                .CustomerNumber("CUST-000123")
                .SourceTransactionNumber("POS-2026-0003")
                .TotalAmount(100.00m)
                .Item(StoreTransactionItem.Builder()
                    .Ean("5901234123457")
                    .Name("Kawa")
                    .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").Build())
                    .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
                    .Build())
                .Build();

            LoyaltyClubValidationException exception =
                Assert.Throws<LoyaltyClubValidationException>(() => client.RegisterSale(request));
            Assert.Contains("totalAmount", exception.Message);
        }

        // Zadne zadanie nie poszlo na serwer — nawet logowanie.
        Assert.Equal(0, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "odrzuca lokalnie paragon bez pozycji i bez numeru transakcji zrodlowej")]
    public void RejectsIncompleteRequestsLocally()
    {
        using (StoreClient client = ClientWithJwtLogin())
        {
            Assert.Throws<LoyaltyClubValidationException>(() => client.RegisterSale(
                StoreSaleRequest.Builder()
                    .CustomerNumber("CUST-1")
                    .SourceTransactionNumber("POS-1")
                    .TotalAmount(10.00m)
                    .Build()));

            Assert.Throws<LoyaltyClubValidationException>(() => client.RegisterSale(
                StoreSaleRequest.Builder()
                    .CustomerNumber("CUST-1")
                    .TotalAmount(59.98m)
                    .Item(StoreTransactionItem.Builder()
                        .Ean("5901234123457")
                        .Name("Kawa")
                        .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").Build())
                        .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
                        .Build())
                    .Build()));
        }

        Assert.Equal(0, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "odrzuca kod kraju dluzszy niz trzy znaki, tak jak zrobilby to backend")]
    public void RejectsTooLongCountryCode()
    {
        using StoreClient client = ClientWithJwtLogin();
        Assert.Throws<LoyaltyClubValidationException>(() => client.RegisterSale("POLSKA", SampleSale()));
    }

    [Fact(DisplayName = "bez domyslnego kodu kraju wariant jednoargumentowy zglasza czytelny blad")]
    public void RequiresCountryCodeWhenDefaultMissing()
    {
        using StoreClient client = StoreClient.Builder()
            .BaseUrl(_server.BaseUrl())
            .BasicAuth("kasa-01", "haslo")
            .Build();

        LoyaltyClubValidationException exception =
            Assert.Throws<LoyaltyClubValidationException>(() => client.RegisterSale(SampleSale()));
        Assert.Contains("defaultCountryCode", exception.Message);
    }

    [Fact(DisplayName = "wariant Basic nie wywoluje logowania i wysyla naglowek Authorization")]
    public void BasicAuthSkipsLogin()
    {
        _server.EnqueueJson(200, """{"customerNumber":"CUST-1","availablePoints":3}""");

        using (StoreClient client = StoreClient.Builder()
                   .BaseUrl(_server.BaseUrl())
                   .BasicAuth("kasa-01", "haslo")
                   .Build())
        {
            Assert.Equal(3, client.GetPointsBalance("CUST-1").AvailablePoints);
        }

        MockApiServer.RecordedRequest request = _server.TakeRequest();
        Assert.Equal("/api/store/customers/CUST-1/points", request.Path);
        Assert.StartsWith("Basic ", request.Header("Authorization"));
    }

    [Fact(DisplayName = "nieznany klient konczy sie NotFoundException z komunikatem backendu")]
    public void MapsUnknownCustomerToNotFound()
    {
        EnqueueLogin();
        _server.EnqueueProblem(404, """{"status":404,"detail":"Customer not found for customerNumber: CUST-999"}""");

        using StoreClient client = ClientWithJwtLogin();
        NotFoundException exception =
            Assert.Throws<NotFoundException>(() => client.GetPointsBalance("CUST-999"));
        Assert.Contains("CUST-999", exception.Detail!);
    }

    [Fact(DisplayName = "duplikat numeru transakcji zrodlowej wraca jako BadRequestException")]
    public void MapsDuplicateSourceTransactionNumber()
    {
        EnqueueLogin();
        _server.EnqueueProblem(400, """{"status":400,"detail":"sourceTransactionNumber must be unique"}""");

        using StoreClient client = ClientWithJwtLogin();
        BadRequestException exception =
            Assert.Throws<BadRequestException>(() => client.RegisterSale(SampleSale()));
        Assert.Equal("sourceTransactionNumber must be unique", exception.Detail);
    }

    [Fact(DisplayName = "Info() czyta metadane integracji")]
    public void ReadsServiceInfo()
    {
        EnqueueLogin();
        _server.EnqueueJson(200, """{"name":"store","status":"ready"}""");

        using StoreClient client = ClientWithJwtLogin();
        Assert.Equal("store", client.Info().Name);
    }
}
