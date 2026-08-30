using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// HTTP 400. Backend uzywa tego kodu zarowno dla bledow walidacji
/// (wtedy wypelnione jest <see cref="LoyaltyClubApiException.FieldErrors"/>), jak i dla wszystkich
/// bledow biznesowych (np. przekroczona kwota zwrotu, niepowtarzalny numer transakcji) —
/// wtedy liczy sie <see cref="LoyaltyClubApiException.Detail"/>.
/// </summary>
public class BadRequestException : LoyaltyClubApiException
{
    public BadRequestException(ProblemDetail? problemDetail, string? rawBody)
        : base(400, problemDetail, rawBody)
    {
    }
}
