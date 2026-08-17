using Microsoft.EntityFrameworkCore;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Exceptions;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Services;

public class ResourceService : IResourceService
{
    private readonly SlotKeeperDbContext _db;

    public ResourceService(SlotKeeperDbContext db)
    {
        _db = db;
    }

    public async Task<List<Resource>> GetActiveResourcesAsync(CancellationToken ct)
    {
        return await _db.Resources
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<Resource> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var resource = await _db.Resources.FindAsync(new object?[] { id }, ct);
        return resource ?? throw new EntityNotFoundException($"Resource {id} was not found.");
    }

    public async Task<Resource> CreateAsync(Resource resource, CancellationToken ct)
    {
        resource.Id = Guid.NewGuid();
        resource.CreatedUtc = DateTime.UtcNow;
        resource.IsActive = true;

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);

        return resource;
    }

    public async Task<Resource> UpdateAsync(Guid id, Action<Resource> apply, CancellationToken ct)
    {
        var resource = await GetByIdAsync(id, ct);
        apply(resource);
        await _db.SaveChangesAsync(ct);
        return resource;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var resource = await GetByIdAsync(id, ct);
        resource.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
