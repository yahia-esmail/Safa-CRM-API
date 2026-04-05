namespace Domain.Entities;

public class ImportLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;        // Companies | Orders
    public string Status { get; set; } = string.Empty;      // Success | PartialSuccess | Failed
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public string? ErrorDetails { get; set; }               // JSON
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SystemUser UploadedBy { get; set; } = null!;
}
