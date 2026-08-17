using SlotKeeper.Domain.Enums;

namespace SlotKeeper.Domain.Entities;

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    public DateTime DesiredEndUtc { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ResolvedUtc { get; set; }
    public Guid? PromotedBookingId { get; set; }
}
