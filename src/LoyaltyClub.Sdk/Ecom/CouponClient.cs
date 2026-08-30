using LoyaltyClub.Sdk.Core;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Ecom.Models;

// Metoda Validate przyslania typ Validate w obrebie tej klasy, stad alias.
using SdkValidate = LoyaltyClub.Sdk.Core.Util.Validate;

namespace LoyaltyClub.Sdk.Ecom;

/// <summary>
/// Klient API kuponowego <c>/api/coupon/**</c> — wymiana punktow na kupon i walidacja kuponu
/// przy skladaniu zamowienia. Wymaga konta z rola <c>ECOM</c>, tak samo jak <see cref="EcomClient"/>.
/// <para>
/// <code>
/// using CouponClient coupons = CouponClient.Builder()
///         .BaseUrl("http://localhost:8089")
///         .BasicAuth("ecom-shop", "haslo")
///         .Build();
///
/// CouponValidationResponse validation = coupons.Validate("PL-ABC123", "CUST-000123");
/// if (validation.IsValid)
/// {
///     // zastosuj rabat validation.Definition.CouponValue
/// }
/// </code>
/// </para>
/// <para>Instancja jest bezpieczna watkowo.</para>
/// </summary>
public class CouponClient : AbstractApiClient
{
    private const string BasePath = "/api/coupon";
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    internal CouponClient(HttpTransport transport)
        : base(transport)
    {
    }

    public static CouponClientBuilder Builder() => new CouponClientBuilder();

    /// <summary>
    /// <c>POST /api/coupon/redeem-points</c> — wymienia punkty klienta na kupon z podanego szablonu.
    /// <para>
    /// Klucz idempotentnosci jest wymagany przez backend i tam realnie deduplikuje zadania:
    /// powtorzenie z tym samym kluczem zwraca ten sam kupon zamiast pobierac punkty drugi raz.
    /// Dlatego klucz musi byc stabilny dla jednej proby biznesowej — zwykle identyfikator
    /// zamowienia lub akcji w sklepie, a nie swiezy GUID przy kazdej probie. Dzieki temu SDK
    /// moze bezpiecznie ponowic to wywolanie po bledzie sieci.
    /// </para>
    /// </summary>
    /// <param name="idempotencyKey">klucz idempotentnosci, wymagany</param>
    /// <param name="request">numer klienta i identyfikator szablonu kuponu</param>
    public CouponRedeemResponse RedeemPoints(string idempotencyKey, CouponRedeemRequest request)
    {
        string normalizedKey = SdkValidate.RequireText(idempotencyKey, "idempotencyKey");
        CouponRedeemRequest redeemRequest = SdkValidate.RequireNonNull(request, "request");
        SdkValidate.RequireText(redeemRequest.CustomerNumber, "customerNumber");
        SdkValidate.RequireNonNullValue(redeemRequest.CouponTemplateId, "couponTemplateId");

        return Transport.ExecuteRequired<CouponRedeemResponse>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Post)
            .Path(BasePath + "/redeem-points")
            .Header(IdempotencyKeyHeader, normalizedKey)
            .Body(redeemRequest)
            // Bezpieczne do ponowienia: backend deduplikuje po naglowku Idempotency-Key.
            .Retryable(true)
            .Build());
    }

    /// <summary>
    /// <c>GET /api/coupon/validate</c> — sprawdza, czy kupon nalezy do klienta i da sie go zrealizowac.
    /// <para>
    /// Kupon nieprawidlowy nie jest bledem HTTP: odpowiedz ma status 200, a werdykt siedzi
    /// w <see cref="CouponValidationResponse.Status"/>.
    /// </para>
    /// </summary>
    public CouponValidationResponse Validate(string couponCode, string customerNumber) =>
        Transport.ExecuteRequired<CouponValidationResponse>(ApiRequest.Builder()
            .Method(ApiHttpMethod.Get)
            .Path(BasePath + "/validate")
            .QueryParam("couponCode", SdkValidate.RequireText(couponCode, "couponCode"))
            .QueryParam("customerNumber", SdkValidate.RequireText(customerNumber, "customerNumber"))
            .Retryable(true)
            .Build());
}

/// <summary>Builder klienta kuponowego.</summary>
public sealed class CouponClientBuilder : EcomClientBuilderSupport<CouponClientBuilder>
{
    internal CouponClientBuilder()
    {
    }

    public CouponClient Build() => new CouponClient(BuildTransport(RequireAuthentication()));
}
