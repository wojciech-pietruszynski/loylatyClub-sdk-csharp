namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>
/// Hierarchia towarowa pozycji paragonu. Backend dopasowuje po niej promocje sklepowe,
/// dlatego kod <see cref="HierarchyCode"/> jest wymagany.
/// </summary>
public sealed class Hierarchy
{
    /// <summary>
    /// Kod hierarchii, wymagany. Wlasciwosc nie moze nazywac sie tak jak typ, wiec nosi
    /// inna nazwe niz pole JSON — atrybut przywraca zgodnosc z kontraktem backendu.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("hierarchy")]
    public string? HierarchyCode { get; init; }

    /// <summary>Klasa towarowa, opcjonalna.</summary>
    public string? ProductClass { get; init; }

    /// <summary>Podklasa towarowa, opcjonalna.</summary>
    public string? Subclass { get; init; }

    public static HierarchyBuilder Builder() => new HierarchyBuilder();
}

public sealed class HierarchyBuilder
{
    private string? _hierarchy;
    private string? _productClass;
    private string? _subclass;

    public HierarchyBuilder HierarchyCode(string? hierarchy)
    {
        _hierarchy = hierarchy;
        return this;
    }

    public HierarchyBuilder ProductClass(string? productClass)
    {
        _productClass = productClass;
        return this;
    }

    public HierarchyBuilder Subclass(string? subclass)
    {
        _subclass = subclass;
        return this;
    }

    public Hierarchy Build() => new Hierarchy
    {
        HierarchyCode = _hierarchy,
        ProductClass = _productClass,
        Subclass = _subclass
    };
}
