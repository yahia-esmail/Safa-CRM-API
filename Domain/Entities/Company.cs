using Domain.Enums;

namespace Domain.Entities;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;           // E.164 format
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
    public int? SafaKey { get; set; }
    public string AccountType { get; set; } = "New";            // New | Existing
    public string? ContractAttachment { get; set; }
    public string? ApplicationForm { get; set; }
    public Stage Stage { get; set; } = Stage.LeadOpportunity;
    public string? LeadSource { get; set; }                     // Facebook | LinkedIn | ...
    public string LeadStatus { get; set; } = "UnReached";       // Reached | UnReached
    public decimal? ExpectedRevenue { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SystemUser? AssignedTo { get; set; }
    public ICollection<CompanyContact> Contacts { get; set; } = [];
    public ICollection<Activity> Activities { get; set; } = [];
    public ICollection<SalesOrder> SalesOrders { get; set; } = [];
}
