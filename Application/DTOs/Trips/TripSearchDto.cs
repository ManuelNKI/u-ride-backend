namespace Application.DTOs.Trips;

/// <summary>
/// DTO para filtros de búsqueda de viajes.
/// </summary>
public class TripSearchDto
{
    public string? OriginZone { get; set; }
    public string? DestinationZone { get; set; }
    public DateTime? DepartureDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
