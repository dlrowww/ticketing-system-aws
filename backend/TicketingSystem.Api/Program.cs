using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Quartz;

using Serilog;
using Serilog.Events;

using System.Text;

using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Services.Tickets;
using TicketingSystem.Api.Services.Localization;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Services.Reporting;
using TicketingSystem.Api.Services.Users.Admin;
using TicketingSystem.Api.Services.Categories;
using TicketingSystem.Api.Validators;
using TicketingSystem.Api.Validators.Users;
using TicketingSystem.Api.Validators.Categories;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.Infrastructure.Email;
using TicketingSystem.Api.Infrastructure.RateLimiting;
using TicketingSystem.Api.Utils;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/ticketing-system-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024, // 50 MB
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Ticketing System API");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Ticketing System API", Version = "v1" });
    
    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register AppDbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS configuration - production-ready with explicit origins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:3000" }
        : Array.Empty<string>());

if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "CORS AllowedOrigins must be configured in production. " +
        "Add Cors:AllowedOrigins array in appsettings.json or set CORS__ALLOWEDORIGINS__0=https://your-domain.com");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

builder.Services.Configure<CurrentUserOptions>(builder.Configuration.GetSection(CurrentUserOptions.SectionName));
builder.Services.Configure<TicketOptions>(builder.Configuration.GetSection(TicketOptions.SectionName));
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection(FileUploadOptions.SectionName));
builder.Services.Configure<CommentOptions>(builder.Configuration.GetSection(CommentOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorage, PostgresFileStorage>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Register EF Core services
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITicketHistoryService, TicketHistoryService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<ITicketValidator, TicketValidator>();
builder.Services.AddScoped<ITicketUpdateValidator, TicketUpdateValidator>();
builder.Services.AddScoped<ITicketAssignmentValidator, TicketAssignmentValidator>();
builder.Services.AddScoped<IAttachmentValidator, AttachmentValidator>();
builder.Services.AddScoped<ICommentValidator, CommentValidator>();
builder.Services.AddScoped<IUserValidator, UserValidator>();
builder.Services.AddScoped<ICategoryValidator, CategoryValidator>();

// Localization and Email services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 60L * 1024 * 1024;
    o.MultipartHeadersLengthLimit = 64 * 1024;
});

// Email configuration with validation
builder.Services.Configure<TicketingSystem.Api.Infrastructure.Email.EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddOptions<TicketingSystem.Api.Infrastructure.Email.EmailOptions>()
    .Bind(builder.Configuration.GetSection("Email"))
    .ValidateDataAnnotations()
    .Validate(o => o.Validate(), "Email configuration is invalid")
    .ValidateOnStart();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db", "ready" });

