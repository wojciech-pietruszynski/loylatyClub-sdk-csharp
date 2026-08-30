using System.Collections;

namespace LoyaltyClub.Sdk.Core.Util;

/// <summary>
/// Mapa lancuchow zachowujaca kolejnosc wstawiania — odpowiednik <c>LinkedHashMap</c>,
/// ktorej Lombok uzywa dla pol <c>@Singular</c>. Kolejnosc ma znaczenie: parametry zapytania
/// trafiaja do adresu w tej samej kolejnosci, w jakiej ustawil je klient.
/// </summary>
public sealed class OrderedStringMap : IReadOnlyDictionary<string, string>
{
    public static readonly OrderedStringMap Empty = new OrderedStringMap();

    private readonly List<string> _order = new List<string>();
    private readonly Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.Ordinal);

    public OrderedStringMap()
    {
    }

    public OrderedStringMap(IReadOnlyDictionary<string, string> source)
    {
        foreach (KeyValuePair<string, string> entry in source)
        {
            Put(entry.Key, entry.Value);
        }
    }

    public int Count => _order.Count;

    public IEnumerable<string> Keys => _order;

    public IEnumerable<string> Values => _order.Select(key => _entries[key]);

    public string this[string key] => _entries[key];

    /// <summary>Wstawia albo nadpisuje wpis, zachowujac pozycje pierwszego wstawienia.</summary>
    public OrderedStringMap Put(string key, string value)
    {
        if (!_entries.ContainsKey(key))
        {
            _order.Add(key);
        }

        _entries[key] = value;
        return this;
    }

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public bool TryGetValue(string key, out string value) => _entries.TryGetValue(key, out value!);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (string key in _order)
        {
            yield return new KeyValuePair<string, string>(key, _entries[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
