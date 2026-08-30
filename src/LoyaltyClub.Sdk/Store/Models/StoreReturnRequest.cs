namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>
/// Rejestracja zwrotu — wycofanie punktow naliczonych wczesniejsza sprzedaza.
/// <para>
/// Zwrot musi wskazywac numer transakcji sprzedazy przez <see cref="SaleTransactionNumber"/>.
/// Backend odrzuci zadanie, gdy kraj zwrotu nie zgadza sie z krajem sprzedazy, punkty juz
/// wygasly albo kwota zwrotu przekracza pozostala wartosc sprzedazy.
/// </para>
/// </summary>
public sealed class StoreReturnRequest
{
    /// <summary>Numer klienta w programie lojalnosciowym, wymagany.</summary>
    public string? CustomerNumber { get; init; }

    /// <summary>Zwracane pozycje; lista nie moze byc pusta.</summary>
    public IReadOnlyList<StoreTransactionItem> Items { get; init; } = Array.Empty<StoreTransactionItem>();

    /// <summary>Wartosc zwrotu; musi byc dodatnia i rowna sumie cen pozycji.</summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>Numer transakcji zwrotu w systemie zrodlowym; wymagany i unikalny.</summary>
    public string? SourceTransactionNumber { get; init; }

    /// <summary>Numer pierwotnej transakcji sprzedazy, ktorej dotyczy zwrot; wymagany.</summary>
    public string? SaleTransactionNumber { get; init; }

    /// <summary>Moment zwrotu; gdy pominiety, backend przyjmuje czas biezacy.</summary>
    public DateTime? PurchaseTimestamp { get; init; }

    public static StoreReturnRequestBuilder Builder() => new StoreReturnRequestBuilder();
}

public sealed class StoreReturnRequestBuilder
{
    private readonly List<StoreTransactionItem> _items = new List<StoreTransactionItem>();
    private string? _customerNumber;
    private decimal? _totalAmount;
    private string? _sourceTransactionNumber;
    private string? _saleTransactionNumber;
    private DateTime? _purchaseTimestamp;

    public StoreReturnRequestBuilder CustomerNumber(string? customerNumber)
    {
        _customerNumber = customerNumber;
        return this;
    }

    /// <summary>Dokleja pojedyncza pozycje; odpowiednik <c>@Singular</c> z Lomboka.</summary>
    public StoreReturnRequestBuilder Item(StoreTransactionItem item)
    {
        _items.Add(item);
        return this;
    }

    public StoreReturnRequestBuilder Items(IEnumerable<StoreTransactionItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        return this;
    }

    public StoreReturnRequestBuilder TotalAmount(decimal? totalAmount)
    {
        _totalAmount = totalAmount;
        return this;
    }

    public StoreReturnRequestBuilder SourceTransactionNumber(string? sourceTransactionNumber)
    {
        _sourceTransactionNumber = sourceTransactionNumber;
        return this;
    }

    public StoreReturnRequestBuilder SaleTransactionNumber(string? saleTransactionNumber)
    {
        _saleTransactionNumber = saleTransactionNumber;
        return this;
    }

    public StoreReturnRequestBuilder PurchaseTimestamp(DateTime? purchaseTimestamp)
    {
        _purchaseTimestamp = purchaseTimestamp;
        return this;
    }

    public StoreReturnRequest Build() => new StoreReturnRequest
    {
        CustomerNumber = _customerNumber,
        Items = _items.ToArray(),
        TotalAmount = _totalAmount,
        SourceTransactionNumber = _sourceTransactionNumber,
        SaleTransactionNumber = _saleTransactionNumber,
        PurchaseTimestamp = _purchaseTimestamp
    };
}
