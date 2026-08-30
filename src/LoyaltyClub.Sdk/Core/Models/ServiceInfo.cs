namespace LoyaltyClub.Sdk.Core.Models;

/// <summary>
/// Metadane integracyjne zwracane przez <c>GET /api/store</c> i <c>GET /api/ecom</c>.
/// Sluza jako lekki health-check poswiadczen: odpowiedz 200 oznacza, ze konto ma
/// wlasciwa role i dostep do namespace'u.
/// </summary>
public sealed class ServiceInfo
{
    public string? Name { get; init; }

    public string? Status { get; init; }

    /// <summary>Wersja API; wypelniana przez <c>/api/ecom</c>, dla <c>/api/store</c> pozostaje <c>null</c>.</summary>
    public string? ApiVersion { get; init; }

    /// <summary>Krotka podpowiedz nawigacyjna od backendu; tylko <c>/api/ecom</c>.</summary>
    public string? Docs { get; init; }

    public override string ToString() =>
        $"ServiceInfo(name={Name}, status={Status}, apiVersion={ApiVersion})";
}
