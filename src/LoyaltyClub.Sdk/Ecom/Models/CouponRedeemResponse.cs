namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Wynik wymiany punktow na kupon.</summary>
public sealed class CouponRedeemResponse
{
    /// <summary>Kod kuponu do przekazania klientowi.</summary>
    public string? CouponCode { get; init; }

    public string? CustomerNumber { get; init; }

    /// <summary>Status wydanego kuponu, np. <c>ACTIVE</c>.</summary>
    public string? Status { get; init; }

    public DateTime? IssuedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    /// <summary>Warunki handlowe kuponu.</summary>
    public CouponDefinition? Definition { get; init; }
}
