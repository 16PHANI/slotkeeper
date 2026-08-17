using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Exceptions;

namespace SlotKeeper.Domain.BookingLogic;

public static class BookingRules
{
    public static void EnsureWithinDailyLimit(
        IEnumerable<Booking> existingActiveBookingsForUserAndResourceOnDay,
        DateTime candidateStartUtc,
        int maxBookingsPerUserPerDay)
    {
        var countOnDay = existingActiveBookingsForUserAndResourceOnDay
            .Count(b => b.StartUtc.Date == candidateStartUtc.Date);

        if (countOnDay >= maxBookingsPerUserPerDay)
        {
            throw new BookingLimitExceededException(
                $"You already have {countOnDay} booking(s) for this resource on {candidateStartUtc:yyyy-MM-dd}. " +
                $"The limit is {maxBookingsPerUserPerDay} per day.");
        }
    }

    public static void EnsureBookingIsInTheFuture(DateTime startUtc, DateTime nowUtc)
    {
        if (startUtc <= nowUtc)
        {
            throw new InvalidBookingWindowException("Bookings must start in the future.");
        }
    }
}
