using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Application.Services;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Infrastructure.Data;
using PawConnect.Api.Infrastructure.Identity;
using PawConnect.Api.Infrastructure.OpenApi;
using PawConnect.Api.Middleware;
using PawConnect.Api.Infrastructure.FileStorage;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 4, 32))));

// ---------- Identity ----------
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------- JWT authentication ----------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

if (jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be at least 32 characters. Set it with 'dotnet user-secrets set'.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;

                if (!Guid.TryParse(subject, out var userId))
                {
                    context.Fail("Invalid subject claim.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                var status = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => (AccountStatus?)u.Status)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (status is null || status == AccountStatus.Disabled)
                {
                    context.Fail("The account is not available.");
                }
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.ManagePets, policy => policy.RequireRole(Roles.Rescuer))
    .AddPolicy(Policies.ModeratePets, policy => policy.RequireRole(Roles.Rescuer, Roles.Admin))
    .AddPolicy(Policies.ApplyForPets, policy => policy.RequireRole(Roles.Adopter))
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));

// ---------- Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(Policies.AuthRateLimit, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy(Policies.SubmitRateLimit, httpContext =>
{
var key = httpContext.User.FindFirst("sub")?.Value
      ?? httpContext.Connection.RemoteIpAddress?.ToString()
      ?? "unknown";

return RateLimitPartition.GetFixedWindowLimiter(
key,
_ => new FixedWindowRateLimiterOptions
{
    PermitLimit = 10,
    Window = TimeSpan.FromHours(1),
    QueueLimit = 0
});
});

    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        return ValueTask.CompletedTask;
    };
});

// ---------- MVC, JSON, OpenAPI ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- Errors ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Application services ----------
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountAdminService, AccountAdminService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationReviewService, ApplicationReviewService>();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IPetPhotoService, PetPhotoService>();
builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// ---------- Startup data ----------
using (var identityScope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAsync(identityScope.ServiceProvider);
    await IdentitySeeder.SeedAdminAsync(identityScope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    using var devScope = app.Services.CreateScope();

    var ownerId = await IdentitySeeder.SeedDemoRescuerAsync(devScope.ServiceProvider, app.Configuration);

    if (ownerId is not null)
    {
        var db = devScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DevelopmentSeeder.SeedAsync(db, ownerId.Value);
    }
    else
    {
        app.Logger.LogWarning(
            "Demo pets were not seeded. Set 'Seed:DemoRescuerPassword' in user secrets to enable them.");
    }
}

// ---------- Request pipeline (order matters) ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PawConnect API v1");
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();


app.Run();