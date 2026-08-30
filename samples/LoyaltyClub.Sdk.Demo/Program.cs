using LoyaltyClub.Sdk.Core.Exceptions;
using LoyaltyClub.Sdk.Core.Models;
using LoyaltyClub.Sdk.Ecom;
using LoyaltyClub.Sdk.Ecom.Models;
using LoyaltyClub.Sdk.Store;
using LoyaltyClub.Sdk.Store.Models;

namespace LoyaltyClub.Sdk.Demo;

/// <summary>
/// Przyklad uzycia SDK. Domyslnie celuje w lokalny backend LoyaltyClub na porcie 8089;
/// adres i poswiadczenia mozna nadpisac zmiennymi srodowiskowymi.
/// </summary>
public static class Program
{
    public static int Main()
    {
        string baseUrl = Environment.GetEnvironmentVariable("LOYALTYCLUB_BASE_URL") ?? "http://localhost:8089";
        string customerNumber = Environment.GetEnvironmentVariable("LOYALTYCLUB_CUSTOMER") ?? "CUST-000123";

        Console.WriteLine("LoyaltyClub .NET SDK — demo");
        Console.WriteLine("Backend: " + baseUrl);
        Console.WriteLine();

        try
        {
            RunStoreDemo(baseUrl, customerNumber);
            Console.WriteLine();
            RunEcomDemo(baseUrl, customerNumber);
            return 0;
        }
        catch (LoyaltyClubValidationException e)
        {
            Console.Error.WriteLine("Zadanie odrzucone lokalnie: " + e.Message);
            return 2;
        }
        catch (LoyaltyClubApiException e)
        {
            Console.Error.WriteLine("Backend odpowiedzial HTTP " + e.StatusCode + ": " + (e.Detail ?? "brak szczegolow"));
            foreach (KeyValuePair<string, string?> fieldError in e.FieldErrors)
            {
                Console.Error.WriteLine("  " + fieldError.Key + ": " + fieldError.Value);
            }

            return 3;
        }
        catch (LoyaltyClubTransportException e)
        {
            Console.Error.WriteLine("Backend nieosiagalny: " + e.Message);
            Console.Error.WriteLine("Uruchom LoyaltyClub lokalnie albo ustaw LOYALTYCLUB_BASE_URL.");
            return 4;
        }
    }

    private static void RunStoreDemo(string baseUrl, string customerNumber)
    {
        Console.WriteLine("== Store (rola STORE) ==");

        using StoreClient store = StoreClient.Builder()
            .BaseUrl(baseUrl)
            .Credentials(
                Environment.GetEnvironmentVariable("LOYALTYCLUB_STORE_USER") ?? "kasa-01",
                Environment.GetEnvironmentVariable("LOYALTYCLUB_STORE_PASSWORD") ?? "haslo")
            .DefaultCountryCode("PL")
            .Build();

        ServiceInfo info = store.Info();
        Console.WriteLine("info(): " + info.Name + " / " + info.Status);

        string sourceTransactionNumber = "POS-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
        StoreTransactionResponse sale = store.RegisterSale(StoreSaleRequest.Builder()
            .CustomerNumber(customerNumber)
            .SourceTransactionNumber(sourceTransactionNumber)
            .TotalAmount(59.98m)
            .PurchaseTimestamp(DateTime.Now)
            .Item(StoreTransactionItem.Builder()
                .CartPosition("1")
                .Ean("5901234123457")
                .Name("Kawa ziarnista 1 kg")
                .Hierarchy(Hierarchy.Builder().HierarchyCode("FOOD").ProductClass("COFFEE").Build())
                .Price(ItemPrice.Builder().Amount(59.98m).Currency("PLN").Build())
                .Build())
            .Build());

        Console.WriteLine("sprzedaz " + sourceTransactionNumber + ": " + sale.Points
                          + " pkt, dostepne od " + sale.AvailableFrom);

        PointsBalance balance = store.GetPointsBalance(customerNumber);
        Console.WriteLine("saldo: oczekujace=" + balance.PendingPoints
                          + ", dostepne=" + balance.AvailablePoints
                          + ", wygasle=" + balance.ExpiredPoints);
    }

    private static void RunEcomDemo(string baseUrl, string customerNumber)
    {
        Console.WriteLine("== E-commerce (rola ECOM) ==");

        using EcomClient ecom = EcomClient.Builder()
            .BaseUrl(baseUrl)
            .BasicAuth(
                Environment.GetEnvironmentVariable("LOYALTYCLUB_ECOM_USER") ?? "ecom-shop",
                Environment.GetEnvironmentVariable("LOYALTYCLUB_ECOM_PASSWORD") ?? "haslo")
            .Build();

        Console.WriteLine("info(): wersja API " + ecom.Info().ApiVersion);

        EcomCustomerProfile profile = ecom.GetCustomerProfile(customerNumber);
        Console.WriteLine("profil: " + profile.FirstName + " " + profile.LastName
                          + ", prog " + profile.LoyaltyTierCode
                          + ", punkty " + profile.LoyaltyPoints);

        IReadOnlyList<CustomerTransaction> history = ecom.GetTransactions(customerNumber);
        Console.WriteLine("historia: " + history.Count + " pozycji");

        IReadOnlyList<CustomerCoupon> coupons = ecom.GetCoupons(customerNumber);
        Console.WriteLine("kupony: " + coupons.Count);

        // Walidacja kuponu nie jest bledem HTTP — werdykt siedzi w polu status.
        CustomerCoupon? firstCoupon = coupons.Count > 0 ? coupons[0] : null;
        if (firstCoupon?.CouponCode != null)
        {
            CouponValidationResponse validation = ecom.Coupons().Validate(firstCoupon.CouponCode, customerNumber);
            Console.WriteLine("walidacja " + firstCoupon.CouponCode + ": " + validation.Status
                              + (validation.IsValid ? " (do realizacji)" : " (odmowa)"));
        }
        else
        {
            Console.WriteLine("walidacja: klient nie ma kuponow do sprawdzenia");
        }
    }
}
