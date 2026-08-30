namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Warunki handlowe kuponu, przepisane z szablonu w momencie wydania.</summary>
public sealed class CouponDefinition
{
    public long? CouponTemplateId { get; init; }

    /// <summary>Nominal kuponu.</summary>
    public decimal? CouponValue { get; init; }

    /// <summary>Minimalna wartosc koszyka, przy ktorej kupon dziala.</summary>
    public decimal? MinimumPurchaseValue { get; init; }

    /// <summary>Punkty potrzebne do wymiany.</summary>
    public int? RequiredPoints { get; init; }

    public int? ValidityDays { get; init; }

    public string? CouponPrefix { get; init; }

    public string? Country { get; init; }
}
