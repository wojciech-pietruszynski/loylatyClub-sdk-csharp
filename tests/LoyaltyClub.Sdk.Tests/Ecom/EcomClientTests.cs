using System.Text.Json;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Retry;
using LoyaltyClub.Sdk.Ecom;
using LoyaltyClub.Sdk.Ecom.Models;
using LoyaltyClub.Sdk.Tests.Core;
using Xunit;

namespace LoyaltyClub.Sdk.Tests.Ecom;

public class EcomClientTests : IDisposable
{
    private readonly MockApiServer _server = MockApiServer.Start();

    public void Dispose() => _server.Dispose();

    private EcomClient Client() =>
        EcomClient.Builder()
            .BaseUrl(_server.BaseUrl())
            .BasicAuth("ecom-shop", "haslo")
            .RetryPolicy(RetryPolicy.None())
            .RequestTimeout(TimeSpan.FromSeconds(5))
            .Build();

    private static JsonElement Json(string body) => JsonDocument.Parse(body).RootElement;

    [Fact(DisplayName = "czyta profil lojalnosciowy klienta")]
    public void ReadsCustomerProfile()
    {
        _server.EnqueueJson(200,
            """
            {"customerId":7,"customerNumber":"CUST-000123","firstName":"Anna","lastName":"Kowalska",
             "email":"anna@example.com","phoneNumber":"+48123456789","country":"PL",
             "loyaltyPoints":250,"loyaltyTierCode":"SILVER","referralCode":"REF-ANNA"}
            """);

        using (EcomClient client = Client())
        {
            EcomCustomerProfile profile = client.GetCustomerProfile("CUST-000123");

            Assert.Equal("Anna", profile.FirstName);
            Assert.Equal("SILVER", profile.LoyaltyTierCode);
            Assert.Equal(250, profile.LoyaltyPoints);
        }

        MockApiServer.RecordedRequest request = _server.TakeRequest();
        Assert.Equal("GET", request.Method);
        Assert.Equal("/api/ecom/customers/CUST-000123/profile", request.Path);
        Assert.Equal("Basic ZWNvbS1zaG9wOmhhc2xv", request.Header("Authorization"));
    }

    [Fact(DisplayName = "czyta saldo punktow w tym samym ksztalcie, co API kasowe")]
    public void ReadsPointsBalance()
    {
        _server.EnqueueJson(200,
            """
            {"customerId":7,"customerNumber":"CUST-000123","pendingPoints":40,
             "availablePoints":210,"expiredPoints":5}
            """);

        using (EcomClient client = Client())
        {
            PointsBalance balance = client.GetPointsBalance("CUST-000123");

            Assert.Equal(40, balance.PendingPoints);
            Assert.Equal(210, balance.AvailablePoints);
            Assert.Equal(5, balance.ExpiredPoints);
        }

        Assert.Equal("/api/ecom/customers/CUST-000123/points", _server.TakeRequest().Path);
    }

    [Fact(DisplayName = "czyta historie punktowa jako liste")]
    public void ReadsTransactions()
    {
        _server.EnqueueJson(200,
            """
            [{"id":1,"points":59,"description":"Zakup POS-2026-0001","timestamp":"2026-08-28T12:00:00",
              "availableFrom":"2026-09-11T12:00:00"},
             {"id":2,"points":-59,"description":"Zwrot POS-2026-0002","timestamp":"2026-08-29T09:30:00"}]
            """);

        using (EcomClient client = Client())
        {
            IReadOnlyList<CustomerTransaction> transactions = client.GetTransactions("CUST-000123");

            Assert.Equal(2, transactions.Count);
            Assert.Equal(59, transactions[0].Points);
            Assert.Equal(new DateTime(2026, 9, 11, 12, 0, 0), transactions[0].AvailableFrom);
            Assert.Equal(-59, transactions[1].Points);
            Assert.Null(transactions[1].AvailableFrom);
        }

        Assert.Equal("/api/ecom/customers/CUST-000123/transactions", _server.TakeRequest().Path);
    }

