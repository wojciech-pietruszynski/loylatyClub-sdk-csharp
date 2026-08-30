namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Pozycja historii punktowej klienta.</summary>
public sealed class CustomerTransaction
{
    public long? Id { get; init; }

    /// <summary>Punkty naliczone (dodatnie) lub wycofane (ujemne).</summary>
    public int? Points { get; init; }

    public string? Description { get; init; }

    public DateTime? Timestamp { get; init; }

    /// <summary>Moment, od ktorego punkty sa dostepne do wykorzystania.</summary>
    public DateTime? AvailableFrom { get; init; }
}
