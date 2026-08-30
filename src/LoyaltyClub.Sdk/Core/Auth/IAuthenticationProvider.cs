namespace LoyaltyClub.Sdk.Core.Auth;

/// <summary>
/// Zrodlo poswiadczen dla wywolan API. Implementacja dokleja wlasciwy naglowek
/// <c>Authorization</c> do kazdego zadania.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>Dokleja naglowek uwierzytelniajacy do budowanego zadania.</summary>
    void Authorize(HttpRequestMessage request);

    /// <summary>
    /// Wywolywane po odpowiedzi HTTP 401. Implementacja moze uniewaznic zbuforowane
    /// poswiadczenia (np. wygasly token) i zglosic, ze zadanie warto powtorzyc.
    /// </summary>
    /// <returns><c>true</c>, jesli po odswiezeniu poswiadczen zadanie ma sens powtorzyc.</returns>
    bool RefreshAfterUnauthorized() => false;
}
