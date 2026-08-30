using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoyaltyClub.Sdk.Core.Json;

/// <summary>
/// Fabryka <see cref="JsonSerializerOptions"/> skonfigurowanych pod kontrakt backendu LoyaltyClub.
/// Odpowiednik klasy <c>LoyaltyClubJson</c> z wersji dla Javy.
///
/// <para>Backend to Spring Boot z domyslna konfiguracja Jacksona, wiec:</para>
/// <list type="bullet">
///   <item><description><c>DateTime</c> jedzie jako ISO-8601 bez strefy (<c>2026-08-28T21:48:00</c>),
///       nie jako tablica liczb;</description></item>
///   <item><description>nieznane pola w odpowiedzi sa ignorowane, zeby nowsze API nie psulo starszego SDK;</description></item>
///   <item><description>nieznane wartosci enumow mapuja sie na wariant <c>UNKNOWN</c> zamiast rzucac wyjatkiem;</description></item>
///   <item><description>pola <c>null</c> nie sa wysylane — backend traktuje brak pola jak wartosc domyslna
///       (np. <c>purchaseTimestamp</c>).</description></item>
/// </list>
/// </summary>
public static class LoyaltyClubJson
{
    public static JsonSerializerOptions CreateDefault()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            // Backend serializuje camelCase, modele SDK sa PascalCase.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = null,

            // Nieznane pola odpowiedzi sa ignorowane — to domyslne zachowanie System.Text.Json,
            // ustawiamy je jawnie, zeby zmiana domyslnej wartosci nie zmienila kontraktu SDK.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,

            // Pola null nie trafiaja do zadania; backend zastosuje wtedy swoje wartosci domyslne.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Backend potrafi zwrocic liczbe jako lancuch (np. kwoty z niektorych integracji).
            NumberHandling = JsonNumberHandling.AllowReadingFromString,

            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = false
        };

        options.Converters.Add(new TolerantEnumConverterFactory());
        return options;
    }
}