    [Fact(DisplayName = "czyta kupony klienta")]
    public void ReadsCoupons()
    {
        _server.EnqueueJson(200,
            """
            [{"id":11,"couponCode":"PL-ABC123","customerId":7,"couponValue":20.00,
              "minimumPurchaseValue":100.00,"requiredPoints":200,"validityDays":30,
              "couponPrefix":"PL","status":"ACTIVE","issuedAt":"2026-08-28T12:00:00",
              "expiresAt":"2026-09-27T12:00:00"}]
            """);

        using EcomClient client = Client();
        IReadOnlyList<CustomerCoupon> coupons = client.GetCoupons("CUST-000123");

        CustomerCoupon coupon = Assert.Single(coupons);
        Assert.Equal("PL-ABC123", coupon.CouponCode);
        Assert.Equal(20.00m, coupon.CouponValue);
        Assert.Equal("ACTIVE", coupon.Status);
    }

    [Fact(DisplayName = "koduje numer klienta w sciezce")]
    public void EncodesCustomerNumberInPath()
    {
        _server.EnqueueJson(200, "{}");

        using (EcomClient client = Client())
        {
            client.GetPointsBalance("CUST/000 123");
        }

        Assert.Equal("/api/ecom/customers/CUST%2F000%20123/points", _server.TakeRequest().Path);
    }

    [Fact(DisplayName = "pusty numer klienta jest odrzucany lokalnie, bez wywolania sieciowego")]
    public void RejectsBlankCustomerNumber()
    {
        using (EcomClient client = Client())
        {
            Assert.Throws<LoyaltyClubValidationException>(() => client.GetCustomerProfile("  "));
        }

        Assert.Equal(0, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "konto bez roli ECOM konczy sie ForbiddenException")]
    public void MapsWrongRoleToForbidden()
    {
        _server.EnqueueProblem(403, """{"status":403,"detail":"Forbidden"}""");

        using EcomClient client = Client();
        Assert.Throws<ForbiddenException>(() => client.GetCustomerProfile("CUST-1"));
    }

    [Fact(DisplayName = "klient kuponowy wspoldzieli poswiadczenia i wysyla naglowek Idempotency-Key")]
    public void CouponClientSharesCredentialsAndSendsIdempotencyKey()
    {
        _server.EnqueueJson(200,
            """
            {"couponCode":"PL-ABC123","customerNumber":"CUST-000123","status":"ACTIVE",
             "issuedAt":"2026-08-28T12:00:00","expiresAt":"2026-09-27T12:00:00",
             "definition":{"couponTemplateId":3,"couponValue":20.00,"minimumPurchaseValue":100.00,
                           "requiredPoints":200,"validityDays":30,"couponPrefix":"PL","country":"PL"}}
            """);

        using (EcomClient client = Client())
        {
            CouponRedeemResponse response = client.Coupons().RedeemPoints("order-2026-0001",
                CouponRedeemRequest.Builder()
                    .CustomerNumber("CUST-000123")
                    .CouponTemplateId(3L)
                    .Build());

            Assert.Equal("PL-ABC123", response.CouponCode);
            Assert.Equal(200, response.Definition!.RequiredPoints);
        }

        MockApiServer.RecordedRequest request = _server.TakeRequest();
        Assert.Equal("POST", request.Method);
        Assert.Equal("/api/coupon/redeem-points", request.Path);
        Assert.Equal("order-2026-0001", request.Header("Idempotency-Key"));
        Assert.Equal("Basic ZWNvbS1zaG9wOmhhc2xv", request.Header("Authorization"));

        JsonElement body = Json(request.Body);
        Assert.Equal("CUST-000123", body.GetProperty("customerNumber").GetString());
        Assert.Equal(3, body.GetProperty("couponTemplateId").GetInt32());
    }

