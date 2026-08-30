using LoyaltyClub.Sdk.Core;
using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Exceptions;

namespace LoyaltyClub.Sdk.Ecom;

/// <summary>
/// Poswiadczenia wspolne dla obu klientow e-commerce. Backend nie wystawia endpointu
/// logowania dla roli <c>ECOM</c>, wiec domyslna droga jest HTTP Basic; token JWT
/// mozna podac, jesli integracja zdobywa go wlasnym kanalem.
/// </summary>
/// <typeparam name="TBuilder">typ konkretnego buildera</typeparam>
public abstract class EcomClientBuilderSupport<TBuilder> : AbstractClientBuilder<TBuilder>
    where TBuilder : EcomClientBuilderSupport<TBuilder>
{
    private IAuthenticationProvider? _authentication;

    /// <summary>HTTP Basic z poswiadczeniami uzytkownika o roli <c>ECOM</c>.</summary>
    public TBuilder BasicAuth(string username, string password)
    {
        _authentication = new BasicAuthentication(username, password);
        return Self();
    }

    /// <summary>Gotowy token JWT z rola <c>ECOM</c>, zdobyty poza SDK.</summary>
    public TBuilder BearerToken(string token)
    {
        _authentication = new BearerTokenAuthentication(token);
        return Self();
    }

    /// <summary>Token JWT odczytywany przy kazdym zadaniu — dla integracji rotujacych tokeny.</summary>
    public TBuilder BearerToken(Func<string> tokenSupplier)
    {
        _authentication = new BearerTokenAuthentication(tokenSupplier);
        return Self();
    }

    /// <summary>Wlasna implementacja zrodla poswiadczen.</summary>
    public TBuilder Authentication(IAuthenticationProvider authentication)
    {
        _authentication = authentication;
        return Self();
    }

    protected IAuthenticationProvider RequireAuthentication()
    {
        if (_authentication == null)
        {
            throw new LoyaltyClubValidationException(
                "Brak poswiadczen — uzyj basicAuth(...), bearerToken(...) albo authentication(...)");
        }

        return _authentication;
    }
}
