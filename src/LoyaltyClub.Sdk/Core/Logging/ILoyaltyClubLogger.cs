namespace LoyaltyClub.Sdk.Core.Logging;

/// <summary>Poziomy logowania uzywane przez SDK; odpowiednik <c>System.Logger.Level</c> z Javy.</summary>
public enum LoyaltyClubLogLevel
{
    Trace,
    Debug
}

/// <summary>
/// Minimalne wyjscie diagnostyczne SDK. Odpowiada <c>System.Logger</c> z wersji dla Javy —
/// SDK nie wciaga zadnego frameworku logowania, a host podpina wlasny przez ta abstrakcje.
/// </summary>
public interface ILoyaltyClubLogger
{
    void Log(LoyaltyClubLogLevel level, string message, Exception? exception = null);
}

/// <summary>Domyslne, milczace wyjscie diagnostyczne.</summary>
public sealed class NullLoyaltyClubLogger : ILoyaltyClubLogger
{
    public static readonly NullLoyaltyClubLogger Instance = new NullLoyaltyClubLogger();

    private NullLoyaltyClubLogger()
    {
    }

    public void Log(LoyaltyClubLogLevel level, string message, Exception? exception = null)
    {
        // celowo pusto
    }
}

/// <summary>Wyjscie diagnostyczne kierowane do <see cref="System.Diagnostics.Trace"/>.</summary>
public sealed class TraceLoyaltyClubLogger : ILoyaltyClubLogger
{
    public static readonly TraceLoyaltyClubLogger Instance = new TraceLoyaltyClubLogger();

    public void Log(LoyaltyClubLogLevel level, string message, Exception? exception = null)
    {
        string line = "[LoyaltyClub." + level + "] " + message;
        if (exception != null)
        {
            line += " | " + exception;
        }

        System.Diagnostics.Trace.WriteLine(line);
    }
}
