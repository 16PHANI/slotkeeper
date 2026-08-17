namespace SlotKeeper.Api.Dtos;

public record CreateResourceRequest(string Name, string Description, string Location, int SlotMinutes, int MaxBookingsPerUserPerDay);
public record UpdateResourceRequest(string Name, string Description, string Location, int MaxBookingsPerUserPerDay, bool IsActive);
public record ResourceResponse(Guid Id, string Name, string Description, string Location, int SlotMinutes, int MaxBookingsPerUserPerDay, bool IsActive);
