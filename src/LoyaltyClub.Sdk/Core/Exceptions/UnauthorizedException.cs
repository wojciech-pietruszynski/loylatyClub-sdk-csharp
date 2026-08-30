using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>HTTP 401 — brak lub niewazne poswiadczenia (wygasly token JWT, zle haslo Basic).</summary>
public class UnauthorizedException : LoyaltyClubApiException
{
    public UnauthorizedException(ProblemDetail? problemDetail, string? rawBody)
        : base(401, problemDetail, rawBody)
    {
    }
}
