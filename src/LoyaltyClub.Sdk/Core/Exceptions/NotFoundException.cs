using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>HTTP 404 — zasob nie istnieje, np. nieznany <c>customerNumber</c>.</summary>
public class NotFoundException : LoyaltyClubApiException
{
    public NotFoundException(ProblemDetail? problemDetail, string? rawBody)
        : base(404, problemDetail, rawBody)
    {
    }
}
