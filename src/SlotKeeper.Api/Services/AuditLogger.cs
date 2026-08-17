using System.Text.Json;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Services;

public class AuditLogger
{
    private readonly SlotKeeperDbContext _db;

    public AuditLogger(SlotKeeperDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid? userId, string action, string entityType, string entityId, object details, CancellationToken ct)
    {
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(details),
            TimestampUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
