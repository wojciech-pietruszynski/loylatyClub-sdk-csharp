namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Kupon wydany klientowi.</summary>
public sealed class CustomerCoupon
{
    public long? Id { get; init; }

    public string? CouponCode { get; init; }

    public long? CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public string? Country { get; init; }

    /// <summary>Nominal kuponu.</summary>
    public decimal? CouponValue { get; init; }

    /// <summary>Minimalna wartosc koszyka, przy ktorej kupon dziala.</summary>
    public decimal? MinimumPurchaseValue { get; init; }

    /// <summary>Punkty pobrane za wydanie kuponu.</summary>
    public int? RequiredPoints { get; init; }

    public int? ValidityDays { get; init; }

    public string? CouponPrefix { get; init; }

    /// <summary>Powod wydania kuponu, np. wymiana punktow albo akcja obslugi klienta.</summary>
    public string? Reason { get; init; }

    /// <summary>Status kuponu nadany przez backend, np. <c>ACTIVE</c>, <c>USED</c>, <c>EXPIRED</c>.</summary>
    public string? Status { get; init; }

    public DateTime? IssuedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }
}
