namespace SlotKeeper.Api.Dtos;

public record UtilizationRow(DateTime BookingDate, int BookedMinutes, decimal UtilizationPercent);
