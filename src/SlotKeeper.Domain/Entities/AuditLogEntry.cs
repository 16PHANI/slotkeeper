namespace SlotKeeper.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public DateTime TimestampUtc { get; set; }
}
