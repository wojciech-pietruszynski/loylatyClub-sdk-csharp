using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core.Auth;

/// <summary>
/// Baza dla uwierzytelnienia tokenem, ktory SDK samo pobiera i odswieza.
///
/// <para>Token trzymany jest w pamieci i wymieniany na nowy, zanim wygasnie — z zapasem
/// <see cref="RefreshSkew"/>, ktory pochlania zegar rozjechany miedzy klientem a serwerem oraz
/// czas przelotu zadania. Klasa jest bezpieczna watkowo: rownolegle watki czekaja
/// na jedno logowanie zamiast wysylac ich kilka.</para>
/// </summary>
public abstract class RefreshingTokenAuthentication : IAuthenticationProvider
{
    /// <summary>Token wraz z momentem wygasniecia zwroconym przez backend.</summary>
    public sealed record Token
    {
        public Token(string value, DateTimeOffset expiresAt)
        {
            Value = Validate.RequireText(value, "token");
            ExpiresAt = expiresAt;
        }

        public string Value { get; }

        public DateTimeOffset ExpiresAt { get; }
    }

    public static readonly TimeSpan DefaultRefreshSkew = TimeSpan.FromSeconds(60);

    private readonly object _lock = new object();

    private volatile Token? _token;

    protected RefreshingTokenAuthentication(TimeSpan? refreshSkew)
    {
        RefreshSkew = refreshSkew ?? DefaultRefreshSkew;
    }

    /// <summary>Zapas czasu, z jakim token jest wymieniany przed wygasnieciem.</summary>
    public TimeSpan RefreshSkew { get; }

    /// <summary>Pobiera swiezy token z backendu. Wolane pod blokada, wiec bez rownoleglych logowan.</summary>
    protected abstract Token FetchToken();

    public void Authorize(HttpRequestMessage request)
    {
        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + CurrentToken().Value);
    }

    public bool RefreshAfterUnauthorized()
    {
        Invalidate();
        return true;
    }

    /// <summary>Zwraca wazny token, logujac sie lub odswiezajac go w razie potrzeby.</summary>
    public Token CurrentToken()
    {
        Token? cached = _token;
        if (IsUsable(cached))
        {
            return cached!;
        }

        lock (_lock)
        {
            // Inny watek mogl odswiezyc token, zanim ten przejal blokade.
            if (IsUsable(_token))
            {
                return _token!;
            }

            Token fresh = FetchToken();
            _token = fresh;
            return fresh;
        }
    }

    /// <summary>Wyrzuca zbuforowany token; nastepne zadanie zaloguje sie ponownie.</summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            _token = null;
        }
    }

    private bool IsUsable(Token? candidate) =>
        candidate != null && DateTimeOffset.UtcNow.Add(RefreshSkew) < candidate.ExpiresAt;
}
