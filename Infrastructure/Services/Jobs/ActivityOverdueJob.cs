using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Jobs;

public class ActivityOverdueJob(
    IAppDbContext context,
    INotificationService notificationService,
    ILogger<ActivityOverdueJob> logger)
{
    public async Task RunAsync()
    {
        logger.LogInformation("Starting ActivityOverdueJob...");

        var now = DateTime.UtcNow;

        // Query active (not completed) task activities where due date is in the past
        var overdueTasks = await context.Activities
            .Include(a => a.Company)
            .Where(a => a.Type == ActivityType.Task && !a.IsCompleted && a.DueDate.HasValue && a.DueDate.Value < now)
            .ToListAsync();

        foreach (var task in overdueTasks)
        {
            var alreadyNotified = await context.Notifications.AnyAsync(n =>
                n.Type == NotificationType.ActivityOverdue &&
                n.EntityId == task.Id.ToString());

            if (alreadyNotified)
            {
                // To avoid spamming, only notify once when a task becomes overdue
                continue;
            }

            var company = task.Company;
            var recipientId = company.AssignedToUserId ?? task.CreatedByUserId;

            var title = "Overdue Task Alert";
            var body = $"The task '{task.Note}' for company '{company.EnglishName}' was due on {task.DueDate.Value:yyyy-MM-dd HH:mm} UTC and is now overdue.";

            try
            {
                await notificationService.SendAsync(
                    recipientId,
                    title,
                    body,
                    NotificationType.ActivityOverdue,
                    "Activity",
                    task.Id.ToString());
                
                logger.LogInformation("Sent overdue task notification for activity {ActivityId} to user {UserId}", task.Id, recipientId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send overdue task notification for activity {ActivityId} to user {UserId}", task.Id, recipientId);
            }
        }

        logger.LogInformation("Finished ActivityOverdueJob.");
    }
}
