using SlotKeeper.Domain.Entities;

namespace SlotKeeper.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid userId, Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task CancelBookingAsync(Guid userId, bool isAdmin, Guid bookingId, CancellationToken ct);
    Task<List<Booking>> GetBookingsForUserAsync(Guid userId, CancellationToken ct);
    Task<List<Booking>> GetBookingsForResourceAsync(Guid resourceId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
