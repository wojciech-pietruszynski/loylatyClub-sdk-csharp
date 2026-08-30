using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>HTTP 5xx — blad po stronie backendu; zgodnie z polityka retry zostal juz ponowiony.</summary>
public class ServerException : LoyaltyClubApiException
{
    public ServerException(int statusCode, ProblemDetail? problemDetail, string? rawBody)
        : base(statusCode, problemDetail, rawBody)
    {
    }
}
