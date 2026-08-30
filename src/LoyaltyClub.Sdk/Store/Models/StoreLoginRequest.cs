namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Poswiadczenia uzytkownika sklepu wysylane do <c>POST /api/store/auth/login</c>.</summary>
public sealed class StoreLoginRequest
{
    public string? Username { get; init; }

    public string? Password { get; init; }

    public static StoreLoginRequestBuilder Builder() => new StoreLoginRequestBuilder();
}

public sealed class StoreLoginRequestBuilder
{
    private string? _username;
    private string? _password;

    public StoreLoginRequestBuilder Username(string? username)
    {
        _username = username;
        return this;
    }

    public StoreLoginRequestBuilder Password(string? password)
    {
        _password = password;
        return this;
    }

    public StoreLoginRequest Build() => new StoreLoginRequest
    {
        Username = _username,
        Password = _password
    };
}
