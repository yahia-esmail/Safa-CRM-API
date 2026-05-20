using Domain.Enums;

using Domain.Common;

namespace Domain.Entities;

public class SalesOrder : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;    // INV/2026/00393
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? OrderReference { get; set; }
    public string SaleOrderType { get; set; } = "New";           // New | Renewal | Upgrade
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public string? PaymentMethod { get; set; }                   // Cash | Online | Transfer
    public Currency OriginalCurrency { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal UsdRateAtTime { get; set; }                   // Exchange rate snapshot
    public decimal UsdAmount { get; set; }                       // Calculated & stored
    public string? Attachment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // D-3 — Missing fields
    public DateTime? UpdatedAt { get; set; }
    public string? CancellationReason { get; set; }  // Why was the order cancelled?
    public DateTime? ConfirmedAt { get; set; }        // When was it confirmed?
    public DateTime? CancelledAt { get; set; }        // When was it cancelled?

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public SystemUser CreatedBy { get; set; } = null!;
    public ICollection<SalesOrderItem> Items { get; set; } = [];
    public Invoice? Invoice { get; set; }
}
