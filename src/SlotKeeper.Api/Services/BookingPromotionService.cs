using Microsoft.EntityFrameworkCore;
using SlotKeeper.Domain.Enums;
using SlotKeeper.Domain.Exceptions;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Services;

public class BookingPromotionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingPromotionService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public BookingPromotionService(IServiceScopeFactory scopeFactory, ILogger<BookingPromotionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PromotePendingEntriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Waitlist promotion sweep failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown.
            }
        }
    }

    private async Task PromotePendingEntriesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlotKeeperDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var pending = await db.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.CreatedUtc)
            .Take(50)
            .ToListAsync(ct);

        foreach (var entry in pending)
        {
            try
            {
                var booking = await bookingService.CreateBookingAsync(
                    entry.UserId, entry.ResourceId, entry.DesiredStartUtc, entry.DesiredEndUtc, ct);

                entry.Status = WaitlistStatus.Promoted;
                entry.ResolvedUtc = DateTime.UtcNow;
                entry.PromotedBookingId = booking.Id;

                _logger.LogInformation("Promoted waitlist entry {WaitlistId} to booking {BookingId}.", entry.Id, booking.Id);
            }
            catch (DomainException)
            {
                // Still unavailable, or the user would now violate another rule.
                // Leave it waiting for the next sweep instead of failing loudly.
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
