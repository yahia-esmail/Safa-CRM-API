using API.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Application;
using Hangfire;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Services.ExchangeRate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Application.Common.Interfaces.ICurrentUserService, API.Services.CurrentUserService>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Login endpoint: 5 attempts per minute per IP
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // General API: 100 requests per minute per user
    options.AddSlidingWindowLimiter("ApiPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 4;
        opt.QueueLimit = 10;
    });
});

// Clean Architecture Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire — registered in API where Hangfire.AspNetCore is available
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS — allow frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5500",
                "http://127.0.0.1:5500",
                "https://localhost:3000",
                "https://127.0.0.1:3000",
                "https://localhost:7221",
                "http://localhost:8080",
                "http://192.168.9.42:8080",
                "https://ac20a44f-f3d2-4014-8221-55a9cc1a1d8d-00-3l6vhnt16qxzc.kirk.replit.dev",
                "https://id-preview--3d7aab4f-5a46-4e62-b8a6-573062073371.lovable.app",
                "http://safa-crm.tryasp.net",
                "https://safa-crm.tryasp.net"

            )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ─── App Pipeline ───────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Safa CRM API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}


app.UseMiddleware<GlobalExceptionMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    bool isSwaggerOrScalar = context.Request.Path.Value != null && 
        (context.Request.Path.Value.Contains("/scalar", StringComparison.OrdinalIgnoreCase) || 
         context.Request.Path.Value.Contains("/openapi", StringComparison.OrdinalIgnoreCase));

    if (!isSwaggerOrScalar)
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    }
    else
    {
        // Allow framing for scalar if needed
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    }

    await next();
});

app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseHsts();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Admin only in production)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [] // Open for dev — restrict in production
});

// ─── Hangfire Recurring Jobs ────────────────────────────────────
RecurringJob.AddOrUpdate<ExchangeRateService>(
    "fetch-exchange-rates",
    service => service.FetchAndSaveRatesAsync(),
    "0 6 * * *"  // Every day at 6:00 AM UTC
);

RecurringJob.AddOrUpdate<Infrastructure.Services.Jobs.RenewalReminderJob>(
    "renewal-reminders",
    job => job.RunAsync(),
    "0 8 * * *"  // Every day at 8:00 AM UTC
);

RecurringJob.AddOrUpdate<Infrastructure.Services.Jobs.ActivityOverdueJob>(
    "activity-overdue-checks",
    job => job.RunAsync(),
    "0 8 * * *"  // Every day at 8:00 AM UTC
);

app.MapControllers();

// Seed the database with the initial Admin user
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
