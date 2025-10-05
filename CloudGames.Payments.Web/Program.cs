using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Azure.Messaging.ServiceBus;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Handlers;
using CloudGames.Payments.Application.Queries;
using CloudGames.Payments.Domain.Events;
using CloudGames.Payments.Domain.Repositories;
using CloudGames.Payments.Infra.EventStore;
using CloudGames.Payments.Infra.Outbox;
using CloudGames.Payments.Infra.Persistence;
using CloudGames.Payments.Infra.Repositories;
using CloudGames.Payments.Web.Configurations;
using CloudGames.Payments.Web.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Serilog bootstrap (final config in ObservabilityConfig)
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins("http://localhost:4200", "https://*.azurestaticapps.net")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddDbContext<PaymentsDbContext>(opt =>
    opt.UseSqlServer(config.GetConnectionString("PaymentsDb")));

builder.Services.AddDbContext<EventStoreSqlContext>(opt =>
    opt.UseSqlServer(config.GetConnectionString("PaymentsDb")));

// Modular setup
builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(config);
builder.Services.AddObservability(config);

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<InitiatePaymentHandler>());

// DI
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEventStore, EventStoreRepository>();

// Service Bus client with retries (conditional)
var sbConn = config.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConn))
{
    var sbClient = new ServiceBusClient(sbConn, new ServiceBusClientOptions
    {
        RetryOptions = new ServiceBusRetryOptions
        {
            Mode = ServiceBusRetryMode.Exponential,
            MaxRetries = 5,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30)
        }
    });
    builder.Services.AddSingleton(sbClient);
    builder.Services.AddHostedService<OutboxPublisher>();
}
else
{
    Log.Warning("ServiceBus connection string is not configured. Outbox publisher is disabled.");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationMiddleware>();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("ok"));

// Use MVC controllers (avoid duplicate minimal endpoints)
app.MapControllers();

app.Run();
