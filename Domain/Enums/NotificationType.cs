namespace Domain.Enums;

public enum NotificationType
{
    CompanyAssigned,      // Admin assigned a company to sales rep
    OrderConfirmed,       // A sales order was confirmed
    OrderCancelled,       // An order was cancelled
    RenewalReminder,      // Subscription ending within 30 days
    ActivityOverdue,      // A task/follow-up is overdue
    NewCompanyImported,   // Excel import completed
    StageChanged,         // Company moved to a new stage
    SystemAlert           // Generic admin broadcast
}
