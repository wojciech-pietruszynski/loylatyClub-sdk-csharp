using LoyaltyClub.Sdk.Core.Http;
using LoyaltyClub.Sdk.Core.Util;

namespace LoyaltyClub.Sdk.Core;

/// <summary>
/// Wspolna baza klientow API: trzyma transport i domyka pule polaczen.
/// <para>
/// Klient jest bezpieczny watkowo i przeznaczony do stworzenia raz na aplikacje.
/// Zamkniecie klienta zamyka pule polaczen tylko wtedy, gdy SDK samo ja utworzylo —
/// <see cref="HttpTransportBuilder.HttpClient"/> dostarczony z zewnatrz pozostaje otwarty.
/// </para>
/// </summary>
public abstract class AbstractApiClient : IDisposable
{
    private bool _disposed;

    protected AbstractApiClient(HttpTransport transport)
    {
        Transport = Validate.RequireNonNull(transport, "transport");
    }

    /// <summary>Warstwa transportowa uzywana przez klienta.</summary>
    public HttpTransport Transport { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            Transport.Dispose();
        }
    }
}
