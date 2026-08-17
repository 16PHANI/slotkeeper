using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SlotKeeper.Domain.BookingLogic;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Enums;
using SlotKeeper.Domain.Exceptions;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Services;

public class BookingService : IBookingService
{
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlServerUniqueIndexViolation = 2601;

    private readonly SlotKeeperDbContext _db;
    private readonly IResourceService _resources;
    private readonly AuditLogger _audit;

    public BookingService(SlotKeeperDbContext db, IResourceService resources, AuditLogger audit)
    {
        _db = db;
        _resources = resources;
        _audit = audit;
    }

    public async Task<Booking> CreateBookingAsync(Guid userId, Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        var resource = await _resources.GetByIdAsync(resourceId, ct);

        var now = DateTime.UtcNow;
        BookingRules.EnsureBookingIsInTheFuture(startUtc, now);

        var slotStarts = SlotCalculator.GetSlotStarts(startUtc, endUtc, resource.SlotMinutes);

        var dayStart = startUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var bookingsThatDay = await _db.Bookings
            .Where(b => b.ResourceId == resourceId
                && b.UserId == userId
                && b.Status == BookingStatus.Confirmed
                && b.StartUtc >= dayStart
                && b.StartUtc < dayEnd)
            .ToListAsync(ct);

        BookingRules.EnsureWithinDailyLimit(bookingsThatDay, startUtc, resource.MaxBookingsPerUserPerDay);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = userId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = BookingStatus.Confirmed,
            CreatedUtc = now,
            RowVersion = Guid.NewGuid().ToByteArray()

        };

        foreach (var slotStart in slotStarts)
        {
            booking.Slots.Add(new BookingSlot
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                ResourceId = resourceId,
                SlotStartUtc = slotStart
            });
        }

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _db.Entry(booking).State = EntityState.Detached;

            foreach (var slot in booking.Slots)
            {
                _db.Entry(slot).State = EntityState.Detached;
            }

            throw new BookingConflictException(
                "One or more of the requested time slots was just taken by another booking. " +
                "Join the waitlist to be notified automatically if it frees up.");
        }

        await _audit.LogAsync(userId, "BookingCreated", nameof(Booking), booking.Id.ToString(),
            new { resourceId, startUtc, endUtc }, ct);

        return booking;
    }

    public async Task CancelBookingAsync(Guid userId, bool isAdmin, Guid bookingId, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots)
            .SingleOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
        {
            throw new EntityNotFoundException($"Booking {bookingId} was not found.");
        }

        if (booking.UserId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return;
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledUtc = DateTime.UtcNow;
        booking.RowVersion = Guid.NewGuid().ToByteArray();
        _db.BookingSlots.RemoveRange(booking.Slots);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(userId, "BookingCancelled", nameof(Booking), booking.Id.ToString(),
            new { booking.ResourceId }, ct);
    }

    public async Task<List<Booking>> GetBookingsForUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Bookings
            .Include(b => b.Resource)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetBookingsForResourceAsync(Guid resourceId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        return await _db.Bookings
            .Where(b => b.ResourceId == resourceId
                && b.Status == BookingStatus.Confirmed
                && b.StartUtc >= fromUtc
                && b.StartUtc < toUtc)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is SqlServerUniqueConstraintViolation or SqlServerUniqueIndexViolation;
        }

        // SQLite is what the integration test suite runs against, and it reports
        // constraint violations as a plain message instead of a numbered error code.
        return ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
    }
}
