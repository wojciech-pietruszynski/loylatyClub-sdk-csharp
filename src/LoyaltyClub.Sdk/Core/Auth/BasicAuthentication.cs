using System.Text;
using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Auth;

/// <summary>
/// HTTP Basic — sposob uwierzytelnienia zalecany dla integracji e-commerce
/// (<c>/api/ecom/**</c> i <c>/api/coupon/**</c>), bo backend nie wystawia
/// endpointu logowania dla roli <c>ECOM</c>.
/// </summary>
public sealed class BasicAuthentication : IAuthenticationProvider
{
    private readonly string _headerValue;

    public BasicAuthentication(string username, string password)
    {
        Validate.RequireText(username, "username");
        Validate.RequireNonNull(password, "password");
        string credentials = username + ":" + password;
        _headerValue = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
    }

    public void Authorize(HttpRequestMessage request)
    {
        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", _headerValue);
    }
}
