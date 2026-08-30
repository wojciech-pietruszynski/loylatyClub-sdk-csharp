namespace LoyaltyClub.Sdk.Core.Auth;

/// <summary>
/// Brak uwierzytelnienia — uzywane wewnetrznie dla wywolania logowania,
/// ktore samo poswiadczen w naglowku nie potrzebuje.
/// </summary>
public sealed class NoAuthentication : IAuthenticationProvider
{
    public static readonly NoAuthentication Instance = new NoAuthentication();

    private NoAuthentication()
    {
    }

    public void Authorize(HttpRequestMessage request)
    {
        // celowo pusto
    }
}
