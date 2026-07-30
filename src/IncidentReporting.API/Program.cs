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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using IncidentReporting.API.Services;
using IncidentReporting.Application.AI;
using IncidentReporting.Infrastructure.AI;
using IncidentReporting.Infrastructure.Integrations;
using IncidentReporting.Application.Common.Settings;

var builder = WebApplication.CreateBuilder(args);
    var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
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

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Keep this removed for now to avoid HTTPS warning
// app.UseHttpsRedirection();
app.UseCors("FrontendCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();