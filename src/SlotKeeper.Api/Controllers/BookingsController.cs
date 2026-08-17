using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotKeeper.Api.Dtos;
using SlotKeeper.Api.Services;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Enums;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    private readonly SlotKeeperDbContext _db;

    public BookingsController(IBookingService bookings, SlotKeeperDbContext db)
    {
        _bookings = bookings;
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var booking = await _bookings.CreateBookingAsync(userId, request.ResourceId, request.StartUtc, request.EndUtc, ct);
        var resourceName = (await _db.Resources.FindAsync(new object?[] { request.ResourceId }, ct))?.Name;

        return Ok(ToResponse(booking, resourceName));
    }

    [HttpPost("waitlist")]
    public async Task<IActionResult> JoinWaitlist(JoinWaitlistRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        _db.WaitlistEntries.Add(new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            ResourceId = request.ResourceId,
            UserId = userId,
            DesiredStartUtc = request.DesiredStartUtc,
            DesiredEndUtc = request.DesiredEndUtc,
            Status = WaitlistStatus.Waiting,
            CreatedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return Accepted();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        await _bookings.CancelBookingAsync(userId, isAdmin, id, ct);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<BookingResponse>>> GetMine(CancellationToken ct)
    {
        var userId = GetUserId();
        var bookings = await _bookings.GetBookingsForUserAsync(userId, ct);

        return Ok(bookings.Select(b => ToResponse(b, b.Resource?.Name)));
    }

    [HttpGet("resource/{resourceId:guid}")]
    public async Task<ActionResult<List<BookingResponse>>> GetForResource(
        Guid resourceId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        var bookings = await _bookings.GetBookingsForResourceAsync(resourceId, fromUtc, toUtc, ct);
        return Ok(bookings.Select(b => ToResponse(b, null)));
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(subject ?? throw new UnauthorizedAccessException("Token is missing a subject claim."));
    }

    private static BookingResponse ToResponse(Booking booking, string? resourceName) =>
        new(booking.Id, booking.ResourceId, resourceName, booking.StartUtc, booking.EndUtc, booking.Status.ToString(), booking.CreatedUtc);
}
