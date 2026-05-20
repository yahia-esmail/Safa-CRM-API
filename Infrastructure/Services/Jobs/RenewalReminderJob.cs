using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Jobs;

public class RenewalReminderJob(
    IAppDbContext context,
    INotificationService notificationService,
    ILogger<RenewalReminderJob> logger)
{
    public async Task RunAsync()
    {
        logger.LogInformation("Starting RenewalReminderJob...");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var date30 = today.AddDays(30);
        var date7 = today.AddDays(7);

        // We query orders with status Confirmed that have items expiring in exactly 30 or 7 days
        var items = await context.SalesOrderItems
            .Include(i => i.SalesOrder)
                .ThenInclude(o => o.Company)
            .Include(i => i.Solution)
            .Where(i => i.SalesOrder.Status == OrderStatus.Confirmed && i.EndDate.HasValue && (i.EndDate == date30 || i.EndDate == date7))
            .ToListAsync();

        foreach (var item in items)
        {
            var company = item.SalesOrder.Company;
            var solution = item.Solution;
            var orderId = item.SalesOrderId;
            var endDate = item.EndDate!.Value;

            int daysRemaining = (endDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
            
            // Check if we already sent a notification for this order and days remaining
            var notificationTitle = daysRemaining == 30 
                ? "Subscription Renewal Reminder (30 Days)" 
                : "Urgent Subscription Renewal Reminder (7 Days)";

            var alreadyNotified = await context.Notifications.AnyAsync(n => 
                n.Type == NotificationType.RenewalReminder && 
                n.EntityId == orderId.ToString() && 
                n.Title == notificationTitle);

            if (alreadyNotified)
            {
                logger.LogInformation("Order {OrderId} already notified for {Days} days renewal. Skipping.", orderId, daysRemaining);
                continue;
            }

            var body = daysRemaining == 30
                ? $"The subscription for solution '{solution.Name}' for company '{company.EnglishName}' is expiring in 30 days on {endDate:yyyy-MM-dd}."
                : $"The subscription for solution '{solution.Name}' for company '{company.EnglishName}' is expiring in 7 days on {endDate:yyyy-MM-dd}.";

            var recipientIds = new List<Guid>();

            // Main recipient is the company's assigned sales rep
            if (company.AssignedToUserId.HasValue)
            {
                recipientIds.Add(company.AssignedToUserId.Value);
            }
            else
            {
                // Fallback to order creator
                recipientIds.Add(item.SalesOrder.CreatedByUserId);
            }

            // For 7-day warning, also notify all admins of the tenant
            if (daysRemaining == 7)
            {
                var admins = await context.Users
                    .Where(u => u.TenantId == item.SalesOrder.TenantId && u.Role == Role.Admin && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    if (!recipientIds.Contains(adminId))
                    {
                        recipientIds.Add(adminId);
                    }
                }
            }

            foreach (var recipientId in recipientIds)
            {
                try
                {
                    await notificationService.SendAsync(
                        recipientId,
                        notificationTitle,
                        body,
                        NotificationType.RenewalReminder,
                        "SalesOrder",
                        orderId.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send renewal notification to user {UserId} for order {OrderId}", recipientId, orderId);
                }
            }
        }

        logger.LogInformation("Finished RenewalReminderJob.");
    }
}
