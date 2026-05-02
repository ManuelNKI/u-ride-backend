namespace Domain.Entities;

/// <summary>
/// Owned Type — información del vehículo asociada a un Trip.
/// No tiene tabla propia; se almacena como columnas dentro de Trips.
/// </summary>
public class VehicleInfo
{
    public string Plate { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Color { get; set; } = null!;
}
