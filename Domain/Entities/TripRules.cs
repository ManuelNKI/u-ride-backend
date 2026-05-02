namespace Domain.Entities;

/// <summary>
/// Owned Type — reglas de convivencia definidas por el conductor para un Trip.
/// </summary>
public class TripRules
{
    public bool Punctuality { get; set; }
    public bool Respect { get; set; }
    public bool NoSensitiveData { get; set; }
}
