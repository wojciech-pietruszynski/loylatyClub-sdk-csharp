namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// Blad warstwy transportowej: brak polaczenia, timeout, anulowanie operacji.
/// Serwer nie zwrocil odpowiedzi HTTP, wiec stan operacji po stronie backendu jest nieznany.
/// </summary>
public class LoyaltyClubTransportException : LoyaltyClubException
{
    public LoyaltyClubTransportException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
