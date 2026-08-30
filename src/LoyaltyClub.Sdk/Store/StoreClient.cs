using LoyaltyClub.Sdk.Core;
using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Util;
using LoyaltyClub.Sdk.Store.Auth;
using LoyaltyClub.Sdk.Store.Models;

namespace LoyaltyClub.Sdk.Store;

/// <summary>
/// Klient API kasowego <c>/api/store/**</c> — rejestracja sprzedazy i zwrotow oraz
/// odczyt salda punktow klienta. Wymaga konta z rola <c>STORE</c>.
/// <para>
/// Typowe uzycie z automatycznym logowaniem i odswiezaniem tokenu JWT:
/// <code>
/// using StoreClient store = StoreClient.Builder()
///         .BaseUrl("http://localhost:8089")
///         .Credentials("kasa-01", "haslo")
///         .DefaultCountryCode("PL")
///         .Build();
///
/// StoreTransactionResponse sale = store.RegisterSale(StoreSaleRequest.Builder()
///         .CustomerNumber("CUST-000123")
///         .SourceTransactionNumber("POS-2026-08-28-0001")
///         .TotalAmount(59.98m)
///         .Item(StoreTransactionItem.Builder()
///                 .Ean("5901234123457")
///                 .Name("Kawa ziarnista 1 kg")
///                 .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").Build())
///                 .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
///                 .Build())
///         .Build());
/// </code>
/// </para>
/// <para>Instancja jest bezpieczna watkowo — tworz ja raz na aplikacje i wspoldziel.</para>
/// </summary>
public class StoreClient : AbstractApiClient
{
    private const string BasePath = "/api/store";
    private const string CountryCodeHeader = "X-CountryCode";

    private readonly string? _defaultCountryCode;

    internal StoreClient(HttpTransport transport, string? defaultCountryCode)
        : base(transport)
    {
        _defaultCountryCode = defaultCountryCode;
    }

    public static StoreClientBuilder Builder() => new StoreClientBuilder();

    /// <summary>
    /// <c>GET /api/store</c> — metadane integracji. Odpowiedz 200 potwierdza, ze poswiadczenia
    /// dzialaja i konto ma role <c>STORE</c>, wiec nadaje sie na health-check przy starcie kasy.
    /// </summary>
    public ServiceInfo Info() =>
        Transport.ExecuteRequired<ServiceInfo>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path(BasePath)
            .Retryable(true)
            .Build());

    /// <summary>
    /// <c>POST /api/store/transactions/sale</c> — rejestruje sprzedaz i nalicza punkty,
    /// uzywajac domyslnego kodu kraju ustawionego w builderze.
    /// </summary>
    public StoreTransactionResponse RegisterSale(StoreSaleRequest request) =>
        RegisterSale(RequireDefaultCountryCode(), request);

    /// <summary>
    /// <c>POST /api/store/transactions/sale</c> — rejestruje sprzedaz i nalicza punkty.
    /// <para>
    /// Operacja nie jest ponawiana automatycznie: przy bledzie sieci nie da sie odroznic
    /// zadania nieodebranego od zapisanego. Po <see cref="LoyaltyClubTransportException"/>
    /// ponow z tym samym <c>sourceTransactionNumber</c> — backend wymusza jego unikalnosc,
    /// wiec duplikat skonczy sie HTTP 400 zamiast podwojnym naliczeniem.
    /// </para>
    /// </summary>
    /// <param name="countryCode">kod kraju sklepu, trafia do naglowka <c>X-CountryCode</c></param>
    /// <param name="request">tresc rejestrowanej sprzedazy</param>
    public StoreTransactionResponse RegisterSale(string countryCode, StoreSaleRequest request)
    {
        string normalizedCountryCode = StoreRequestValidator.NormalizeCountryCode(countryCode);
        StoreRequestValidator.ValidateSale(request);

        return Transport.ExecuteRequired<StoreTransactionResponse>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Post)
            .Path(BasePath + "/transactions/sale")
            .Header(CountryCodeHeader, normalizedCountryCode)
            .Body(request)
            .Retryable(false)
            .Build());
    }

    /// <summary>
    /// <c>POST /api/store/transactions/return</c> — rejestruje zwrot, uzywajac domyslnego
    /// kodu kraju ustawionego w builderze.
    /// </summary>
    public StoreTransactionResponse RegisterReturn(StoreReturnRequest request) =>
        RegisterReturn(RequireDefaultCountryCode(), request);

    /// <summary>
    /// <c>POST /api/store/transactions/return</c> — wycofuje punkty naliczone wskazana sprzedaza.
    /// Tak jak sprzedaz, nie jest ponawiana automatycznie.
    /// </summary>
    /// <param name="countryCode">kod kraju sklepu; musi zgadzac sie z krajem pierwotnej sprzedazy</param>
    /// <param name="request">tresc rejestrowanego zwrotu</param>
    public StoreTransactionResponse RegisterReturn(string countryCode, StoreReturnRequest request)
    {
        string normalizedCountryCode = StoreRequestValidator.NormalizeCountryCode(countryCode);
        StoreRequestValidator.ValidateReturn(request);

        return Transport.ExecuteRequired<StoreTransactionResponse>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Post)
            .Path(BasePath + "/transactions/return")
            .Header(CountryCodeHeader, normalizedCountryCode)
            .Body(request)
            .Retryable(false)
            .Build());
    }

    /// <summary>
    /// <c>GET /api/store/customers/{customerNumber}/points</c> — saldo punktow klienta
    /// w rozbiciu na oczekujace, dostepne i wygasle.
    /// </summary>
    /// <exception cref="NotFoundException">gdy klient nie istnieje</exception>
    public PointsBalance GetPointsBalance(string customerNumber)
    {
        string normalized = Validate.RequireText(customerNumber, "customerNumber");
        return Transport.ExecuteRequired<PointsBalance>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path(BasePath + "/customers/" + Uris.EncodePathSegment(normalized) + "/points")
            .Retryable(true)
            .Build());
    }

    private string RequireDefaultCountryCode()
    {
        if (_defaultCountryCode == null)
        {
            throw new LoyaltyClubValidationException(
                "Nie ustawiono defaultCountryCode — podaj kod kraju w wywolaniu albo w builderze klienta");
        }

        return _defaultCountryCode;
    }
}

