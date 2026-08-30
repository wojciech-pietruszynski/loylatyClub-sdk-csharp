using LoyaltyClub.Sdk.Core;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Core.Util;
using LoyaltyClub.Sdk.Ecom.Models;

namespace LoyaltyClub.Sdk.Ecom;

/// <summary>
/// Klient odczytowego API sklepu internetowego <c>/api/ecom/**</c> — profil lojalnosciowy,
/// saldo punktow, historia transakcji i kupony klienta. Wymaga konta z rola <c>ECOM</c>.
/// <para>
/// Naliczanie punktow i zwroty zostaja po stronie kasy (<c>/api/store</c>); wymiana punktow
/// na kupon i walidacja kuponu maja wlasnego klienta — dostepnego przez <see cref="Coupons"/>.
/// </para>
/// <para>
/// <code>
/// using EcomClient ecom = EcomClient.Builder()
///         .BaseUrl("http://localhost:8089")
///         .BasicAuth("ecom-shop", "haslo")
///         .Build();
///
/// EcomCustomerProfile profile = ecom.GetCustomerProfile("CUST-000123");
/// PointsBalance balance = ecom.GetPointsBalance("CUST-000123");
/// CouponValidationResponse validation = ecom.Coupons().Validate("PL-ABC123", "CUST-000123");
/// </code>
/// </para>
/// <para>Instancja jest bezpieczna watkowo.</para>
/// </summary>
public class EcomClient : AbstractApiClient
{
    private const string BasePath = "/api/ecom";

    private readonly CouponClient _couponClient;

    internal EcomClient(HttpTransport transport)
        : base(transport)
    {
        _couponClient = new CouponClient(transport);
    }

    public static EcomClientBuilder Builder() => new EcomClientBuilder();

    /// <summary>
    /// Klient <c>/api/coupon/**</c> korzystajacy z tych samych poswiadczen i tej samej puli polaczen.
    /// Nie zamykaj go osobno — zamkniecie tego klienta zamyka oba.
    /// </summary>
    public CouponClient Coupons() => _couponClient;

    /// <summary>
    /// <c>GET /api/ecom</c> — metadane integracji: wersja API i wskazowki nawigacyjne.
    /// Odpowiedz 200 potwierdza dzialajace poswiadczenia z rola <c>ECOM</c>.
    /// </summary>
    public ServiceInfo Info() =>
        Transport.ExecuteRequired<ServiceInfo>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path(BasePath)
            .Retryable(true)
            .Build());

    /// <summary>
    /// <c>GET /api/ecom/customers/{customerNumber}/points</c> — saldo punktow w rozbiciu
    /// na oczekujace, dostepne i wygasle. Ten sam ksztalt, co odpowiednik kasowy.
    /// </summary>
    /// <exception cref="NotFoundException">gdy klient nie istnieje</exception>
    public PointsBalance GetPointsBalance(string customerNumber) =>
        Transport.ExecuteRequired<PointsBalance>(CustomerRequest(customerNumber, "points"));

    /// <summary>
    /// <c>GET /api/ecom/customers/{customerNumber}/profile</c> — dane klienta wraz z progiem
    /// lojalnosciowym i kodem polecajacym.
    /// </summary>
    /// <exception cref="NotFoundException">gdy klient nie istnieje</exception>
    public EcomCustomerProfile GetCustomerProfile(string customerNumber) =>
        Transport.ExecuteRequired<EcomCustomerProfile>(CustomerRequest(customerNumber, "profile"));

    /// <summary><c>GET /api/ecom/customers/{customerNumber}/transactions</c> — historia punktowa klienta.</summary>
    public IReadOnlyList<CustomerTransaction> GetTransactions(string customerNumber) =>
        Transport.ExecuteRequired<List<CustomerTransaction>>(CustomerRequest(customerNumber, "transactions"));

    /// <summary>
    /// <c>GET /api/ecom/customers/{customerNumber}/coupons</c> — kupony wydane klientowi,
    /// niezaleznie od ich statusu.
    /// </summary>
    public IReadOnlyList<CustomerCoupon> GetCoupons(string customerNumber) =>
        Transport.ExecuteRequired<List<CustomerCoupon>>(CustomerRequest(customerNumber, "coupons"));

    private static ApiRequest CustomerRequest(string customerNumber, string resource)
    {
        string normalized = Validate.RequireText(customerNumber, "customerNumber");
        return ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path(BasePath + "/customers/" + Uris.EncodePathSegment(normalized) + "/" + resource)
            .Retryable(true)
            .Build();
    }
}

/// <summary>Builder klienta e-commerce.</summary>
public sealed class EcomClientBuilder : EcomClientBuilderSupport<EcomClientBuilder>
{
    internal EcomClientBuilder()
    {
    }

    public EcomClient Build() => new EcomClient(BuildTransport(RequireAuthentication()));
}
