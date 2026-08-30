namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>
/// Rejestracja sprzedazy — naliczenie punktow za paragon.
/// <para>
/// Backend wymaga, by <see cref="TotalAmount"/> po zaokragleniu do dwoch miejsc rownalo sie
/// sumie cen pozycji, a <see cref="SourceTransactionNumber"/> bylo globalnie unikalne. Oba warunki
/// SDK sprawdza lokalnie (pierwszy) albo zglosi jako HTTP 400 (drugi).
/// </para>
/// </summary>
public sealed class StoreSaleRequest
{
    /// <summary>Numer klienta w programie lojalnosciowym, wymagany.</summary>
    public string? CustomerNumber { get; init; }

    /// <summary>Pozycje paragonu; lista nie moze byc pusta.</summary>
    public IReadOnlyList<StoreTransactionItem> Items { get; init; } = Array.Empty<StoreTransactionItem>();

    /// <summary>Wartosc paragonu; musi byc dodatnia i rowna sumie cen pozycji.</summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>Numer transakcji w systemie zrodlowym (kasa); wymagany i unikalny.</summary>
    public string? SourceTransactionNumber { get; init; }

    /// <summary>Moment zakupu; gdy pominiety, backend przyjmuje czas biezacy.</summary>
    public DateTime? PurchaseTimestamp { get; init; }

    public static StoreSaleRequestBuilder Builder() => new StoreSaleRequestBuilder();
}

public sealed class StoreSaleRequestBuilder
{
    private readonly List<StoreTransactionItem> _items = new List<StoreTransactionItem>();
    private string? _customerNumber;
    private decimal? _totalAmount;
    private string? _sourceTransactionNumber;
    private DateTime? _purchaseTimestamp;

    public StoreSaleRequestBuilder CustomerNumber(string? customerNumber)
    {
        _customerNumber = customerNumber;
        return this;
    }

    /// <summary>Dokleja pojedyncza pozycje; odpowiednik <c>@Singular</c> z Lomboka.</summary>
    public StoreSaleRequestBuilder Item(StoreTransactionItem item)
    {
        _items.Add(item);
        return this;
    }

    public StoreSaleRequestBuilder Items(IEnumerable<StoreTransactionItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        return this;
    }

    public StoreSaleRequestBuilder TotalAmount(decimal? totalAmount)
    {
        _totalAmount = totalAmount;
        return this;
    }

    public StoreSaleRequestBuilder SourceTransactionNumber(string? sourceTransactionNumber)
    {
        _sourceTransactionNumber = sourceTransactionNumber;
        return this;
    }

    public StoreSaleRequestBuilder PurchaseTimestamp(DateTime? purchaseTimestamp)
    {
        _purchaseTimestamp = purchaseTimestamp;
        return this;
    }

    public StoreSaleRequest Build() => new StoreSaleRequest
    {
        CustomerNumber = _customerNumber,
        Items = _items.ToArray(),
        TotalAmount = _totalAmount,
        SourceTransactionNumber = _sourceTransactionNumber,
        PurchaseTimestamp = _purchaseTimestamp
    };
}
