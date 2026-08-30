using System.Text.Json.Serialization;

namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Wynik walidacji kuponu przy skladaniu zamowienia.</summary>
public sealed class CouponValidationResponse
{
    /// <summary>Werdykt walidacji; sprawdz go przed zastosowaniem rabatu.</summary>
    public CouponValidationStatus? Status { get; init; }

    public string? CouponCode { get; init; }

    public string? CustomerNumber { get; init; }

    /// <summary>Status kuponu w bazie, np. <c>ACTIVE</c>; <c>null</c>, gdy kuponu nie znaleziono.</summary>
    public string? CouponStatus { get; init; }

    public DateTime? IssuedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    /// <summary>Warunki handlowe kuponu; <c>null</c>, gdy kuponu nie znaleziono.</summary>
    public CouponDefinition? Definition { get; init; }

    /// <summary>Skrot na <c>Status == VALID</c>, odporny na <c>null</c>.</summary>
    [JsonIgnore]
    public bool IsValid => Status.HasValue && Status.Value.IsValid();
}
