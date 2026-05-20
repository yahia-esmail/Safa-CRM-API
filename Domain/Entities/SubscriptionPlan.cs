namespace Domain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // e.g. Basic, Pro, Enterprise
    public int MaxAdmins { get; set; }
    public int MaxSales { get; set; }
    public decimal Price { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Tenant> Tenants { get; set; } = [];
}
