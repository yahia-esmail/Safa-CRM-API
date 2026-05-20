using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public class SanitizationInterceptor : SaveChangesInterceptor
{
    private readonly HtmlSanitizer _sanitizer;

    public SanitizationInterceptor()
    {
        _sanitizer = new HtmlSanitizer();
        // Customize sanitizer here if needed
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SanitizeEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SanitizeEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SanitizeEntities(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(string) && property.CurrentValue is string value && !string.IsNullOrEmpty(value))
                {
                    var sanitizedValue = _sanitizer.Sanitize(value).Trim();

                    // Replace specific problem characters if the sanitizer misses any
                    // sanitizedValue = sanitizedValue.Replace("'", "''"); // Optional SQL injection prevention (though EF core already parametrizes)
                    
                    if (sanitizedValue != value)
                    {
                        property.CurrentValue = sanitizedValue;
                    }
                }
            }
        }
    }
}
