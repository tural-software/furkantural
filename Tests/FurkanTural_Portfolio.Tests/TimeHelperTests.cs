using FluentAssertions;
using FurkanTural_Portfolio.Helpers;

namespace FurkanTural_Portfolio.Tests;

/// <summary>Unit tests for TimeHelper (pure static helper).<para>TimeHelper.NowIstanbul converts UTC to Europe/Istanbul (UTC+3, no DST since 2016). We verify offset invariants and basic conversion correctness.</para><para>Note: TimeHelper.Resolve() is private and its logic (trying "Europe/Istanbul" then "Turkey Standard Time" then a manual UTC+3 zone) is transparent to tests - the important observable is the UTC+3 offset on the returned DateTime.</para></summary>
public class TimeHelperTests
{
    /// <summary>Turkey has been on permanent UTC+3 (no DST) since 2016. NowIstanbul must always be 3 hours ahead of UtcNow. We allow ±1 minute tolerance for test execution time.</summary>
    [Fact]
    public void NowIstanbul_IsAlwaysThreeHoursAheadOfUtc()
    {
        // Arrange
        var utcBefore = DateTime.UtcNow;

        // Act
        var istanbul = TimeHelper.NowIstanbul;

        // Assert - must be within 3h ± 1 min of the captured UTC snapshot
        var expectedMin = utcBefore.AddHours(3).AddMinutes(-1);
        var expectedMax = utcBefore.AddHours(3).AddMinutes( 1);
        istanbul.Should().BeOnOrAfter(expectedMin)
                         .And.BeOnOrBefore(expectedMax);
    }

    [Fact]
    public void NowIstanbul_KindIsUnspecified()
    {
        // TimeZoneInfo.ConvertTimeFromUtc returns Unspecified kind (not UTC, not Local)
        var istanbul = TimeHelper.NowIstanbul;

        istanbul.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void NowIstanbul_IsGreaterThanUtcNow()
    {
        // Istanbul is always ahead of UTC
        var utc      = DateTime.UtcNow;
        var istanbul = TimeHelper.NowIstanbul;

        istanbul.Should().BeAfter(utc.AddHours(2).AddMinutes(59));
    }

    [Fact]
    public void NowIstanbul_CalledTwiceReturnsMonotonicallyIncreasingOrEqualTimes()
    {
        // Consecutive calls must not go backwards
        var first  = TimeHelper.NowIstanbul;
        var second = TimeHelper.NowIstanbul;

        second.Should().BeOnOrAfter(first);
    }

    [Fact]
    public void NowIstanbul_IsNotMinValue()
    {
        // Ensures Resolve() did not throw or fall through to a bad default
        var istanbul = TimeHelper.NowIstanbul;

        istanbul.Should().NotBe(DateTime.MinValue);
        istanbul.Should().NotBe(DateTime.MaxValue);
    }
}
