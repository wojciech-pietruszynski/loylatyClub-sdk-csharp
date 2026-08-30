using System.Text.Json;
using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Logging;
using LoyaltyClub.Sdk.Core.Retry;
using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core;

/// <summary>
/// Wspolne pokretla konfiguracyjne buildera klienta: adres backendu, pula polaczen,
/// limity czasu, polityka ponowien i naglowki domyslne. Podklasy dokladaja wylacznie
/// to, co specyficzne dla swojego API (poswiadczenia, domyslny kod kraju).
/// </summary>
/// <typeparam name="TBuilder">typ konkretnego buildera, zeby metody lancuchowe zwracaly wlasciwy typ</typeparam>
public abstract class AbstractClientBuilder<TBuilder>
    where TBuilder : AbstractClientBuilder<TBuilder>
{
    private readonly OrderedStringMap _defaultHeaders = new OrderedStringMap();
    private string? _baseUrl;
    private System.Net.Http.HttpClient? _httpClient;
    private JsonSerializerOptions? _jsonOptions;
    private TimeSpan? _connectTimeout;
    private TimeSpan? _requestTimeout;
    private RetryPolicy? _retryPolicy;
    private string? _userAgent;
    private ILoyaltyClubLogger? _logger;

    protected TBuilder Self() => (TBuilder)this;

    /// <summary>Adres bazowy backendu, np. <c>https://loyalty.example.com</c> lub <c>http://localhost:8089</c>.</summary>
    public TBuilder BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return Self();
    }

    /// <summary>Wlasna pula polaczen; przydatne, gdy aplikacja hosta ma juz skonfigurowany <c>HttpClient</c>.</summary>
    public TBuilder HttpClient(System.Net.Http.HttpClient? httpClient)
    {
        _httpClient = httpClient;
        return Self();
    }

    /// <summary>
    /// Wlasna konfiguracja JSON. Musi zachowywac kontrakt backendu — nazwy w camelCase,
    /// pomijanie nieznanych pol i tolerancyjne wartosci wyliczeniowe.
    /// </summary>
    public TBuilder JsonOptions(JsonSerializerOptions? jsonOptions)
    {
        _jsonOptions = jsonOptions;
        return Self();
    }

    /// <summary>Limit czasu na nawiazanie polaczenia TCP; domyslnie 10 s.</summary>
    public TBuilder ConnectTimeout(TimeSpan? connectTimeout)
    {
        _connectTimeout = connectTimeout;
        return Self();
    }

    /// <summary>Limit czasu na cale wywolanie; domyslnie 30 s.</summary>
    public TBuilder RequestTimeout(TimeSpan? requestTimeout)
    {
        _requestTimeout = requestTimeout;
        return Self();
    }

    /// <summary>Polityka ponowien; domyslnie 3 proby z wykladniczym backoffem.</summary>
    public TBuilder RetryPolicy(RetryPolicy? retryPolicy)
    {
        _retryPolicy = retryPolicy;
        return Self();
    }

    /// <summary>Naglowek doklejany do kazdego zadania, np. identyfikator systemu wywolujacego.</summary>
    public TBuilder DefaultHeader(string name, string value)
    {
        _defaultHeaders.Put(name, value);
        return Self();
    }

    public TBuilder UserAgent(string? userAgent)
    {
        _userAgent = userAgent;
        return Self();
    }

    /// <summary>
    /// Wyjscie diagnostyczne SDK. Rozszerzenie wzgledem wersji dla Javy, ktora loguje
    /// przez globalny <c>System.Logger</c>; domyslnie SDK nie loguje niczego.
    /// </summary>
    public TBuilder Logger(ILoyaltyClubLogger? logger)
    {
        _logger = logger;
        return Self();
    }

    /// <summary>Sklada transport z zebranej konfiguracji i podanego zrodla poswiadczen.</summary>
    protected HttpTransport BuildTransport(IAuthenticationProvider authentication) =>
        HttpTransport.Builder()
            .BaseUrl(Validate.RequireText(_baseUrl, "baseUrl"))
            .HttpClient(_httpClient)
            .JsonOptions(_jsonOptions)
            .ConnectTimeout(_connectTimeout)
            .RequestTimeout(_requestTimeout)
            .RetryPolicy(_retryPolicy)
            .Authentication(authentication)
            .DefaultHeaders(_defaultHeaders)
            .UserAgent(_userAgent)
            .Logger(_logger)
            .Build();
}
