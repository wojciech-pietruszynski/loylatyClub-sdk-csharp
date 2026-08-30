using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>HTTP 403 — poswiadczenia poprawne, ale rola nie ma dostepu do zasobu (np. ECOM na /api/store).</summary>
public class ForbiddenException : LoyaltyClubApiException
{
    public ForbiddenException(ProblemDetail? problemDetail, string? rawBody)
        : base(403, problemDetail, rawBody)
    {
    }
}
