using System.Text.Json;
using LoyaltyClub.Sdk.Core.Json;
using LoyaltyClub.Sdk.Core.Models;
using Xunit;

namespace LoyaltyClub.Sdk.Tests.Core;

public class LoyaltyClubJsonTests
{
    private readonly JsonSerializerOptions _options = LoyaltyClubJson.CreateDefault();

    /// <summary>Cialo zadania z data lokalna, odwzorowujace ksztalt zadan sklepowych.</summary>
    private sealed record Payload(string CustomerNumber, DateTime? PurchaseTimestamp);

    [Fact(DisplayName = "serializuje date lokalna jako ISO-8601 bez strefy, tak jak oczekuje backend")]
    public void SerializesLocalDateTimeAsIsoString()
    {
        string json = JsonSerializer.Serialize(
            new Payload("CUST-1", new DateTime(2026, 8, 28, 21, 48, 5, DateTimeKind.Unspecified)),
            _options);

        Assert.Equal("{\"customerNumber\":\"CUST-1\",\"purchaseTimestamp\":\"2026-08-28T21:48:05\"}", json);
    }

    [Fact(DisplayName = "pomija pola null, zeby backend zastosowal swoje wartosci domyslne")]
    public void OmitsNullFields()
    {
        string json = JsonSerializer.Serialize(new Payload("CUST-1", null), _options);

        Assert.Equal("{\"customerNumber\":\"CUST-1\"}", json);
    }

    [Fact(DisplayName = "ignoruje nieznane pola odpowiedzi, zeby nowsze API nie psulo starszego SDK")]
    public void IgnoresUnknownResponseFields()
    {
        PointsBalance? balance = JsonSerializer.Deserialize<PointsBalance>(
            """{"customerNumber":"CUST-1","availablePoints":12,"nowePoleZPrzyszlosci":"x"}""",
            _options);

        Assert.NotNull(balance);
        Assert.Equal("CUST-1", balance!.CustomerNumber);
        Assert.Equal(12, balance.AvailablePoints);
        Assert.Null(balance.PendingPoints);
    }

    [Fact(DisplayName = "czyta ProblemDetail razem z niestandardowym polem errors")]
    public void ReadsProblemDetailWithCustomProperties()
    {
        ProblemDetail? problem = JsonSerializer.Deserialize<ProblemDetail>(
            """
            {"type":"about:blank","title":"Bad Request","status":400,"detail":"Validation failed",
             "errors":{"items":"Items are required","totalAmount":"Amount is required"}}
            """,
            _options);

        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("Validation failed", problem.Detail);

        IReadOnlyDictionary<string, string?> fieldErrors = problem.GetFieldErrors();
        Assert.Equal(2, fieldErrors.Count);
        Assert.Equal("Items are required", fieldErrors["items"]);
        Assert.Equal("Amount is required", fieldErrors["totalAmount"]);
    }

    [Fact(DisplayName = "ProblemDetail bez pola errors zwraca pusta mape zamiast null")]
    public void ProblemDetailWithoutErrorsReturnsEmptyMap()
    {
        ProblemDetail? problem = JsonSerializer.Deserialize<ProblemDetail>(
            """{"status":404,"detail":"Customer not found"}""",
            _options);

        Assert.NotNull(problem);
        Assert.Empty(problem!.GetFieldErrors());
        Assert.False(problem.Properties.ContainsKey("errors"));
    }
}
