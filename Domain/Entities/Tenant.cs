namespace Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // Company Name
    public string Industry { get; set; } = string.Empty;
    
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public DateTime SubscriptionStart { get; set; }
    public DateTime SubscriptionEnd { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<SystemUser> Users { get; set; } = [];
    public ICollection<Company> Companies { get; set; } = [];
}
