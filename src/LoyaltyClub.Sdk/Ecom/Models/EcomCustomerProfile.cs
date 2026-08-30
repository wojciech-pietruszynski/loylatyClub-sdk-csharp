namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Profil lojalnosciowy klienta widziany przez sklep internetowy.</summary>
public sealed class EcomCustomerProfile
{
    public long? CustomerId { get; init; }

    public string? CustomerNumber { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Country { get; init; }

    /// <summary>Biezacy stan punktow na koncie klienta.</summary>
    public int? LoyaltyPoints { get; init; }

    /// <summary>Kod progu lojalnosciowego, np. <c>SILVER</c>.</summary>
    public string? LoyaltyTierCode { get; init; }

    /// <summary>Kod polecajacy klienta.</summary>
    public string? ReferralCode { get; init; }
}
