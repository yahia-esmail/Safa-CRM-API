namespace Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    
    // If you want to store a static snapshot link or path
    public string? PdfUrl { get; set; }
}
