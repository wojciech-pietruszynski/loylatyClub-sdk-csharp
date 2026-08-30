using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Util;
using LoyaltyClub.Sdk.Store.Models;

namespace LoyaltyClub.Sdk.Store;

/// <summary>
/// Walidacja zadan sklepowych po stronie klienta. Odwzorowuje ograniczenia
/// <c>StoreTransactionService</c> i bean validation backendu, zeby oczywisty blad
/// kasy nie kosztowal round-tripu zakonczonego HTTP 400.
/// <para>
/// Kontrola zgodnosci sumy pozycji z kwota paragonu uzywa tej samej normalizacji,
/// co backend: zaokraglenie do dwoch miejsc w trybie HALF_UP.
/// </para>
/// </summary>
public static class StoreRequestValidator
{
    private const int AmountScale = 2;
    private const int MaxCountryCodeLength = 3;

    /// <summary>Normalizuje kod kraju tak samo jak backend: trim i wielkie litery.</summary>
    public static string NormalizeCountryCode(string? countryCode)
    {
        string normalized = Validate.RequireText(countryCode, "countryCode").ToUpperInvariant();
        if (normalized.Length > MaxCountryCodeLength)
        {
            throw new LoyaltyClubValidationException(
                "countryCode moze miec najwyzej " + MaxCountryCodeLength + " znaki, bylo: " + normalized);
        }

        return normalized;
    }

    public static void ValidateSale(StoreSaleRequest? request)
    {
        StoreSaleRequest sale = Validate.RequireNonNull(request, "request");
        Validate.RequireText(sale.CustomerNumber, "customerNumber");
        Validate.RequireText(sale.SourceTransactionNumber, "sourceTransactionNumber");
        ValidateItems(sale.Items);
        ValidateTotalAmount(sale.TotalAmount, sale.Items);
    }

    public static void ValidateReturn(StoreReturnRequest? request)
    {
        StoreReturnRequest returnRequest = Validate.RequireNonNull(request, "request");
        Validate.RequireText(returnRequest.CustomerNumber, "customerNumber");
        Validate.RequireText(returnRequest.SourceTransactionNumber, "sourceTransactionNumber");
        Validate.RequireText(returnRequest.SaleTransactionNumber, "saleTransactionNumber");
        ValidateItems(returnRequest.Items);
        ValidateTotalAmount(returnRequest.TotalAmount, returnRequest.Items);
    }

    private static void ValidateItems(IReadOnlyList<StoreTransactionItem>? items)
    {
        IReadOnlyList<StoreTransactionItem> checkedItems = Validate.RequireNotEmpty(items, "items");
        for (int index = 0; index < checkedItems.Count; index++)
        {
            StoreTransactionItem item = Validate.RequireNonNull(checkedItems[index], "items[" + index + "]");
            string prefix = "items[" + index + "].";
            Validate.RequireText(item.Ean, prefix + "ean");
            Validate.RequireText(item.Name, prefix + "name");
            Hierarchy hierarchy = Validate.RequireNonNull(item.Hierarchy, prefix + "hierarchy");
            Validate.RequireText(hierarchy.HierarchyCode, prefix + "hierarchy.hierarchy");
            ItemPrice price = Validate.RequireNonNull(item.Price, prefix + "price");
            Validate.RequireNonNegative(price.Amount, prefix + "price.amount");
            Validate.RequireText(price.Currency, prefix + "price.currency");
        }
    }

    private static void ValidateTotalAmount(decimal? totalAmount, IReadOnlyList<StoreTransactionItem> items)
    {
        decimal total = Validate.RequirePositive(totalAmount, "totalAmount");

        decimal itemsTotal = 0m;
        foreach (StoreTransactionItem item in items)
        {
            itemsTotal += item.Price!.Amount!.Value;
        }

        itemsTotal = Round(itemsTotal);
        decimal normalizedTotal = Round(total);
        if (normalizedTotal != itemsTotal)
        {
            throw new LoyaltyClubValidationException(
                "totalAmount musi rownac sie sumie cen pozycji: "
                + Scaled(normalizedTotal) + " != " + Scaled(itemsTotal));
        }
    }

    /// <summary>Zaokraglenie zgodne z <c>RoundingMode.HALF_UP</c> z Javy.</summary>
    private static decimal Round(decimal value) => Math.Round(value, AmountScale, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Kwota w komunikacie zawsze z dwoma miejscami po przecinku — <c>decimal</c> nie niesie
    /// skali tak jak <c>BigDecimal</c>, wiec format wymuszamy jawnie.
    /// </summary>
    private static string Scaled(decimal value) =>
        value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
