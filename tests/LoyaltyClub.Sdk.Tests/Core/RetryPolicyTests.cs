using LoyaltyClub.Sdk.Core.Retry;
using Xunit;

namespace LoyaltyClub.Sdk.Tests.Core;

public class RetryPolicyTests
{
    [Fact(DisplayName = "domyslna polityka ponawia typowe bledy przejsciowe, ale nie bledy klienta")]
    public void DefaultPolicyRetriesTransientStatuses()
    {
        RetryPolicy policy = RetryPolicy.DefaultPolicy();

        Assert.Equal(3, policy.MaxAttempts);
        Assert.True(policy.IsRetryableStatus(503));
        Assert.True(policy.IsRetryableStatus(429));
        Assert.False(policy.IsRetryableStatus(400));
        Assert.False(policy.IsRetryableStatus(404));
    }

    [Fact(DisplayName = "None() wylacza ponawianie calkowicie")]
    public void NonePolicyDisablesRetries()
    {
        RetryPolicy policy = RetryPolicy.None();

        Assert.Equal(1, policy.MaxAttempts);
        Assert.False(policy.RetryOnIoException);
    }

    [Fact(DisplayName = "backoff rosnie wykladniczo i zatrzymuje sie na gornym limicie")]
    public void BackoffGrowsAndSaturates()
    {
        RetryPolicy policy = RetryPolicy.Builder()
            .InitialBackoff(TimeSpan.FromMilliseconds(100))
            .MaxBackoff(TimeSpan.FromMilliseconds(400))
            .Multiplier(2.0d)
            .JitterFactor(0)
            .Build();

        Assert.Equal(100, (long)policy.BackoffBefore(1).TotalMilliseconds);
        Assert.Equal(200, (long)policy.BackoffBefore(2).TotalMilliseconds);
        Assert.Equal(400, (long)policy.BackoffBefore(3).TotalMilliseconds);
        Assert.Equal(400, (long)policy.BackoffBefore(9).TotalMilliseconds);
    }

    [Fact(DisplayName = "jitter trzyma opoznienie w zadanym przedziale wokol wartosci bazowej")]
    public void JitterStaysWithinBounds()
    {
        RetryPolicy policy = RetryPolicy.Builder()
            .InitialBackoff(TimeSpan.FromMilliseconds(100))
            .MaxBackoff(TimeSpan.FromMilliseconds(100))
            .JitterFactor(0.2d)
            .Build();

        for (int i = 0; i < 200; i++)
        {
            long millis = (long)policy.BackoffBefore(1).TotalMilliseconds;
            Assert.True(millis >= 80 && millis <= 120, "opoznienie poza przedzialem: " + millis);
        }
    }
}
