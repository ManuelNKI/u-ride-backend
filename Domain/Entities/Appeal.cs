namespace Domain.Entities;

public enum AppealStatus
{
    Pending,
    Approved,
    Rejected
}

public class Appeal : AuditableEntity
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public string Reason { get; set; } = null!;
    
    public AppealStatus Status { get; set; }

    public DateTime? ProcessedAt { get; set; }
    
    public string? ProcessedById { get; set; }
    public User? ProcessedBy { get; set; }

    public string? AdminNotes { get; set; }
}
