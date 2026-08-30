namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Odpowiedz logowania sklepu: token JWT wraz z momentem wygasniecia.</summary>
public sealed class StoreLoginResponse
{
    /// <summary>Token JWT do naglowka <c>Authorization: Bearer</c>.</summary>
    public string? Token { get; init; }

    /// <summary>Moment wygasniecia tokenu jako epoch w milisekundach.</summary>
    public long ExpiresAt { get; init; }

    /// <summary>Rola przypisana tokenowi; dla tego endpointu zawsze <c>STORE</c>.</summary>
    public string? Role { get; init; }

    /// <summary>Kraj uzytkownika; backend zwraca tu <c>null</c> dla roli STORE.</summary>
    public string? Country { get; init; }

    /// <summary><see cref="ExpiresAt"/> jako <see cref="DateTimeOffset"/>.</summary>
    public DateTimeOffset ExpiresAtInstant() => DateTimeOffset.FromUnixTimeMilliseconds(ExpiresAt);
}