// Quartz.NET - Scheduled Jobs (Automated Backups)
builder.Services.AddQuartz(q =>
{
    // Configure database backup job
    var backupJobKey = new JobKey("DatabaseBackupJob");
    q.AddJob<TicketingSystem.Api.Jobs.DatabaseBackupJob>(opts => opts
        .WithIdentity(backupJobKey)
        .StoreDurably()); // Allow job without trigger when backups are disabled
    
    // Schedule: Daily at 2:00 AM (configurable via appsettings)
    var cronSchedule = builder.Configuration["Backup:CronSchedule"] ?? "0 0 2 * * ?"; // Default: 2 AM daily
    var enableBackupSchedule = builder.Configuration.GetValue<bool>("Backup:Enabled", true);
    
    if (enableBackupSchedule)
    {
        q.AddTrigger(opts => opts
            .ForJob(backupJobKey)
            .WithIdentity("DatabaseBackupTrigger")
            .WithCronSchedule(cronSchedule)
            .WithDescription($"Automated database backup schedule: {cronSchedule}"));
        
        Log.Information("Automated backup job scheduled with cron: {CronSchedule}", cronSchedule);
    }
    else
    {
        Log.Information("Automated backup job is DISABLED via configuration");
    }
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// JWT token with security validation
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? Environment.GetEnvironmentVariable("JWT__KEY")
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured. Set Jwt:Key in appsettings.json or JWT__KEY environment variable. "
        + "Generate a secure key with: openssl rand -base64 64");

if (jwtKey.Length < 64)
{
    throw new InvalidOperationException(
        $"JWT secret key is too short ({jwtKey.Length} chars). Minimum 64 characters required for security. "
        + "Generate a secure key with: openssl rand -base64 64");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TicketingSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TicketingSystemFrontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        // Allow reading JWT from cookie
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.ContainsKey("auth_token"))
            {
                ctx.Token = ctx.Request.Cookies["auth_token"];
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Validate translation files exist
    var webEnv = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var localizationPath = Path.Combine(webEnv.ContentRootPath, "Localization");
    var plTranslationsPath = Path.Combine(localizationPath, "translations.pl.json");
    var enTranslationsPath = Path.Combine(localizationPath, "translations.en.json");
    
    if (!File.Exists(plTranslationsPath))
        throw new FileNotFoundException($"Polish translation file not found: {plTranslationsPath}");
    if (!File.Exists(enTranslationsPath))
        throw new FileNotFoundException($"English translation file not found: {enTranslationsPath}");
    
    // Validate email templates folder exists
    var emailOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TicketingSystem.Api.Infrastructure.Email.EmailOptions>>();
    var templatesPath = Path.Combine(webEnv.ContentRootPath, emailOptions.Value.TemplatesPath);
    if (!Directory.Exists(templatesPath))
        throw new DirectoryNotFoundException($"Email templates directory not found: {templatesPath}");

    // Bootstrap production admin (if configured via environment variables)
    var prodAdminEmail = builder.Configuration["ADMIN_EMAIL"];
    var prodAdminPassword = builder.Configuration["ADMIN_PASSWORD"];

    if (!string.IsNullOrWhiteSpace(prodAdminEmail) && !string.IsNullOrWhiteSpace(prodAdminPassword))
    {
        var normalizedEmail = prodAdminEmail.Trim().ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            db.Users.Add(new User
            {
                Name = "System Administrator",
                Email = prodAdminEmail,
                PasswordHash = PasswordHasher.Hash(prodAdminPassword),
                RoleId = UserRole.Admin,
                IsActive = true,
                CategoryId = null
            });
            await db.SaveChangesAsync();
            Log.Information("Production admin user created: {Email}", prodAdminEmail);
        }
    }

    // Seed demo data in Development OR Production (controlled by SEED_DEMO_DATA flag)
    // This allows seeding in any environment when explicitly enabled via configuration
    var seedDemoData = builder.Configuration.GetValue<bool>("SEED_DEMO_DATA", false);
    
    if (seedDemoData)
    {
        var environment = env.EnvironmentName;
        Log.Information("SEED_DEMO_DATA=true detected in {Environment} environment. Starting demo data seeding (49 tickets)...", environment);
        await TicketingSystem.Api.SeedData.DemoDataSeeder.SeedAsync(scope.ServiceProvider);
        Log.Information("Demo data seeding completed successfully with 49 sample tickets");
    }
    else
    {
        Log.Information("SEED_DEMO_DATA not enabled. Database will start empty. Set SEED_DEMO_DATA=true to seed 49 demo tickets.");
    }
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Detailed stack traces in dev *and* JSON envelope for FE
    app.UseDeveloperExceptionPage();
}
else
{
    // Production: HTTPS enforcement with HSTS
    app.UseHsts(); // HTTP Strict Transport Security
}

// Global exception handler
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("GlobalExceptionHandler");

        var pd = ProblemDetailsExtensions.FromException(context, feature?.Error ?? new Exception("Unknown error"), logger);
        // log with traceId for correlation
        logger.LogError(feature?.Error, "Unhandled error, traceId={TraceId}", context.TraceIdentifier);

        if (pd is ValidationProblemDetails vpd)
        {
            await vpd.ToResult().ExecuteAsync(context);
            return;
        }

        await pd.ToResult().ExecuteAsync(context);
    });
});

app.UseHttpsRedirection();

// Request logging with Serilog
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
        if (elapsed > 1000) return LogEventLevel.Warning; // Slow requests
        return LogEventLevel.Information;
    };
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
        
        var user = httpContext.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(user))
        {
            diagnosticContext.Set("User", user);
        }
    };
});

// Rate limiting middleware (before authentication) - disabled in Testing environment or when explicitly disabled
var disableRateLimiting = app.Configuration.GetValue<bool>("DISABLE_RATE_LIMITING", false);
if (!app.Environment.IsEnvironment("Testing") && !disableRateLimiting)
{
    app.UseMiddleware<RateLimitingMiddleware>();
}

// Enable CORS policy
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Map controllers (attribute routing)
app.MapControllers();

Log.Information("Ticketing System API started successfully");
app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;