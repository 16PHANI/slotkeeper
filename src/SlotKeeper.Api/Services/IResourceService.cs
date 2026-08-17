using SlotKeeper.Domain.Entities;

namespace SlotKeeper.Api.Services;

public interface IResourceService
{
    Task<List<Resource>> GetActiveResourcesAsync(CancellationToken ct);
    Task<Resource> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Resource> CreateAsync(Resource resource, CancellationToken ct);
    Task<Resource> UpdateAsync(Guid id, Action<Resource> apply, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
}
