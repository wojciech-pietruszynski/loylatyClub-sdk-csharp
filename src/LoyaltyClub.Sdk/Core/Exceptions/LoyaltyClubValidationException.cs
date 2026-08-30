namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// Zadanie odrzucone lokalnie, przed wyslaniem, bo naruszalo kontrakt API.
/// Nie doszlo do zadnego wywolania sieciowego.
/// </summary>
public class LoyaltyClubValidationException : LoyaltyClubException
{
    public LoyaltyClubValidationException(string message)
        : base(message)
    {
    }
}
