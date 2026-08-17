namespace SlotKeeper.Infrastructure.Sql;

public static class StoredProcedures
{
    public const string CreateGetResourceUtilization = @"
CREATE OR ALTER PROCEDURE dbo.GetResourceUtilization
    @ResourceId UNIQUEIDENTIFIER,
    @FromUtc DATETIME2,
    @ToUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(b.StartUtc AS DATE) AS BookingDate,
        SUM(DATEDIFF(MINUTE, b.StartUtc, b.EndUtc)) AS BookedMinutes,
        CAST(SUM(DATEDIFF(MINUTE, b.StartUtc, b.EndUtc)) AS DECIMAL(10, 2)) / 1440.0 * 100.0 AS UtilizationPercent
    FROM dbo.Bookings b
    WHERE b.ResourceId = @ResourceId
        AND b.Status = 0
        AND b.StartUtc >= @FromUtc
        AND b.StartUtc < @ToUtc
    GROUP BY CAST(b.StartUtc AS DATE)
    ORDER BY BookingDate;
END
";
}
