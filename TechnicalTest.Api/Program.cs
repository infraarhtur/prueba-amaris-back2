using TechnicalTest.Api.Services;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Exceptions;
using TechnicalTest.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

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
});
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddScoped<IFundManagementService, FundManagementService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var problemDetails = exception switch
        {
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
app.MapControllers();

app.Run();
