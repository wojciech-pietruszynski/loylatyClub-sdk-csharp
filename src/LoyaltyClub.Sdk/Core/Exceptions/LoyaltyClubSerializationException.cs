namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// Nie udalo sie zserializowac zadania lub zdeserializowac odpowiedzi.
/// </summary>
public class LoyaltyClubSerializationException : LoyaltyClubException
{
    public LoyaltyClubSerializationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
