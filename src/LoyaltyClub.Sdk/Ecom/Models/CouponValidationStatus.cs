using LoyaltyClub.Sdk.Core.Json;

namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>
/// Wynik walidacji kuponu. Backend zwraca go w odpowiedzi 200 nawet wtedy, gdy kupon
/// jest nie do przyjecia — o odrzuceniu koszyka decyduje ta wartosc, nie kod HTTP.
/// </summary>
public enum CouponValidationStatus
{
    /// <summary>Kupon mozna zrealizowac.</summary>
    VALID,

    COUPON_NOT_FOUND,
    CUSTOMER_NOT_FOUND,

    /// <summary>Kupon nalezy do innego klienta.</summary>
    COUPON_BELONGS_TO_ANOTHER_ACCOUNT,

    COUPON_ALREADY_USED,
    COUPON_EXPIRED,

    /// <summary>Wartosc nieznana temu wydaniu SDK — potraktuj jak odmowe.</summary>
    [JsonEnumDefaultValue]
    UNKNOWN
}

/// <summary>
/// Odpowiednik metody <c>isValid()</c> z wersji dla Javy; w C# typy wyliczeniowe
/// nie moga miec wlasnych metod, wiec trafia ona do rozszerzenia.
/// </summary>
public static class CouponValidationStatusExtensions
{
    /// <summary>Czy kupon nadaje sie do realizacji.</summary>
    public static bool IsValid(this CouponValidationStatus status) => status == CouponValidationStatus.VALID;
}
