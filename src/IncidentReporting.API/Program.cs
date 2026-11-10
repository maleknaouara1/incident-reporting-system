using IncidentReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using IncidentReporting.Application.Incidents.Validators;
using IncidentReporting.Application.Incidents.Services;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Infrastructure.Identity;
using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;
using IncidentReporting.API.Services;
using IncidentReporting.API.ExceptionHandling;
using IncidentReporting.API.Startup;
using IncidentReporting.Application.AI;
using IncidentReporting.Infrastructure.AI;
using IncidentReporting.Infrastructure.Integrations;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.API.Filters;
using Microsoft.AspNetCore.RateLimiting;
using IncidentReporting.Application.Incidents.Attachments;
using IncidentReporting.Infrastructure.Email;
using IncidentReporting.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short (minimum 32 characters). " +
        "Set it via `dotnet user-secrets set \"Jwt:Key\" \"<value>\"` (dev) or an environment variable (prod) " +
        "— never commit it to appsettings.json.");
}

builder.Services.AddControllers();

builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection(FrontendSettings.SectionName));
var frontendSettings = builder.Configuration.GetSection(FrontendSettings.SectionName).Get<FrontendSettings>() ?? new FrontendSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins(frontendSettings.BaseUrl.TrimEnd('/'))
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Lets the browser send the HttpOnly refresh token cookie to /api/auth
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        ClockSkew = TimeSpan.FromSeconds(30),

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Partitioned by client IP so one noisy/abusive client can't exhaust the
    // auth quota for every other user (a plain AddFixedWindowLimiter here would
    // create a single bucket shared by all callers).
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IIncidentAttachmentRepository, IncidentAttachmentRepository>();
builder.Services.AddScoped<IIncidentAttachmentService, IncidentAttachmentService>();

builder.Services.Configure<AttachmentSettings>(builder.Configuration.GetSection(AttachmentSettings.SectionName));
builder.Services.AddScoped<IAttachmentStorage, LocalFileAttachmentStorage>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddScoped<MockEmailSender>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<IEmailSender>(provider =>
{
    var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmtpSettings>>().Value;
    return settings.IsMockEnabled
        ? provider.GetRequiredService<MockEmailSender>()
        : provider.GetRequiredService<SmtpEmailSender>();
});

builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));
builder.Services.AddScoped<MockAiIncidentAnalyzer>();
builder.Services.AddHttpClient<OpenAiIncidentAnalyzer>();

builder.Services.AddScoped<IAiIncidentAnalyzer>(provider =>
{
    var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiSettings>>().Value;
    return settings.IsMockEnabled
        ? provider.GetRequiredService<MockAiIncidentAnalyzer>()
        : provider.GetRequiredService<OpenAiIncidentAnalyzer>();
});
builder.Services.Configure<N8nSettings>(builder.Configuration.GetSection(N8nSettings.SectionName));
builder.Services.AddHttpClient<IN8nService, N8nService>();
builder.Services.Configure<EmailIntakeSettings>(builder.Configuration.GetSection(EmailIntakeSettings.SectionName));
builder.Services.AddScoped<IEmailIncidentService, EmailIncidentService>();
builder.Services.AddScoped<EmailIntakeApiKeyFilter>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateIncidentRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Incident Reporting API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token only. Example: eyJhbGciOi..."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var emailIntakeSettings = startupScope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailIntakeSettings>>().Value;

    if (emailIntakeSettings.IsEnabled)
    {
        if (string.IsNullOrWhiteSpace(emailIntakeSettings.ApiKey))
        {
            throw new InvalidOperationException(
                "EmailIntake is enabled but no API key is configured. Set EmailIntake:ApiKey via an environment variable or user secrets.");
        }

        var userRepository = startupScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var systemUser = await userRepository.GetByIdAsync(emailIntakeSettings.SystemUserId);

        if (systemUser is null || !systemUser.IsActive)
        {
            throw new InvalidOperationException(
                "EmailIntake is enabled but EmailIntake:SystemUserId does not resolve to an existing, active user.");
        }
    }

    await AdminSeeder.SeedAsync(startupScope.ServiceProvider, app.Configuration);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("FrontendCorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
