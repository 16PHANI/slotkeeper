using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SlotKeeper.Api.Dtos;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly SlotKeeperDbContext _db;

    public ReportsController(SlotKeeperDbContext db)
    {
        _db = db;
    }

    [HttpGet("utilization")]
    public async Task<ActionResult<List<UtilizationRow>>> GetUtilization(
        [FromQuery] Guid resourceId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        var resourceIdParam = new SqlParameter("@ResourceId", resourceId);
        var fromParam = new SqlParameter("@FromUtc", fromUtc);
        var toParam = new SqlParameter("@ToUtc", toUtc);

        var rows = await _db.Database
            .SqlQueryRaw<UtilizationRow>(
                "EXEC dbo.GetResourceUtilization @ResourceId, @FromUtc, @ToUtc",
                resourceIdParam, fromParam, toParam)
            .ToListAsync(ct);

        return Ok(rows);
    }
}
