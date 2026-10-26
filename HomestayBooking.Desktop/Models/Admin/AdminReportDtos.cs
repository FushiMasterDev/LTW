namespace HomestayBooking.Desktop.Models.Admin;

internal sealed record AdminAreaDto(int Id, string Name, string Province, string AreaGroup);
internal sealed record AdminCatalogItemDto(int Id, string Name);
internal sealed record AdminAmenityDto(int Id, string Name, string? Icon);
internal sealed record AdminHolidayDto(string Date, string Name);
