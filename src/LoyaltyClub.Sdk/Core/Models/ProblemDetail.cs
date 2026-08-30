using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoyaltyClub.Sdk.Core.Models;

/// <summary>
/// Cialo bledu w formacie RFC 7807, ktore backend zwraca przez <c>GlobalExceptionHandler</c>.
/// Dodatkowe pola spoza specyfikacji (np. <c>errors</c> przy bledzie walidacji) laduja
/// w <see cref="Properties"/> — odpowiednik <c>@JsonAnySetter</c> z Jacksona.
/// </summary>
public sealed class ProblemDetail
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int? Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    /// <summary>Pola spoza specyfikacji RFC 7807, zachowane w oryginalnej postaci.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();

    /// <summary>
    /// Mapa <c>pole -> komunikat</c> z odpowiedzi 400 dla bledu walidacji.
    /// Pusta, gdy backend nie dolaczyl wlasciwosci <c>errors</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string?> GetFieldErrors()
    {
        if (!Properties.TryGetValue("errors", out JsonElement errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string?>();
        }

        Dictionary<string, string?> result = new Dictionary<string, string?>();
        foreach (JsonProperty property in errors.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => property.Value.GetString(),
                _ => property.Value.ToString()
            };
        }

        return result;
    }

    public override string ToString() =>
        $"ProblemDetail(type={Type}, title={Title}, status={Status}, detail={Detail}, instance={Instance})";
}
