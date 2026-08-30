using LoyaltyClub.Sdk.Core.Exceptions;

namespace LoyaltyClub.Sdk.Core.Util;

/// <summary>
/// Walidacja po stronie klienta. Odwzorowuje regularne ograniczenia backendu, zeby
/// oczywisty blad kosztowal wyjatek lokalny zamiast round-tripu zakonczonego HTTP 400.
/// </summary>
public static class Validate
{
    public static string RequireText(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LoyaltyClubValidationException(name + " jest wymagane i nie moze byc puste");
        }

        return value.Trim();
    }

    public static T RequireNonNull<T>(T? value, string name)
        where T : class
    {
        if (value == null)
        {
            throw new LoyaltyClubValidationException(name + " jest wymagane");
        }

        return value;
    }

    public static T RequireNonNullValue<T>(T? value, string name)
        where T : struct
    {
        if (!value.HasValue)
        {
            throw new LoyaltyClubValidationException(name + " jest wymagane");
        }

        return value.Value;
    }

    public static IReadOnlyList<T> RequireNotEmpty<T>(IReadOnlyList<T>? value, string name)
    {
        if (value == null || value.Count == 0)
        {
            throw new LoyaltyClubValidationException(name + " jest wymagane i nie moze byc puste");
        }

        return value;
    }

    public static decimal RequirePositive(decimal? value, string name)
    {
        decimal amount = RequireNonNullValue(value, name);
        if (amount <= 0m)
        {
            throw new LoyaltyClubValidationException(
                name + " musi byc wieksze od zera, bylo: " + Format(amount));
        }

        return amount;
    }

    public static decimal RequireNonNegative(decimal? value, string name)
    {
        decimal amount = RequireNonNullValue(value, name);
        if (amount < 0m)
        {
            throw new LoyaltyClubValidationException(
                name + " nie moze byc ujemne, bylo: " + Format(amount));
        }

        return amount;
    }

    public static void RequireState(bool condition, string message)
    {
        if (!condition)
        {
            throw new LoyaltyClubValidationException(message);
        }
    }

    /// <summary>Kwoty w komunikatach zawsze w formacie niezaleznym od kultury systemu.</summary>
    internal static string Format(decimal value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
