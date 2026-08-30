namespace LoyaltyClub.Sdk.Core.Http;

/// <summary>
/// Metody HTTP uzywane przez API LoyaltyClub. Nazwa typu odbiega od <c>HttpMethod</c>
/// z wersji dla Javy, zeby nie kolidowac z <see cref="System.Net.Http.HttpMethod"/> z BCL.
/// </summary>
public enum ApiHttpMethod
{
    Get,
    Post
}

internal static class ApiHttpMethodExtensions
{
    internal static System.Net.Http.HttpMethod ToHttpMethod(this ApiHttpMethod method) => method switch
    {
        ApiHttpMethod.Get => System.Net.Http.HttpMethod.Get,
        ApiHttpMethod.Post => System.Net.Http.HttpMethod.Post,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Nieobslugiwana metoda HTTP")
    };

    internal static string Name(this ApiHttpMethod method) => method.ToString().ToUpperInvariant();
}
