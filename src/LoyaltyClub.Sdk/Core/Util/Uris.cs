namespace LoyaltyClub.Sdk.Core.Util;

/// <summary>Pomocnik do skladania sciezek URI.</summary>
public static class Uris
{
    /// <summary>
    /// Koduje pojedynczy segment sciezki. Numer klienta czy kod kuponu moga zawierac znaki
    /// wymagajace escapowania, a wklejone surowo rozjechalyby routing po stronie backendu.
    /// </summary>
    public static string EncodePathSegment(string? segment) =>
        Uri.EscapeDataString(segment ?? string.Empty);

    /// <summary>Koduje nazwe albo wartosc parametru zapytania.</summary>
    public static string EncodeQueryComponent(string? value) =>
        Uri.EscapeDataString(value ?? string.Empty);
}
