using SlotKeeper.Domain.Exceptions;

namespace SlotKeeper.Domain.BookingLogic;

public static class SlotCalculator
{
    private static readonly DateTime GridEpochUtc = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);

    public static List<DateTime> GetSlotStarts(DateTime startUtc, DateTime endUtc, int slotMinutes)
    {
        if (slotMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotMinutes), "Slot size must be a positive number of minutes.");
        }

        if (endUtc <= startUtc)
        {
            throw new InvalidBookingWindowException("Booking end time must be after the start time.");
        }

        var slotSpan = TimeSpan.FromMinutes(slotMinutes);
        var totalSpan = endUtc - startUtc;

        if (totalSpan.Ticks % slotSpan.Ticks != 0)
        {
            throw new InvalidBookingWindowException(
                $"Booking window must be a whole multiple of the resource's {slotMinutes}-minute slot size.");
        }

        var offsetFromEpoch = startUtc - GridEpochUtc;

        if (offsetFromEpoch.Ticks % slotSpan.Ticks != 0)
        {
            throw new InvalidBookingWindowException(
                $"Booking start time must align to the resource's {slotMinutes}-minute slot grid.");
        }

        var slots = new List<DateTime>();
        var cursor = startUtc;

        while (cursor < endUtc)
        {
            slots.Add(cursor);
            cursor = cursor.Add(slotSpan);
        }

        return slots;
    }
}
