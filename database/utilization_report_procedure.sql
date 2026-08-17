-- Reference copy of the stored procedure that src/SlotKeeper.Infrastructure/Sql/StoredProcedures.cs
-- creates automatically on startup against SQL Server. Kept here as a plain .sql file so it can be
-- reviewed, run manually against a database, or diffed in a PR without spinning up the API.

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
