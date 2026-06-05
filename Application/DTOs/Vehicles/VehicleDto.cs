namespace Application.DTOs.Vehicles;

public class VehicleDto
{
    public int Id { get; set; }
    public string Brand { get; set; } = null!;
    public string ModelOrBusNumber { get; set; } = null!;
    public string Plate { get; set; } = null!;
    public string Color { get; set; } = null!;
    public int Seats { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
