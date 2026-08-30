namespace LoyaltyClub.Sdk.Ecom.Models;

/// <summary>Zadanie wymiany punktow klienta na kupon z podanego szablonu.</summary>
public sealed class CouponRedeemRequest
{
    /// <summary>Numer klienta, wymagany.</summary>
    public string? CustomerNumber { get; init; }

    /// <summary>Identyfikator szablonu kuponu, wymagany.</summary>
    public long? CouponTemplateId { get; init; }

    public static CouponRedeemRequestBuilder Builder() => new CouponRedeemRequestBuilder();
}

public sealed class CouponRedeemRequestBuilder
{
    private string? _customerNumber;
    private long? _couponTemplateId;

    public CouponRedeemRequestBuilder CustomerNumber(string? customerNumber)
    {
        _customerNumber = customerNumber;
        return this;
    }

    public CouponRedeemRequestBuilder CouponTemplateId(long? couponTemplateId)
    {
        _couponTemplateId = couponTemplateId;
        return this;
    }

    public CouponRedeemRequest Build() => new CouponRedeemRequest
    {
        CustomerNumber = _customerNumber,
        CouponTemplateId = _couponTemplateId
    };
}
