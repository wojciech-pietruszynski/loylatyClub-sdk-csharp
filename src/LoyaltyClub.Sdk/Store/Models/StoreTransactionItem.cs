namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Pojedyncza pozycja paragonu przekazywana przy rejestracji sprzedazy lub zwrotu.</summary>
public sealed class StoreTransactionItem
{
    /// <summary>Numer pozycji na paragonie, opcjonalny.</summary>
    public string? CartPosition { get; init; }

    /// <summary>Kod EAN towaru, wymagany.</summary>
    public string? Ean { get; init; }

    /// <summary>Nazwa towaru, wymagana.</summary>
    public string? Name { get; init; }

    /// <summary>Hierarchia towarowa, wymagana.</summary>
    public Hierarchy? Hierarchy { get; init; }

    /// <summary>Cena pozycji, wymagana.</summary>
    public ItemPrice? Price { get; init; }

    public static StoreTransactionItemBuilder Builder() => new StoreTransactionItemBuilder();
}

public sealed class StoreTransactionItemBuilder
{
    private string? _cartPosition;
    private string? _ean;
    private string? _name;
    private Hierarchy? _hierarchy;
    private ItemPrice? _price;

    public StoreTransactionItemBuilder CartPosition(string? cartPosition)
    {
        _cartPosition = cartPosition;
        return this;
    }

    public StoreTransactionItemBuilder Ean(string? ean)
    {
        _ean = ean;
        return this;
    }

    public StoreTransactionItemBuilder Name(string? name)
    {
        _name = name;
        return this;
    }

    public StoreTransactionItemBuilder Hierarchy(Hierarchy? hierarchy)
    {
        _hierarchy = hierarchy;
        return this;
    }

    public StoreTransactionItemBuilder Price(ItemPrice? price)
    {
        _price = price;
        return this;
    }

    public StoreTransactionItem Build() => new StoreTransactionItem
    {
        CartPosition = _cartPosition,
        Ean = _ean,
        Name = _name,
        Hierarchy = _hierarchy,
        Price = _price
    };
}
