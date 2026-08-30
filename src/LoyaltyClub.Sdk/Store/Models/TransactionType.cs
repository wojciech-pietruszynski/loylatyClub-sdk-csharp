using LoyaltyClub.Sdk.Core.Json;

namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Rodzaj transakcji punktowej zarejestrowanej w programie lojalnosciowym.</summary>
public enum TransactionType
{
    SALE,
    RETURN,
    MANUAL_ADJUSTMENT,

    /// <summary>Wartosc nieznana temu wydaniu SDK — nowszy backend dodal kolejny rodzaj.</summary>
    [JsonEnumDefaultValue]
    UNKNOWN
}
