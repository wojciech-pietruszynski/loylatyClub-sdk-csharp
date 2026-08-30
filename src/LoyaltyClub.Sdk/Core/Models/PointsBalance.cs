namespace LoyaltyClub.Sdk.Core.Models;

/// <summary>
/// Saldo punktow klienta w rozbiciu na stany naliczenia. Ten sam ksztalt zwracaja
/// <c>GET /api/store/customers/{customerNumber}/points</c> oraz odpowiednik w <c>/api/ecom</c>.
/// </summary>
public sealed class PointsBalance
{
    public long? CustomerId { get; init; }

    public string? CustomerNumber { get; init; }

    /// <summary>Punkty naliczone, ale jeszcze niedostepne do wykorzystania (okres karencji).</summary>
    public int? PendingPoints { get; init; }

    /// <summary>Punkty gotowe do wymiany na kupon.</summary>
    public int? AvailablePoints { get; init; }

    /// <summary>Punkty, ktore juz wygasly.</summary>
    public int? ExpiredPoints { get; init; }

    public override string ToString() =>
        $"PointsBalance(customerNumber={CustomerNumber}, pending={PendingPoints}, available={AvailablePoints}, expired={ExpiredPoints})";
}
