using System.Text.Json;
using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Logging;
using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Http;

/// <summary>
/// Budowniczy warstwy transportowej. Pola sa <c>internal</c>, zeby konstruktor
/// <see cref="HttpTransport"/> siegal po nie wprost, bez powielania listy parametrow.
/// </summary>
public sealed class HttpTransportBuilder
{
    internal string? BaseUrlValue { get; private set; }

    internal System.Net.Http.HttpClient? HttpClientValue { get; private set; }

    internal JsonSerializerOptions? JsonOptionsValue { get; private set; }

    internal TimeSpan? ConnectTimeoutValue { get; private set; }

    internal TimeSpan? RequestTimeoutValue { get; private set; }

    internal Retry.RetryPolicy? RetryPolicyValue { get; private set; }

    internal IAuthenticationProvider? AuthenticationValue { get; private set; }

    internal OrderedStringMap DefaultHeadersValue { get; private set; } = new OrderedStringMap();

    internal string? UserAgentValue { get; private set; }

    internal ILoyaltyClubLogger? LoggerValue { get; private set; }

    public HttpTransportBuilder BaseUrl(string baseUrl)
    {
        BaseUrlValue = baseUrl;
        return this;
    }

    public HttpTransportBuilder BaseUri(Uri baseUri)
    {
        BaseUrlValue = Validate.RequireNonNull(baseUri, "baseUri").ToString();
        return this;
    }

    /// <summary>
    /// Podpina gotowa pule polaczen. Transport takiego klienta nie zamyka — cyklem zycia
    /// zarzadza wtedy strona wolajaca.
    /// </summary>
    public HttpTransportBuilder HttpClient(System.Net.Http.HttpClient? httpClient)
    {
        HttpClientValue = httpClient;
        return this;
    }

    public HttpTransportBuilder JsonOptions(JsonSerializerOptions? jsonOptions)
    {
        JsonOptionsValue = jsonOptions;
        return this;
    }

    public HttpTransportBuilder ConnectTimeout(TimeSpan? connectTimeout)
    {
        ConnectTimeoutValue = connectTimeout;
        return this;
    }

    public HttpTransportBuilder RequestTimeout(TimeSpan? requestTimeout)
    {
        RequestTimeoutValue = requestTimeout;
        return this;
    }

    public HttpTransportBuilder RetryPolicy(Retry.RetryPolicy? retryPolicy)
    {
        RetryPolicyValue = retryPolicy;
        return this;
    }

    public HttpTransportBuilder Authentication(IAuthenticationProvider? authentication)
    {
        AuthenticationValue = authentication;
        return this;
    }

    public HttpTransportBuilder DefaultHeaders(IReadOnlyDictionary<string, string> defaultHeaders)
    {
        DefaultHeadersValue = new OrderedStringMap(Validate.RequireNonNull(defaultHeaders, "defaultHeaders"));
        return this;
    }

    public HttpTransportBuilder DefaultHeader(string name, string value)
    {
        DefaultHeadersValue.Put(name, value);
        return this;
    }

    public HttpTransportBuilder UserAgent(string? userAgent)
    {
        UserAgentValue = userAgent;
        return this;
    }

    /// <summary>
    /// Wyjscie diagnostyczne SDK. Rozszerzenie wzgledem wersji dla Javy, ktora korzysta
    /// z globalnego <c>System.Logger</c>; domyslnie transport nie loguje niczego.
    /// </summary>
    public HttpTransportBuilder Logger(ILoyaltyClubLogger? logger)
    {
        LoggerValue = logger;
        return this;
    }

    public HttpTransport Build() => new HttpTransport(this);
}
