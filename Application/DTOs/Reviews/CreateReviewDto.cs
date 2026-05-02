namespace Application.DTOs.Reviews;

/// <summary>
/// DTO para crear una review post-viaje.
/// </summary>
public class CreateReviewDto
{
    public Guid TripId { get; set; }
    public string ToUid { get; set; } = null!;
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
