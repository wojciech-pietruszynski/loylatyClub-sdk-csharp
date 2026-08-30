namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Wynik rejestracji sprzedazy lub zwrotu.</summary>
public sealed class StoreTransactionResponse
{
    public long? TransactionId { get; init; }

    public long? CustomerId { get; init; }

    public string? CustomerNumber { get; init; }

    public TransactionType? Type { get; init; }

    public TransactionState? State { get; init; }

    /// <summary>Punkty naliczone (sprzedaz) lub wycofane, ze znakiem ujemnym (zwrot).</summary>
    public int? Points { get; init; }

    public decimal? Amount { get; init; }

    /// <summary>Przelicznik punktow na jednostke waluty uzyty przy naliczeniu.</summary>
    public decimal? PointsPerCurrency { get; init; }

    public DateTime? PurchaseTimestamp { get; init; }

    /// <summary>Moment, od ktorego punkty staja sie dostepne do wykorzystania.</summary>
    public DateTime? AvailableFrom { get; init; }

    public DateTime? ExpiresAt { get; init; }
}
