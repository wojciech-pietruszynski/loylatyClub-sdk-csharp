using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Http;

/// <summary>
/// Opis pojedynczego wywolania API, niezalezny od warstwy transportowej.
/// </summary>
public sealed class ApiRequest
{
    internal static readonly IReadOnlyDictionary<string, string> NoEntries = OrderedStringMap.Empty;

    public ApiHttpMethod Method { get; init; }

    /// <summary>Sciezka wzgledem bazowego URI, zaczynajaca sie od <c>/</c>, np. <c>/api/store</c>.</summary>
    public string Path { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> QueryParams { get; init; } = NoEntries;

    public IReadOnlyDictionary<string, string> Headers { get; init; } = NoEntries;

    /// <summary>Obiekt do zserializowania jako JSON; <c>null</c> oznacza zadanie bez ciala.</summary>
    public object? Body { get; init; }

    /// <summary>
    /// Czy zadanie mozna bezpiecznie powtorzyc. Ustawiane przez klienta dla kazdej operacji
    /// z osobna — GET-y i operacje chronione kluczem idempotentnosci sa bezpieczne,
    /// rejestracja sprzedazy i zwrotu nie.
    /// </summary>
    public bool Retryable { get; init; }

    public static ApiRequestBuilder Builder() => new ApiRequestBuilder();

    public override string ToString() =>
        $"ApiRequest({Method.Name()} {Path}, queryParams={QueryParams.Count}, headers={Headers.Count}, hasBody={Body != null}, retryable={Retryable})";
}

/// <summary>Budowniczy opisu wywolania; odpowiednik <c>@Builder</c> z Lomboka.</summary>
public sealed class ApiRequestBuilder
{
    private readonly OrderedStringMap _queryParams = new OrderedStringMap();
    private readonly OrderedStringMap _headers = new OrderedStringMap();
    private ApiHttpMethod _method = ApiHttpMethod.Get;
    private string _path = string.Empty;
    private object? _body;
    private bool _retryable;

    public ApiRequestBuilder Method(ApiHttpMethod method)
    {
        _method = method;
        return this;
    }

    public ApiRequestBuilder Path(string path)
    {
        _path = path;
        return this;
    }

    public ApiRequestBuilder QueryParam(string name, string value)
    {
        _queryParams.Put(name, value);
        return this;
    }

    public ApiRequestBuilder Header(string name, string value)
    {
        _headers.Put(name, value);
        return this;
    }

    public ApiRequestBuilder Body(object? body)
    {
        _body = body;
        return this;
    }

    public ApiRequestBuilder Retryable(bool retryable)
    {
        _retryable = retryable;
        return this;
    }

    public ApiRequest Build() => new ApiRequest
    {
        Method = _method,
        Path = _path,
        QueryParams = _queryParams.Count == 0 ? ApiRequest.NoEntries : new OrderedStringMap(_queryParams),
        Headers = _headers.Count == 0 ? ApiRequest.NoEntries : new OrderedStringMap(_headers),
        Body = _body,
        Retryable = _retryable
    };
}
