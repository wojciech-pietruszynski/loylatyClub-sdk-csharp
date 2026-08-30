namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Cena pozycji paragonu.</summary>
public sealed class ItemPrice
{
    /// <summary>Kwota; nie moze byc ujemna.</summary>
    public decimal? Amount { get; init; }

    /// <summary>Kod waluty, np. <c>PLN</c>.</summary>
    public string? Currency { get; init; }

    public static ItemPriceBuilder Builder() => new ItemPriceBuilder();
}

public sealed class ItemPriceBuilder
{
    private decimal? _amount;
    private string? _currency;

    public ItemPriceBuilder Amount(decimal? amount)
    {
        _amount = amount;
        return this;
    }

    public ItemPriceBuilder Currency(string? currency)
    {
        _currency = currency;
        return this;
    }

    public ItemPrice Build() => new ItemPrice
    {
        Amount = _amount,
        Currency = _currency
    };
}
