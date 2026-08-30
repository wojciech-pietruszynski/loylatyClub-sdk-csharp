using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Json;
using LoyaltyClub.Sdk.Core.Logging;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Retry;
using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Http;

/// <summary>
/// Warstwa transportowa SDK: budowa zadania, serializacja JSON, uwierzytelnianie,
/// ponowienia i tlumaczenie odpowiedzi bledow na wyjatki.
/// <para>
/// Instancja jest bezpieczna watkowo i przeznaczona do wspoldzielenia — opakowany
/// <see cref="System.Net.Http.HttpClient"/> utrzymuje pule polaczen, wiec tworzenie
/// transportu na kazde zadanie niweczyloby keep-alive.
/// </para>
/// </summary>
public class HttpTransport : IDisposable
{
    internal static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);
    internal const string DefaultUserAgent = "loyaltyclub-dotnet-sdk/1.0";

    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly TimeSpan _requestTimeout;
    private readonly IAuthenticationProvider _authentication;
    private readonly IReadOnlyDictionary<string, string> _defaultHeaders;
    private readonly string _userAgent;
    private readonly ILoyaltyClubLogger _logger;
    private bool _disposed;

    internal HttpTransport(HttpTransportBuilder builder)
    {
        BaseUrl = NormalizeBaseUrl(Validate.RequireText(builder.BaseUrlValue, "baseUrl"));
        JsonOptions = builder.JsonOptionsValue ?? LoyaltyClubJson.CreateDefault();
        _requestTimeout = builder.RequestTimeoutValue ?? DefaultRequestTimeout;
        RetryPolicy = builder.RetryPolicyValue ?? Retry.RetryPolicy.DefaultPolicy();
        _authentication = builder.AuthenticationValue ?? NoAuthentication.Instance;
        _defaultHeaders = new OrderedStringMap(builder.DefaultHeadersValue);
        _userAgent = builder.UserAgentValue ?? DefaultUserAgent;
        _logger = builder.LoggerValue ?? NullLoyaltyClubLogger.Instance;
        _ownsHttpClient = builder.HttpClientValue == null;
        _httpClient = builder.HttpClientValue ?? CreateHttpClient(builder.ConnectTimeoutValue ?? DefaultConnectTimeout);
    }

    /// <summary>Bazowy adres API bez konczacego ukosnika, np. <c>http://localhost:8089</c>.</summary>
    public string BaseUrl { get; }

    /// <summary>Bazowy adres API jako <see cref="Uri"/>.</summary>
    public Uri BaseUri => new Uri(BaseUrl, UriKind.Absolute);

    /// <summary>Konfiguracja serializacji uzywana dla cial zadan i odpowiedzi.</summary>
    public JsonSerializerOptions JsonOptions { get; }

    /// <summary>Polityka ponowien stosowana dla zadan oznaczonych jako powtarzalne.</summary>
    public RetryPolicy RetryPolicy { get; }

    public static HttpTransportBuilder Builder() => new HttpTransportBuilder();

    /// <summary>
    /// Zwraca transport wykonujacy zadania bez uwierzytelniania — uzywany dla wywolania
    /// logowania, ktore poswiadczen w naglowku nie potrzebuje. Wspoldzieli pule polaczen
    /// i konfiguracje JSON z oryginalem.
    /// </summary>
    public HttpTransport WithoutAuthentication() =>
        Builder()
            .BaseUrl(BaseUrl)
            .HttpClient(_httpClient)
            .JsonOptions(JsonOptions)
            .RequestTimeout(_requestTimeout)
            .RetryPolicy(RetryPolicy)
            .Authentication(NoAuthentication.Instance)
            .DefaultHeaders(_defaultHeaders)
            .UserAgent(_userAgent)
            .Logger(_logger)
            .Build();

    /// <summary>
    /// Wykonuje zadanie i deserializuje odpowiedz do wskazanego typu. Puste cialo odpowiedzi
    /// daje wartosc domyslna typu — odpowiednik <c>null</c> z wersji dla Javy.
    /// </summary>
    public T? Execute<T>(ApiRequest request)
    {
        TransportResponse response = Send(request);
        string? body = response.Body;
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (Exception e) when (e is JsonException || e is NotSupportedException)
        {
            throw new LoyaltyClubSerializationException(
                "Nie udalo sie zdeserializowac odpowiedzi z " + request.Path, e);
        }
    }

    /// <summary>
    /// Jak <see cref="Execute{T}(ApiRequest)"/>, ale wymaga ciala odpowiedzi. Uzywane tam, gdzie
    /// kontrakt backendu gwarantuje dokument, a jego brak jest bledem protokolu, nie stanem biznesowym.
    /// </summary>
    public T ExecuteRequired<T>(ApiRequest request)
    {
        T? value = Execute<T>(request);
        if (value is null)
        {
            throw new LoyaltyClubSerializationException(
                "Odpowiedz z " + request.Path + " nie zawiera oczekiwanego dokumentu JSON");
        }

        return value;
    }

    /// <summary>Wykonuje zadanie i ignoruje cialo odpowiedzi.</summary>
    public void Execute(ApiRequest request) => Send(request);

    private TransportResponse Send(ApiRequest request)
    {
        Validate.RequireNonNull(request, "request");
        byte[]? payload = SerializeBody(request);

        int attempt = 1;
        bool authRefreshed = false;

        while (true)
        {
            TransportResponse response;
            try
            {
                response = SendOnce(request, payload);
            }
            catch (Exception e) when (IsTransportFailure(e))
            {
                if (RetryPolicy.RetryOnIoException && CanRetry(request, attempt))
                {
                    SleepBeforeRetry(attempt, request, e.GetType().Name + ": " + e.Message);
                    attempt++;
                    continue;
                }

                throw new LoyaltyClubTransportException(
                    "Wywolanie " + request.Method.Name() + " " + request.Path + " nie powiodlo sie", e);
            }

            int status = response.StatusCode;

            // Wygasly token: odswiez poswiadczenia i sprobuj raz jeszcze, nie zuzywajac proby z puli retry.
            if (status == 401 && !authRefreshed && _authentication.RefreshAfterUnauthorized())
            {
                authRefreshed = true;
                _logger.Log(LoyaltyClubLogLevel.Debug,
                    "HTTP 401 dla " + request.Path + " — odswiezam poswiadczenia i ponawiam");
                continue;
            }

            if (RetryPolicy.IsRetryableStatus(status) && CanRetry(request, attempt))
            {
                SleepBeforeRetry(attempt, request, "HTTP " + status);
                attempt++;
                continue;
            }

            if (status >= 200 && status < 300)
            {
                return response;
            }

            throw ToApiException(status, response.Body);
        }
    }

    private TransportResponse SendOnce(ApiRequest request, byte[]? payload)
    {
        using HttpRequestMessage message = BuildHttpRequest(request, payload);
        using CancellationTokenSource timeout = new CancellationTokenSource(_requestTimeout);
        using HttpResponseMessage response =
            _httpClient.Send(message, HttpCompletionOption.ResponseContentRead, timeout.Token);
        using Stream stream = response.Content.ReadAsStream(timeout.Token);
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        return new TransportResponse((int)response.StatusCode, reader.ReadToEnd());
    }

    /// <summary>
    /// Awarie transportu odpowiadajace <c>IOException</c> z wersji dla Javy. Przekroczenie limitu
    /// czasu zadania trafia tu jako <see cref="OperationCanceledException"/> z anulowanego zrodla.
    /// </summary>
    private static bool IsTransportFailure(Exception e) =>
        e is HttpRequestException
        || e is IOException
        || e is SocketException
        || e is OperationCanceledException
        || e is TimeoutException;

    private bool CanRetry(ApiRequest request, int attempt) =>
        request.Retryable && attempt < RetryPolicy.MaxAttempts;

    private void SleepBeforeRetry(int attempt, ApiRequest request, string reason)
    {
        TimeSpan backoff = RetryPolicy.BackoffBefore(attempt);
        _logger.Log(LoyaltyClubLogLevel.Debug,
            "Ponawiam " + request.Method.Name() + " " + request.Path
            + " (proba " + (attempt + 1) + "/" + RetryPolicy.MaxAttempts + ") po "
            + (long)backoff.TotalMilliseconds + " ms, powod: " + reason);
        if (backoff > TimeSpan.Zero)
        {
            Thread.Sleep(backoff);
        }
    }

    private byte[]? SerializeBody(ApiRequest request)
    {
        if (request.Body == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(request.Body, request.Body.GetType(), JsonOptions);
        }
        catch (Exception e) when (e is JsonException || e is NotSupportedException)
        {
            throw new LoyaltyClubSerializationException(
                "Nie udalo sie zserializowac ciala zadania dla " + request.Path, e);
        }
    }

    private HttpRequestMessage BuildHttpRequest(ApiRequest request, byte[]? payload)
    {
        HttpRequestMessage message = new HttpRequestMessage(request.Method.ToHttpMethod(), ResolveUri(request));

        // Cialo ustawiamy jako pierwsze: naglowki zwiazane z trescia (Content-Type) mieszkaja
        // w HttpContent, wiec musi ono istniec, zanim zaczniemy doklejac naglowki.
        if (payload != null)
        {
            ByteArrayContent content = new ByteArrayContent(payload);
            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = null };
            message.Content = content;
        }

        SetHeader(message, "Accept", "application/json");
        SetHeader(message, "User-Agent", _userAgent);

        foreach (KeyValuePair<string, string> header in _defaultHeaders)
        {
            SetHeader(message, header.Key, header.Value);
        }

        foreach (KeyValuePair<string, string> header in request.Headers)
        {
            SetHeader(message, header.Key, header.Value);
        }

        _authentication.Authorize(message);
        return message;
    }

    /// <summary>
    /// Ustawia naglowek bez walidacji formatu — odpowiednik <c>HttpRequest.Builder.header</c>,
    /// ktory w Javie nadpisuje wartosc i nie rozroznia naglowkow zadania od naglowkow tresci.
    /// </summary>
    private static void SetHeader(HttpRequestMessage message, string name, string value)
    {
        message.Headers.Remove(name);
        if (message.Headers.TryAddWithoutValidation(name, value))
        {
            return;
        }

        if (message.Content != null)
        {
            message.Content.Headers.Remove(name);
            message.Content.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private Uri ResolveUri(ApiRequest request)
    {
        StringBuilder url = new StringBuilder(BaseUrl).Append(request.Path);
        IReadOnlyDictionary<string, string> queryParams = request.QueryParams;
        if (queryParams.Count > 0)
        {
            char separator = '?';
            foreach (KeyValuePair<string, string> parameter in queryParams)
            {
                url.Append(separator)
                    .Append(Uris.EncodeQueryComponent(parameter.Key))
                    .Append('=')
                    .Append(Uris.EncodeQueryComponent(parameter.Value));
                separator = '&';
            }
        }

        // Sciezka trafia tu z juz zakodowanymi segmentami (np. numer klienta z ukosnikiem);
        // Uri zachowuje sekwencje %2F i %20 w postaci zakodowanej, wiec routing sie nie rozjezdza.
        return new Uri(url.ToString(), UriKind.Absolute);
    }

    private LoyaltyClubApiException ToApiException(int status, string? body)
    {
        ProblemDetail? problemDetail = ParseProblemDetail(body);
        return status switch
        {
            400 => new BadRequestException(problemDetail, body),
            401 => new UnauthorizedException(problemDetail, body),
            403 => new ForbiddenException(problemDetail, body),
            404 => new NotFoundException(problemDetail, body),
            _ => status >= 500
                ? new ServerException(status, problemDetail, body)
                : new LoyaltyClubApiException(status, problemDetail, body)
        };
    }

    private ProblemDetail? ParseProblemDetail(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ProblemDetail>(body, JsonOptions);
        }
        catch (Exception e) when (e is JsonException || e is NotSupportedException)
        {
            // Backend potrafi odpowiedziec samym kodem statusu (np. 401 z entry pointa bezpieczenstwa)
            // albo strona bledu kontenera — surowe cialo zostaje wtedy w wyjatku.
            _logger.Log(LoyaltyClubLogLevel.Trace, "Cialo bledu nie jest dokumentem ProblemDetail", e);
            return null;
        }
    }

    private static System.Net.Http.HttpClient CreateHttpClient(TimeSpan connectTimeout)
    {
        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            ConnectTimeout = connectTimeout,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        return new System.Net.Http.HttpClient(handler, disposeHandler: true)
        {
            // Limit czasu narzuca transport przez CancellationTokenSource, zeby ponowienia
            // rozrozanialy przekroczenie limitu od zwyklego bledu wejscia-wyjscia.
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        string value = baseUrl;
        while (value.EndsWith('/'))
        {
            value = value.Substring(0, value.Length - 1);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? _))
        {
            throw new LoyaltyClubValidationException("baseUrl nie jest poprawnym adresem URL: " + baseUrl);
        }

        return value;
    }

    /// <summary>Zamyka pule polaczen, o ile transport sam ja utworzyl.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>Odpowiedz sprowadzona do statusu i ciala tekstowego.</summary>
    private readonly struct TransportResponse
    {
        internal TransportResponse(int statusCode, string? body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        internal int StatusCode { get; }

        internal string? Body { get; }
    }
}
