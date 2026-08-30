namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// Wspolny nadtyp wszystkich bledow zglaszanych przez SDK. Pozwala zlapac cala rodzine
/// jednym catch-em, bez wiazania sie z konkretna przyczyna niepowodzenia.
/// </summary>
public class LoyaltyClubException : Exception
{
    public LoyaltyClubException(string message)
        : base(message)
    {
    }

    public LoyaltyClubException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
