namespace Domain.Entities;

public class SalesOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public Guid SolutionId { get; set; }
    public decimal Price { get; set; }           // Can be negative (discount line)
    public DateOnly? StartDate { get; set; }     // Subscription start date
    public DateOnly? EndDate { get; set; }       // Subscription end date
    public string? Note { get; set; }

    // Navigation
    public SalesOrder SalesOrder { get; set; } = null!;
    public TechSolution Solution { get; set; } = null!;
}
