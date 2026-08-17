namespace SlotKeeper.Api.Dtos;

public record CreateBookingRequest(Guid ResourceId, DateTime StartUtc, DateTime EndUtc);
public record BookingResponse(Guid Id, Guid ResourceId, string? ResourceName, DateTime StartUtc, DateTime EndUtc, string Status, DateTime CreatedUtc);
public record JoinWaitlistRequest(Guid ResourceId, DateTime DesiredStartUtc, DateTime DesiredEndUtc);
