using LoyaltyClub.Sdk.Core.Json;

namespace LoyaltyClub.Sdk.Store.Models;

/// <summary>Stan naliczonych punktow.</summary>
public enum TransactionState
{
    /// <summary>Punkty naliczone, ale jeszcze nie do wykorzystania.</summary>
    PENDING,

    /// <summary>Punkty dostepne do wymiany.</summary>
    AVAILABLE,

    /// <summary>Punkty po terminie waznosci.</summary>
    EXPIRED,

    /// <summary>Wartosc nieznana temu wydaniu SDK.</summary>
    [JsonEnumDefaultValue]
    UNKNOWN
}
