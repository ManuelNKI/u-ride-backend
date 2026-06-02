namespace Application.DTOs.Appeals;

public class CreateAppealDto
{
    public string Reason { get; set; } = null!;
}

public class AppealResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNotes { get; set; }
}

public class ProcessAppealDto
{
    public bool Approve { get; set; }
    public string? AdminNotes { get; set; }
}
