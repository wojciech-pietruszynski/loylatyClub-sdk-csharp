using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoyaltyClub.Sdk.Core.Json;

/// <summary>
/// Fabryka konwerterow typow wyliczeniowych odpornych na rozszerzenia kontraktu.
/// Odpowiednik <c>READ_UNKNOWN_ENUM_VALUES_USING_DEFAULT_VALUE</c> z Jacksona: nieznana
/// wartosc mapuje sie na stala oznaczona <see cref="JsonEnumDefaultValueAttribute"/>,
/// zamiast wysadzac deserializacje calej odpowiedzi.
/// </summary>
public sealed class TolerantEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(TolerantEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Czyta i zapisuje stale wyliczeniowe po nazwie, dokladnie tak jak Jackson po stronie backendu.
/// </summary>
public sealed class TolerantEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly TEnum? Fallback = ResolveFallback();

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return FallbackOrThrow(null);

            case JsonTokenType.String:
            {
                string? name = reader.GetString();
                if (string.IsNullOrEmpty(name))
                {
                    // ACCEPT_EMPTY_STRING_AS_NULL_OBJECT: pusty lancuch nie jest werdyktem.
                    return FallbackOrThrow(name);
                }

                if (Enum.TryParse(name, ignoreCase: false, out TEnum parsed) && Enum.IsDefined(typeof(TEnum), parsed))
                {
                    return parsed;
                }

                return FallbackOrThrow(name);
            }

            case JsonTokenType.Number:
            {
                // Jackson dopuszcza tez zapis po numerze porzadkowym.
                if (reader.TryGetInt32(out int ordinal))
                {
                    Array values = Enum.GetValues(typeof(TEnum));
                    if (ordinal >= 0 && ordinal < values.Length)
                    {
                        return (TEnum)values.GetValue(ordinal)!;
                    }
                }

                return FallbackOrThrow(reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            default:
                throw new JsonException(
                    "Nieoczekiwany token " + reader.TokenType + " przy odczycie " + typeof(TEnum).Name);
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    private static TEnum FallbackOrThrow(string? rawValue)
    {
        if (Fallback.HasValue)
        {
            return Fallback.Value;
        }

        throw new JsonException(
            "Nieznana wartosc typu " + typeof(TEnum).Name + ": " + (rawValue ?? "null"));
    }

    private static TEnum? ResolveFallback()
    {
        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetCustomAttribute<JsonEnumDefaultValueAttribute>() != null)
            {
                return (TEnum)field.GetValue(null)!;
            }
        }

        return null;
    }
}