    [Fact(DisplayName = "brak klucza idempotentnosci jest wychwytywany lokalnie")]
    public void RequiresIdempotencyKey()
    {
        using (EcomClient client = Client())
        {
            CouponRedeemRequest request = CouponRedeemRequest.Builder()
                .CustomerNumber("CUST-000123")
                .CouponTemplateId(3L)
                .Build();

            Assert.Throws<LoyaltyClubValidationException>(() => client.Coupons().RedeemPoints("  ", request));
        }

        Assert.Equal(0, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "walidacja kuponu przekazuje parametry zapytania i zwraca werdykt")]
    public void ValidatesCoupon()
    {
        _server.EnqueueJson(200,
            """
            {"status":"VALID","couponCode":"PL-ABC123","customerNumber":"CUST-000123",
             "couponStatus":"ACTIVE","issuedAt":"2026-08-28T12:00:00","expiresAt":"2026-09-27T12:00:00",
             "definition":{"couponTemplateId":3,"couponValue":20.00,"minimumPurchaseValue":100.00}}
            """);

        using (EcomClient client = Client())
        {
            CouponValidationResponse validation = client.Coupons().Validate("PL-ABC123", "CUST-000123");

            Assert.True(validation.IsValid);
            Assert.Equal(CouponValidationStatus.VALID, validation.Status);
            Assert.Equal(100.00m, validation.Definition!.MinimumPurchaseValue);
        }

        MockApiServer.RecordedRequest request = _server.TakeRequest();
        Assert.Equal("/api/coupon/validate", request.Path);
        Assert.Equal("couponCode=PL-ABC123&customerNumber=CUST-000123", request.Query);
    }

    [Fact(DisplayName = "kupon odrzucony wraca jako status 200 z werdyktem, nie jako blad HTTP")]
    public void InvalidCouponIsNotAnHttpError()
    {
        _server.EnqueueJson(200,
            """
            {"status":"COUPON_ALREADY_USED","couponCode":"PL-ABC123","customerNumber":"CUST-000123",
             "couponStatus":"USED"}
            """);

        using EcomClient client = Client();
        CouponValidationResponse validation = client.Coupons().Validate("PL-ABC123", "CUST-000123");

        Assert.False(validation.IsValid);
        Assert.Equal(CouponValidationStatus.COUPON_ALREADY_USED, validation.Status);
    }

    [Fact(DisplayName = "nieznany werdykt z nowszego backendu jest traktowany jak odmowa")]
    public void UnknownValidationStatusIsNotValid()
    {
        _server.EnqueueJson(200, """{"status":"COUPON_BLOCKED_BY_FRAUD_CHECK","couponCode":"PL-ABC123"}""");

        using EcomClient client = Client();
        CouponValidationResponse validation = client.Coupons().Validate("PL-ABC123", "CUST-000123");

        Assert.Equal(CouponValidationStatus.UNKNOWN, validation.Status);
        Assert.False(validation.IsValid);
    }

    [Fact(DisplayName = "samodzielny CouponClient dziala bez klienta e-commerce")]
    public void StandaloneCouponClient()
    {
        _server.EnqueueJson(200, """{"status":"VALID","couponCode":"PL-ABC123"}""");

        using CouponClient coupons = CouponClient.Builder()
            .BaseUrl(_server.BaseUrl())
            .BasicAuth("ecom-shop", "haslo")
            .Build();

        Assert.True(coupons.Validate("PL-ABC123", "CUST-000123").IsValid);
    }

    [Fact(DisplayName = "builder bez poswiadczen zglasza czytelny blad")]
    public void BuilderRequiresCredentials()
    {
        LoyaltyClubValidationException exception = Assert.Throws<LoyaltyClubValidationException>(
            () => EcomClient.Builder().BaseUrl(_server.BaseUrl()).Build());

        Assert.Contains("basicAuth", exception.Message);
    }

    [Fact(DisplayName = "Info() czyta wersje API z metadanych integracji")]
    public void ReadsServiceInfo()
    {
        _server.EnqueueJson(200,
            """{"name":"ecom","status":"ready","apiVersion":"1.0.0","docs":"Use GET /api/ecom/..."}""");

        using EcomClient client = Client();
        Assert.Equal("1.0.0", client.Info().ApiVersion);
    }
}
