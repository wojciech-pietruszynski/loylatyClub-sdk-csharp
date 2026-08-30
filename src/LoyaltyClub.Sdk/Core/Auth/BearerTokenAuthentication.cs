using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Auth;

/// <summary>
/// Token JWT dostarczany z zewnatrz — dla integracji, ktore zdobywaja token wlasnym
/// kanalem i tylko chca, zeby SDK go doklejal.
/// </summary>
public sealed class BearerTokenAuthentication : IAuthenticationProvider
{
    private readonly Func<string> _tokenSupplier;

    public BearerTokenAuthentication(string token)
    {
        string value = Validate.RequireText(token, "token");
        _tokenSupplier = () => value;
    }

    public BearerTokenAuthentication(Func<string> tokenSupplier)
    {
        _tokenSupplier = Validate.RequireNonNull(tokenSupplier, "tokenSupplier");
    }

    public void Authorize(HttpRequestMessage request)
    {
        string token = Validate.RequireText(_tokenSupplier(), "token");
        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
    }
}
