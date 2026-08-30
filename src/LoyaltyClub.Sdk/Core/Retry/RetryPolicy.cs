using System.Globalization;

namespace LoyaltyClub.Sdk.Core.Retry;

/// <summary>
/// Polityka ponowien z wykladniczym backoffem i jitterem.
///
/// <para>Ponawiane sa wylacznie zadania oznaczone jako bezpieczne do powtorzenia
/// (wszystkie GET-y, logowanie oraz realizacja kuponu chroniona naglowkiem
/// <c>Idempotency-Key</c>). Rejestracja sprzedazy i zwrotu nie jest ponawiana automatycznie:
/// backend wymusza unikalnosc <c>sourceTransactionNumber</c>, ale przy bledzie sieci
/// po stronie klienta nie wiadomo, czy transakcja zostala juz zapisana.</para>
/// </summary>
public sealed class RetryPolicy
{
    private readonly HashSet<int> _retryableStatusCodes;

    internal RetryPolicy(
        int maxAttempts,
        TimeSpan initialBackoff,
        TimeSpan maxBackoff,
        double multiplier,
        double jitterFactor,
        IEnumerable<int> retryableStatusCodes,
        bool retryOnIoException)
    {
        MaxAttempts = maxAttempts;
        InitialBackoff = initialBackoff;
        MaxBackoff = maxBackoff;
        Multiplier = multiplier;
        JitterFactor = jitterFactor;
        _retryableStatusCodes = new HashSet<int>(retryableStatusCodes);
        RetryOnIoException = retryOnIoException;
    }

    /// <summary>Laczna liczba prob, wliczajac pierwsza. Wartosc 1 wylacza ponawianie.</summary>
    public int MaxAttempts { get; }

    public TimeSpan InitialBackoff { get; }

    public TimeSpan MaxBackoff { get; }

    public double Multiplier { get; }

    /// <summary>Udzial losowego rozrzutu w wyliczonym opoznieniu, z zakresu 0.0-1.0.</summary>
    public double JitterFactor { get; }

    /// <summary>Kody odpowiedzi kwalifikujace sie do ponowienia.</summary>
    public IReadOnlyCollection<int> RetryableStatusCodes => _retryableStatusCodes;

    /// <summary>Czy ponawiac po bledzie wejscia-wyjscia (zerwane polaczenie, timeout).</summary>
    public bool RetryOnIoException { get; }

    public static RetryPolicyBuilder Builder() => new RetryPolicyBuilder();

    public static RetryPolicy DefaultPolicy() => Builder().Build();

    public static RetryPolicy None() => Builder().MaxAttempts(1).RetryOnIoException(false).Build();

    public RetryPolicyBuilder ToBuilder() =>
        new RetryPolicyBuilder()
            .MaxAttempts(MaxAttempts)
            .InitialBackoff(InitialBackoff)
            .MaxBackoff(MaxBackoff)
            .Multiplier(Multiplier)
            .JitterFactor(JitterFactor)
            .RetryableStatusCodes(_retryableStatusCodes)
            .RetryOnIoException(RetryOnIoException);

    public bool IsRetryableStatus(int statusCode) => _retryableStatusCodes.Contains(statusCode);

    /// <summary>
    /// Opoznienie przed proba numer <paramref name="attempt"/> (liczac od 1 dla pierwszego ponowienia).
    /// </summary>
    public TimeSpan BackoffBefore(int attempt)
    {
        double baseMillis = InitialBackoff.TotalMilliseconds * Math.Pow(Multiplier, Math.Max(0, attempt - 1));
        long capped = (long)Math.Min(baseMillis, MaxBackoff.TotalMilliseconds);
        if (JitterFactor > 0)
        {
            long spread = (long)(capped * JitterFactor);
            if (spread > 0)
            {
                capped = capped - spread + Random.Shared.NextInt64(2 * spread + 1);
            }
        }

        return TimeSpan.FromMilliseconds(Math.Max(0, capped));
    }

    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "RetryPolicy(maxAttempts={0}, initialBackoff={1}ms, maxBackoff={2}ms, multiplier={3}, jitterFactor={4}, retryOnIoException={5})",
        MaxAttempts,
        (long)InitialBackoff.TotalMilliseconds,
        (long)MaxBackoff.TotalMilliseconds,
        Multiplier,
        JitterFactor,
        RetryOnIoException);
}

/// <summary>Budowniczy polityki ponowien; odpowiednik <c>@Builder</c> z Lomboka.</summary>
public sealed class RetryPolicyBuilder
{
    private int _maxAttempts = 3;
    private TimeSpan _initialBackoff = TimeSpan.FromMilliseconds(200);
    private TimeSpan _maxBackoff = TimeSpan.FromSeconds(2);
    private double _multiplier = 2.0d;
    private double _jitterFactor = 0.2d;
    private IEnumerable<int> _retryableStatusCodes = new[] { 408, 425, 429, 500, 502, 503, 504 };
    private bool _retryOnIoException = true;

    public RetryPolicyBuilder MaxAttempts(int maxAttempts)
    {
        _maxAttempts = maxAttempts;
        return this;
    }

    public RetryPolicyBuilder InitialBackoff(TimeSpan initialBackoff)
    {
        _initialBackoff = initialBackoff;
        return this;
    }

    public RetryPolicyBuilder MaxBackoff(TimeSpan maxBackoff)
    {
        _maxBackoff = maxBackoff;
        return this;
    }

    public RetryPolicyBuilder Multiplier(double multiplier)
    {
        _multiplier = multiplier;
        return this;
    }

    public RetryPolicyBuilder JitterFactor(double jitterFactor)
    {
        _jitterFactor = jitterFactor;
        return this;
    }

    public RetryPolicyBuilder RetryableStatusCodes(IEnumerable<int> retryableStatusCodes)
    {
        _retryableStatusCodes = retryableStatusCodes;
        return this;
    }

    public RetryPolicyBuilder RetryOnIoException(bool retryOnIoException)
    {
        _retryOnIoException = retryOnIoException;
        return this;
    }

    public RetryPolicy Build() => new RetryPolicy(
        _maxAttempts,
        _initialBackoff,
        _maxBackoff,
        _multiplier,
        _jitterFactor,
        _retryableStatusCodes,
        _retryOnIoException);
}
