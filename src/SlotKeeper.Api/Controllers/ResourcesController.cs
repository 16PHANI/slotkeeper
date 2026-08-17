using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotKeeper.Api.Dtos;
using SlotKeeper.Api.Services;
using SlotKeeper.Domain.Entities;

namespace SlotKeeper.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resources;

    public ResourcesController(IResourceService resources)
    {
        _resources = resources;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ResourceResponse>>> GetAll(CancellationToken ct)
    {
        var resources = await _resources.GetActiveResourcesAsync(ct);
        return Ok(resources.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ResourceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var resource = await _resources.GetByIdAsync(id, ct);
        return Ok(ToResponse(resource));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResourceResponse>> Create(CreateResourceRequest request, CancellationToken ct)
    {
        var resource = new Resource
        {
            Name = request.Name,
            Description = request.Description,
            Location = request.Location,
            SlotMinutes = request.SlotMinutes,
            MaxBookingsPerUserPerDay = request.MaxBookingsPerUserPerDay
        };

        var created = await _resources.CreateAsync(resource, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResourceResponse>> Update(Guid id, UpdateResourceRequest request, CancellationToken ct)
    {
        var updated = await _resources.UpdateAsync(id, resource =>
        {
            resource.Name = request.Name;
            resource.Description = request.Description;
            resource.Location = request.Location;
            resource.MaxBookingsPerUserPerDay = request.MaxBookingsPerUserPerDay;
            resource.IsActive = request.IsActive;
        }, ct);

        return Ok(ToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _resources.DeactivateAsync(id, ct);
        return NoContent();
    }

    private static ResourceResponse ToResponse(Resource resource) =>
        new(resource.Id, resource.Name, resource.Description, resource.Location,
            resource.SlotMinutes, resource.MaxBookingsPerUserPerDay, resource.IsActive);
}
