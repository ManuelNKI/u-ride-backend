namespace Application.DTOs.Reviews;

/// <summary>
/// DTO de respuesta para una review.
/// </summary>
public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string FromUid { get; set; } = null!;
    public string ToUid { get; set; } = null!;
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
