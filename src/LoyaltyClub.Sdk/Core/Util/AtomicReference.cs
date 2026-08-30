namespace LoyaltyClub.Sdk.Core.Util;

/// <summary>
/// Referencja czytana i zapisywana przez rozne watki. Odpowiednik <c>AtomicReference</c>
/// z Javy w zakresie, w jakim SDK z niej korzysta — publikacja transportu logowania,
/// ktory powstaje dopiero po zbudowaniu zrodla poswiadczen.
/// </summary>
internal sealed class AtomicReference<T>
    where T : class
{
    private volatile T? _value;

    internal T? Get() => _value;

    internal void Set(T? value) => _value = value;
}
