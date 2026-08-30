using LoyaltyClub.Sdk.Core.Auth;
using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Util;
using LoyaltyClub.Sdk.Store.Models;

namespace LoyaltyClub.Sdk.Store.Auth;

/// <summary>
/// Uwierzytelnianie sklepu tokenem JWT z <c>POST /api/store/auth/login</c>.
/// <para>
/// Token backendu zyje 15 minut, wiec SDK loguje sie ponownie samo — zaraz przed
/// uplywem waznosci oraz po odpowiedzi HTTP 401. Wywolujacy nie musi w ogole dotykac tokenu.
/// </para>
/// </summary>
public class StoreJwtAuthentication : RefreshingTokenAuthentication
{
    public const string LoginPath = "/api/store/auth/login";

    private readonly Func<HttpTransport?> _transportSupplier;
    private readonly string _username;
    private readonly string _password;

    /// <param name="transportSupplier">
    /// zrodlo transportu bez uwierzytelniania; leniwe, bo transport powstaje dopiero
    /// razem z klientem, ktory to uwierzytelnienie trzyma
    /// </param>
    /// <param name="username">nazwa uzytkownika sklepu</param>
    /// <param name="password">haslo uzytkownika sklepu</param>
    /// <param name="refreshSkew">zapas czasu przed wygasnieciem tokenu; <c>null</c> oznacza 60 s</param>
    public StoreJwtAuthentication(
        Func<HttpTransport?> transportSupplier,
        string? username,
        string? password,
        TimeSpan? refreshSkew)
        : base(refreshSkew)
    {
        _transportSupplier = Validate.RequireNonNull(transportSupplier, "transportSupplier");
        _username = Validate.RequireText(username, "username");
        _password = Validate.RequireNonNull(password, "password");
    }

    protected override Token FetchToken()
    {
        HttpTransport? transport = _transportSupplier();
        if (transport == null)
        {
            throw new LoyaltyClubException(
                "Transport nie jest jeszcze gotowy — klient nie zostal w pelni zbudowany");
        }

        StoreLoginResponse? response = transport.Execute<StoreLoginResponse>(
            ApiRequest.Builder()
                .Method(ApiHttpMethod.Post)
                .Path(LoginPath)
                .Body(StoreLoginRequest.Builder().Username(_username).Password(_password).Build())
                // Logowanie nie zmienia stanu biznesowego, wiec ponowienie jest bezpieczne.
                .Retryable(true)
                .Build());

        if (response == null || string.IsNullOrWhiteSpace(response.Token))
        {
            throw new LoyaltyClubException("Logowanie do " + LoginPath + " nie zwrocilo tokenu");
        }

        return new Token(response.Token, response.ExpiresAtInstant());
    }
}
