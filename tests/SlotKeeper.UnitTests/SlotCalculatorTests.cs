using FluentAssertions;
using SlotKeeper.Domain.BookingLogic;
using SlotKeeper.Domain.Exceptions;
using Xunit;

namespace SlotKeeper.UnitTests;

public class SlotCalculatorTests
{
    private static readonly DateTime Epoch = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);

    [Fact]
    public void GetSlotStarts_ReturnsOneEntryPerSlot_ForAWholeHourBooking()
    {
        var start = Epoch.AddDays(9500).AddHours(9);
        var end = start.AddHours(1);

        var slots = SlotCalculator.GetSlotStarts(start, end, slotMinutes: 30);

        slots.Should().HaveCount(2);
        slots[0].Should().Be(start);
        slots[1].Should().Be(start.AddMinutes(30));
    }

    [Fact]
    public void GetSlotStarts_Throws_WhenEndIsBeforeStart()
    {
        var start = Epoch.AddDays(9500).AddHours(9);
        var end = start.AddMinutes(-15);

        var act = () => SlotCalculator.GetSlotStarts(start, end, slotMinutes: 30);

        act.Should().Throw<InvalidBookingWindowException>();
    }

    [Fact]
    public void GetSlotStarts_Throws_WhenDurationIsNotAMultipleOfSlotSize()
    {
        var start = Epoch.AddDays(9500).AddHours(9);
        var end = start.AddMinutes(40);

        var act = () => SlotCalculator.GetSlotStarts(start, end, slotMinutes: 30);

        act.Should().Throw<InvalidBookingWindowException>();
    }

    [Fact]
    public void GetSlotStarts_Throws_WhenStartIsNotAlignedToTheSlotGrid()
    {
        var start = Epoch.AddDays(9500).AddHours(9).AddMinutes(10);
        var end = start.AddMinutes(30);

        var act = () => SlotCalculator.GetSlotStarts(start, end, slotMinutes: 30);

        act.Should().Throw<InvalidBookingWindowException>();
    }

    [Fact]
    public void GetSlotStarts_TwoAdjacentBookings_ProduceNoOverlappingSlots()
    {
        var firstStart = Epoch.AddDays(9500).AddHours(9);
        var firstEnd = firstStart.AddHours(1);
        var secondStart = firstEnd;
        var secondEnd = secondStart.AddHours(1);

        var firstSlots = SlotCalculator.GetSlotStarts(firstStart, firstEnd, slotMinutes: 30);
        var secondSlots = SlotCalculator.GetSlotStarts(secondStart, secondEnd, slotMinutes: 30);

        firstSlots.Should().NotContain(slot => secondSlots.Contains(slot));
    }
}
