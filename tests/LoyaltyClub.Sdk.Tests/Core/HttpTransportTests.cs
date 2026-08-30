using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Retry;
using Xunit;

namespace LoyaltyClub.Sdk.Tests.Core;

public class HttpTransportTests : IDisposable
{
    private readonly MockApiServer _server = MockApiServer.Start();

    public void Dispose() => _server.Dispose();

    private HttpTransport Transport(RetryPolicy retryPolicy, IAuthenticationProvider? authentication) =>
        HttpTransport.Builder()
            .BaseUrl(_server.BaseUrl())
            .RetryPolicy(retryPolicy)
            .Authentication(authentication)
            .RequestTimeout(TimeSpan.FromSeconds(5))
            .Build();

    private static ApiRequest Get(bool retryable) =>
        ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path("/api/store/customers/CUST-1/points")
            .Retryable(retryable)
            .Build();

    [Fact(DisplayName = "deserializuje odpowiedz i dokleja naglowki Accept oraz Authorization")]
    public void DeserializesResponseAndSendsHeaders()
    {
        _server.EnqueueJson(200,
            """{"customerId":7,"customerNumber":"CUST-1","pendingPoints":10,"availablePoints":90,"expiredPoints":0}""");

        using (HttpTransport transport = Transport(RetryPolicy.None(), new BasicAuthentication("ecom", "secret")))
        {
            PointsBalance? balance = transport.Execute<PointsBalance>(Get(true));

            Assert.NotNull(balance);
            Assert.Equal(7L, balance!.CustomerId);
            Assert.Equal(90, balance.AvailablePoints);
        }

        MockApiServer.RecordedRequest request = _server.TakeRequest();
        Assert.Equal("GET", request.Method);
        Assert.Equal("/api/store/customers/CUST-1/points", request.Path);
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("Basic ZWNvbTpzZWNyZXQ=", request.Header("Authorization"));
    }

    [Fact(DisplayName = "koduje parametry zapytania, zeby znaki specjalne nie rozjechaly routingu")]
    public void EncodesQueryParameters()
    {
        _server.EnqueueJson(200, "{}");

        using (HttpTransport transport = Transport(RetryPolicy.None(), null))
        {
            transport.Execute(ApiRequest.Builder()
                .Method(ApiHttpMethod.Get)
                .Path("/api/coupon/validate")
                .QueryParam("couponCode", "PL ABC/123")
                .QueryParam("customerNumber", "CUST-1")
                .Build());
        }

        Assert.Equal("couponCode=PL%20ABC%2F123&customerNumber=CUST-1", _server.TakeRequest().Query);
    }

    [Fact(DisplayName = "ponawia zadanie oznaczone jako bezpieczne po HTTP 503")]
    public void RetriesRetryableRequestOnServerError()
    {
        _server.EnqueueEmpty(503);
        _server.EnqueueJson(200, """{"customerNumber":"CUST-1","availablePoints":5}""");

        using (HttpTransport transport = Transport(FastRetry(3), null))
        {
            PointsBalance? balance = transport.Execute<PointsBalance>(Get(true));
            Assert.Equal(5, balance!.AvailablePoints);
        }

        Assert.Equal(2, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "nie ponawia zadania nieidempotentnego — rejestracja sprzedazy leci raz")]
    public void DoesNotRetryNonRetryableRequest()
    {
        _server.EnqueueEmpty(503);
        _server.EnqueueJson(200, "{}");

        using (HttpTransport transport = Transport(FastRetry(3), null))
        {
            Assert.Throws<ServerException>(() => transport.Execute<PointsBalance>(Get(false)));
        }

        Assert.Equal(1, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "konczy sie bledem po wyczerpaniu prob")]
    public void FailsAfterExhaustingAttempts()
    {
        _server.EnqueueEmpty(503).EnqueueEmpty(503).EnqueueEmpty(503);

        using (HttpTransport transport = Transport(FastRetry(3), null))
        {
            ServerException exception =
                Assert.Throws<ServerException>(() => transport.Execute<PointsBalance>(Get(true)));
            Assert.Equal(503, exception.StatusCode);
        }

        Assert.Equal(3, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "po HTTP 401 odswieza poswiadczenia i ponawia raz, nie zuzywajac puli retry")]
    public void RefreshesCredentialsOnUnauthorized()
    {
        _server.EnqueueEmpty(401);
        _server.EnqueueJson(200, """{"customerNumber":"CUST-1","availablePoints":42}""");

        RotatingAuthentication authentication = new RotatingAuthentication();

        // MaxAttempts=1 wylacza zwykle ponowienia — powtorzenie moze wynikac tylko z odswiezenia tokenu.
        using (HttpTransport transport = Transport(RetryPolicy.None(), authentication))
        {
            Assert.Equal(42, transport.Execute<PointsBalance>(Get(false))!.AvailablePoints);
        }

        Assert.Equal(1, authentication.RefreshCount);
        Assert.Equal("Bearer token-1", _server.TakeRequest().Header("Authorization"));
        Assert.Equal("Bearer token-2", _server.TakeRequest().Header("Authorization"));
    }

