namespace Domain.Entities;

/// <summary>
/// D-5 — A label/tag that can be applied to companies for filtering and classification.
/// Tags are user-created (not system-defined). Any Sales Rep can create. Admin can delete any.
/// </summary>
public class CompanyTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;   // e.g. "VIP", "Hot Lead"
    public string Color { get; set; } = "#3B82F6";     // Hex color for UI badge
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SystemUser CreatedBy { get; set; } = null!;
    public ICollection<CompanyTagAssignment> Assignments { get; set; } = [];
}

/// <summary>
/// D-5 — Join table: Company ↔ CompanyTag (many-to-many).
/// </summary>
public class CompanyTagAssignment
{
    public Guid CompanyId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public CompanyTag Tag { get; set; } = null!;
}
