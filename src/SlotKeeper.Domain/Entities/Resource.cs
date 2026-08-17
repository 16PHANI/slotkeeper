namespace SlotKeeper.Domain.Entities;

public class Resource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int SlotMinutes { get; set; } = 30;
    public int MaxBookingsPerUserPerDay { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}
