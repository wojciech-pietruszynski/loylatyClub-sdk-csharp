using System.Text;
using LoyaltyClub.Sdk.Core.Models;

namespace LoyaltyClub.Sdk.Core.Exceptions;

/// <summary>
/// Backend odpowiedzial kodem bledu HTTP. Niesie surowa odpowiedz oraz sparsowany
/// <see cref="Models.ProblemDetail"/>, jesli serwer go zwrocil.
/// </summary>
public class LoyaltyClubApiException : LoyaltyClubException
{
    public LoyaltyClubApiException(int statusCode, ProblemDetail? problemDetail, string? rawBody)
        : base(BuildMessage(statusCode, problemDetail, rawBody))
    {
        StatusCode = statusCode;
        ProblemDetail = problemDetail;
        RawBody = rawBody;
    }

    /// <summary>Kod odpowiedzi HTTP zwrocony przez backend.</summary>
    public int StatusCode { get; }

    /// <summary>Sparsowany dokument RFC 7807 albo <c>null</c>, gdy odpowiedz nim nie byla.</summary>
    public ProblemDetail? ProblemDetail { get; }

    /// <summary>Surowa tresc odpowiedzi bledu, zawsze dostepna do diagnostyki.</summary>
    public string? RawBody { get; }

    /// <summary>
    /// Komunikat biznesowy z pola <c>detail</c>; dla bledow biznesowych backend wklada tam
    /// tresc wyjatku (np. <c>"sourceTransactionNumber must be unique"</c>).
    /// Odpowiednik <c>Optional&lt;String&gt; getDetail()</c> z wersji dla Javy.
    /// </summary>
    public string? Detail => ProblemDetail?.Detail;

    /// <summary>Mapa <c>pole -> komunikat</c> przy bledzie walidacji; pusta w pozostalych przypadkach.</summary>
    public IReadOnlyDictionary<string, string?> FieldErrors =>
        ProblemDetail == null ? EmptyFieldErrors : ProblemDetail.GetFieldErrors();

    private static readonly IReadOnlyDictionary<string, string?> EmptyFieldErrors =
        new Dictionary<string, string?>();

    private static string BuildMessage(int statusCode, ProblemDetail? problemDetail, string? rawBody)
    {
        StringBuilder message = new StringBuilder("LoyaltyClub API zwrocilo HTTP ").Append(statusCode);
        if (problemDetail != null && !string.IsNullOrWhiteSpace(problemDetail.Detail))
        {
            message.Append(": ").Append(problemDetail.Detail);
            IReadOnlyDictionary<string, string?> fieldErrors = problemDetail.GetFieldErrors();
            if (fieldErrors.Count > 0)
            {
                message.Append(' ').Append(FormatFieldErrors(fieldErrors));
            }
        }
        else if (!string.IsNullOrWhiteSpace(rawBody))
        {
            message.Append(": ").Append(rawBody!.Length > 512 ? rawBody.Substring(0, 512) + "..." : rawBody);
        }

        return message.ToString();
    }

    private static string FormatFieldErrors(IReadOnlyDictionary<string, string?> fieldErrors)
    {
        return "{" + string.Join(", ", fieldErrors.Select(entry => entry.Key + "=" + entry.Value)) + "}";
    }
}
