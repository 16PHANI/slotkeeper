using SlotKeeper.Domain.Enums;

namespace SlotKeeper.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedUtc { get; set; }
    public DateTime? CancelledUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Resource? Resource { get; set; }
    public User? User { get; set; }
    public List<BookingSlot> Slots { get; set; } = new();
}
