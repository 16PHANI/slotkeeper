namespace SlotKeeper.Domain.Entities;

public class BookingSlot
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid ResourceId { get; set; }
    public DateTime SlotStartUtc { get; set; }

    public Booking? Booking { get; set; }
}
