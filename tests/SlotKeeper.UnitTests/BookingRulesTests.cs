using FluentAssertions;
using SlotKeeper.Domain.BookingLogic;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Exceptions;
using Xunit;

namespace SlotKeeper.UnitTests;

public class BookingRulesTests
{
    [Fact]
    public void EnsureWithinDailyLimit_Allows_WhenUnderTheLimit()
    {
        var existing = new List<Booking>
        {
            new() { StartUtc = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc) }
        };

        var act = () => BookingRules.EnsureWithinDailyLimit(
            existing, new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc), maxBookingsPerUserPerDay: 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureWithinDailyLimit_Throws_WhenAtTheLimit()
    {
        var existing = new List<Booking>
        {
            new() { StartUtc = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartUtc = new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc) }
        };

        var act = () => BookingRules.EnsureWithinDailyLimit(
            existing, new DateTime(2026, 3, 10, 16, 0, 0, DateTimeKind.Utc), maxBookingsPerUserPerDay: 2);

        act.Should().Throw<BookingLimitExceededException>();
    }

    [Fact]
    public void EnsureWithinDailyLimit_IgnoresBookingsOnOtherDays()
    {
        var existing = new List<Booking>
        {
            new() { StartUtc = new DateTime(2026, 3, 9, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartUtc = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Utc) }
        };

        var act = () => BookingRules.EnsureWithinDailyLimit(
            existing, new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc), maxBookingsPerUserPerDay: 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureBookingIsInTheFuture_Throws_ForAPastStartTime()
    {
        var now = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var start = now.AddMinutes(-5);

        var act = () => BookingRules.EnsureBookingIsInTheFuture(start, now);

        act.Should().Throw<InvalidBookingWindowException>();
    }
}