    [Fact(DisplayName = "nie wpada w petle, gdy odswiezone poswiadczenia dalej daja 401")]
    public void DoesNotLoopWhenRefreshedCredentialsStillFail()
    {
        _server.EnqueueEmpty(401).EnqueueEmpty(401);

        using (HttpTransport transport = Transport(RetryPolicy.None(), new StaleAuthentication()))
        {
            Assert.Throws<UnauthorizedException>(() => transport.Execute<PointsBalance>(Get(false)));
        }

        Assert.Equal(2, _server.ReceivedRequestCount);
    }

    [Fact(DisplayName = "mapuje ProblemDetail walidacji na BadRequestException z bledami pol")]
    public void MapsValidationProblemDetail()
    {
        _server.EnqueueProblem(400,
            """
            {"type":"about:blank","title":"Bad Request","status":400,"detail":"Validation failed",
             "errors":{"customerNumber":"Customer number is required"}}
            """);

        using HttpTransport transport = Transport(RetryPolicy.None(), null);
        BadRequestException exception =
            Assert.Throws<BadRequestException>(() => transport.Execute<PointsBalance>(Get(false)));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Validation failed", exception.Detail);
        Assert.Equal("Customer number is required", exception.FieldErrors["customerNumber"]);
        Assert.Contains("customerNumber", exception.Message);
    }

    [Fact(DisplayName = "mapuje bledy biznesowe 400 na komunikat z pola detail")]
    public void MapsBusinessProblemDetail()
    {
        _server.EnqueueProblem(400, """{"status":400,"detail":"sourceTransactionNumber must be unique"}""");

        using HttpTransport transport = Transport(RetryPolicy.None(), null);
        BadRequestException exception =
            Assert.Throws<BadRequestException>(() => transport.Execute<PointsBalance>(Get(false)));

        Assert.Equal("sourceTransactionNumber must be unique", exception.Detail);
        Assert.Empty(exception.FieldErrors);
    }

    [Fact(DisplayName = "mapuje 403 i 404 na dedykowane wyjatki")]
    public void MapsForbiddenAndNotFound()
    {
        _server.EnqueueProblem(403, """{"status":403,"detail":"Forbidden"}""");
        _server.EnqueueProblem(404, """{"status":404,"detail":"Customer not found for customerNumber: CUST-1"}""");

        using HttpTransport transport = Transport(RetryPolicy.None(), null);
        Assert.Throws<ForbiddenException>(() => transport.Execute<PointsBalance>(Get(false)));

        NotFoundException notFound =
            Assert.Throws<NotFoundException>(() => transport.Execute<PointsBalance>(Get(false)));
        Assert.Contains("CUST-1", notFound.Detail!);
    }

    [Fact(DisplayName = "radzi sobie z odpowiedzia bledu, ktora nie jest dokumentem ProblemDetail")]
    public void HandlesNonProblemErrorBody()
    {
        _server.EnqueueJson(401, "<html>401</html>");

        using HttpTransport transport = Transport(RetryPolicy.None(), null);
        UnauthorizedException exception =
            Assert.Throws<UnauthorizedException>(() => transport.Execute<PointsBalance>(Get(false)));

        Assert.Null(exception.Detail);
        Assert.Equal("<html>401</html>", exception.RawBody);
    }

    [Fact(DisplayName = "ucina koncowy ukosnik w adresie bazowym, zeby sciezka nie zdublowala separatora")]
    public void NormalizesBaseUrl()
    {
        _server.EnqueueJson(200, "{}");

        using (HttpTransport transport = HttpTransport.Builder()
                   .BaseUrl(_server.BaseUrl() + "/")
                   .RetryPolicy(RetryPolicy.None())
                   .Build())
        {
            transport.Execute<PointsBalance>(Get(false));
        }

        Assert.Equal("/api/store/customers/CUST-1/points", _server.TakeRequest().Path);
    }

    private static RetryPolicy FastRetry(int maxAttempts) =>
        RetryPolicy.Builder()
            .MaxAttempts(maxAttempts)
            .InitialBackoff(TimeSpan.FromMilliseconds(1))
            .MaxBackoff(TimeSpan.FromMilliseconds(5))
            .Build();

    /// <summary>Poswiadczenia zmieniajace token po kazdym odswiezeniu.</summary>
    private sealed class RotatingAuthentication : IAuthenticationProvider
    {
        private int _tokenVersion = 1;

        internal int RefreshCount { get; private set; }

        public void Authorize(HttpRequestMessage request)
        {
            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer token-" + _tokenVersion);
        }

        public bool RefreshAfterUnauthorized()
        {
            RefreshCount++;
            _tokenVersion++;
            return true;
        }
    }

    /// <summary>Poswiadczenia, ktore po odswiezeniu dalej sa nieaktualne.</summary>
    private sealed class StaleAuthentication : IAuthenticationProvider
    {
        public void Authorize(HttpRequestMessage request)
        {
            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer stale");
        }

        public bool RefreshAfterUnauthorized() => true;
    }
}
