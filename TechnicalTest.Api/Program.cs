using System.Text;
using Amazon.EventBridge;
using Amazon.SimpleNotificationService;
using HealthChecks.NpgSql;
using TechnicalTest.Api.Services;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Exceptions;
using TechnicalTest.Infrastructure;
using TechnicalTest.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TechnicalTest.Api.Swagger;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TechnicalTest API",
        Version = "v1",
        Description = "Documentación OpenAPI para los servicios expuestos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Ingrese el token JWT con el formato: Bearer {token}.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

// AWS EventBridge Configuration
var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";
builder.Services.AddSingleton<IAmazonEventBridge>(sp =>
{
    var config = new AmazonEventBridgeConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
    };
    return new AmazonEventBridgeClient(config);
});

// AWS SNS Configuration
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
{
    var config = new AmazonSimpleNotificationServiceConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
    };
    return new AmazonSimpleNotificationServiceClient(config);
});

// Register services
builder.Services.AddSingleton<IEventBridgeService, EventBridgeService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<ISnsSubscriptionService, SnsSubscriptionService>();
builder.Services.AddScoped<IProductManagementService, ProductManagementService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBankBranchService, BankBranchService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? throw new InvalidOperationException("La configuración JWT es obligatoria.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var problemDetails = exception switch
        {
            NotFoundException notFoundException => new ProblemDetails
            {
                Title = "Recurso no encontrado",
                Detail = notFoundException.Message,
                Status = StatusCodes.Status404NotFound
            },
            DomainException domainException when domainException.Message.Contains("Credenciales inválidas", StringComparison.OrdinalIgnoreCase) => new ProblemDetails
            {
                Title = "Error de autenticación",
                Detail = domainException.Message,
                Status = StatusCodes.Status401Unauthorized
            },
            DomainException domainException => new ProblemDetails
            {
                Title = "Regla de negocio incumplida",
                Detail = domainException.Message,
                Status = StatusCodes.Status400BadRequest
            },
            _ => new ProblemDetails
            {
                Title = "Error inesperado",
                Detail = "Ocurrió un error al procesar la solicitud.",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TechnicalTest API v1");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
