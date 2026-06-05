using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Vehicles;

public class UpdateVehicleDto
{
    [Required]
    public string Brand { get; set; } = null!;
    
    [Required]
    public string ModelOrBusNumber { get; set; } = null!;
    
    [Required]
    public string Plate { get; set; } = null!;
    
    [Required]
    public string Color { get; set; } = null!;
    
    [Range(1, 50)]
    public int Seats { get; set; }
}