/// <summary>Builder klienta sklepowego.</summary>
public sealed class StoreClientBuilder : AbstractClientBuilder<StoreClientBuilder>
{
    private string? _username;
    private string? _password;
    private TimeSpan? _tokenRefreshSkew;
    private IAuthenticationProvider? _authentication;
    private string? _defaultCountryCode;

    internal StoreClientBuilder()
    {
    }

    /// <summary>
    /// Poswiadczenia uzytkownika sklepu. SDK samo zaloguje sie przez
    /// <c>POST /api/store/auth/login</c> i bedzie odswiezac token przed wygasnieciem.
    /// </summary>
    public StoreClientBuilder Credentials(string username, string password)
    {
        _username = username;
        _password = password;
        return this;
    }

    /// <summary>
    /// Zapas czasu, z jakim token jest wymieniany przed wygasnieciem; domyslnie 60 s.
    /// Token backendu zyje 15 minut.
    /// </summary>
    public StoreClientBuilder TokenRefreshSkew(TimeSpan? tokenRefreshSkew)
    {
        _tokenRefreshSkew = tokenRefreshSkew;
        return this;
    }

    /// <summary>
    /// HTTP Basic zamiast JWT. Backend akceptuje oba warianty dla <c>/api/store/**</c>,
    /// ale przy Basic haslo leci w kazdym zadaniu.
    /// </summary>
    public StoreClientBuilder BasicAuth(string username, string password)
    {
        _authentication = new BasicAuthentication(username, password);
        return this;
    }

    /// <summary>Gotowy token JWT zdobyty poza SDK.</summary>
    public StoreClientBuilder BearerToken(string token)
    {
        _authentication = new BearerTokenAuthentication(token);
        return this;
    }

    /// <summary>Wlasna implementacja zrodla poswiadczen.</summary>
    public StoreClientBuilder Authentication(IAuthenticationProvider authentication)
    {
        _authentication = authentication;
        return this;
    }

    /// <summary>
    /// Kod kraju uzywany przez warianty <c>RegisterSale</c>/<c>RegisterReturn</c>
    /// bez jawnego parametru. Kasa stoi w jednym kraju, wiec zwykle ustawia sie go raz.
    /// </summary>
    public StoreClientBuilder DefaultCountryCode(string? defaultCountryCode)
    {
        _defaultCountryCode = defaultCountryCode;
        return this;
    }

    public StoreClient Build()
    {
        string? normalizedCountryCode = _defaultCountryCode == null
            ? null
            : StoreRequestValidator.NormalizeCountryCode(_defaultCountryCode);

        if (_authentication == null)
        {
            Validate.RequireText(_username, "username");
            Validate.RequireNonNull(_password, "password");

            // Logowanie musi isc tym samym transportem, ale bez naglowka Authorization.
            // Transport powstaje dopiero po zbudowaniu uwierzytelnienia, stad referencja z opoznieniem.
            AtomicReference<HttpTransport> loginTransport = new AtomicReference<HttpTransport>();
            StoreJwtAuthentication jwtAuthentication =
                new StoreJwtAuthentication(loginTransport.Get, _username, _password, _tokenRefreshSkew);

            HttpTransport transport = BuildTransport(jwtAuthentication);
            loginTransport.Set(transport.WithoutAuthentication());
            return new StoreClient(transport, normalizedCountryCode);
        }

        return new StoreClient(BuildTransport(_authentication), normalizedCountryCode);
    }
}
