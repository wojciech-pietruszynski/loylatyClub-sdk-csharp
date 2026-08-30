namespace LoyaltyClub.Sdk.Core.Json;

/// <summary>
/// Oznacza stala wyliczeniowa uzywana, gdy backend przysle wartosc nieznana temu wydaniu SDK.
/// Odpowiednik <c>@JsonEnumDefaultValue</c> z Jacksona.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class JsonEnumDefaultValueAttribute : Attribute
{
}
