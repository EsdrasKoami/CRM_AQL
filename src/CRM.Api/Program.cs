using CRM.Business.Services;
using CRM.DataAccess.Context;
using CRM.DataAccess.Repositories;
using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using CRM.Logging;
using CRM.Messaging.Messages;
using CRM.Messaging.Options;
using CRM.Messaging.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// ── Logging quotidien BEYYYYMMDD.log ────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDailyFileLogger(opts =>
{
    opts.LogDirectory = builder.Configuration["DailyLog:Directory"] ?? "logs";
    opts.MinLevel = Enum.Parse<LogLevel>(
        builder.Configuration["DailyLog:MinLevel"] ?? "Information");
});

// ── Entity Framework Core (SQL Server) ──────────────────────────────────
builder.Services.AddDbContext<CrmDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("CrmDb")));

// ── Repositories (Data Access — seule couche qui touche la BD) ──────────────
builder.Services.AddScoped<IClientRepository,      ClientRepository>();
builder.Services.AddScoped<IContratRepository,     ContratRepository>();
builder.Services.AddScoped<IItemContratRepository, ItemContratRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// ── Services Métier ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IContratValidationService, ContratValidationService>();
builder.Services.AddScoped<ICreditControlService,     CreditControlService>();
builder.Services.AddScoped<IFacturationService,       FacturationService>();
builder.Services.AddScoped<IJitQuotaService>(sp =>
    new JitQuotaService(
        sp.GetRequiredService<IItemContratRepository>(),
        sp.GetRequiredService<ITransactionRepository>(),
        sp.GetRequiredService<ILogger<JitQuotaService>>(),
        builder.Configuration.GetConnectionString("CrmDb")!));

// ── File de messages (RabbitMQ) ─────────────────────────────────────────────
builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection(MessageQueueOptions.Section));

builder.Services.AddHostedService<MqBackgroundService>();

// ── JWT Authentication ───────────────────────────────────────────────────────
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]
    ?? throw new InvalidOperationException("Jwt:Key manquant dans appsettings."));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtConfig["Issuer"],
            ValidAudience            = jwtConfig["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(key),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Agent",            p => p.RequireRole("Agent", "DirecteurFinances"));
    opts.AddPolicy("DirecteurFinances", p => p.RequireRole("DirecteurFinances"));
});

// ── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "CRM AQL 420-454-RI API",
        Version = "v1",
        Description = "Backend CRM pour système manufacturier Just In Time"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name   = "Authorization",
        Type   = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In     = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Toujours activer Swagger pour la démonstration (même en prod)
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation(
    "=== CRM AQL 420-454-RI démarré le {Date:yyyy-MM-dd HH:mm:ss} UTC ===",
    DateTime.UtcNow);

try
{
    CRM.Api.Extensions.DbInitializer.Initialize(app.Services);
    app.Logger.LogInformation("Base de données LocalDB initialisée avec succès.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Erreur lors de l'initialisation de la base de données.");
}

app.Run("http://localhost:5000");
