namespace Domain.Entities;

/// <summary>
/// Clase base de auditoría. Todas las entidades heredan de aquí
/// para registrar automáticamente las fechas de creación y actualización.
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
