using CloudGames.Payments.Web.Configurations;
using CloudGames.Payments.Web.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins("http://localhost:4200", "https://*.azurestaticapps.net")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Authentication is handled by API Management (APIM) in production
// No authentication middleware is required in the microservice
Log.Information("Authentication: Disabled - APIM handles authentication and authorization");
Log.Information("Security: All endpoints are accessible without JWT validation in this microservice");

builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
// REMOVED: builder.Services.AddJwtAuthentication(config); - APIM handles authentication
builder.Services.AddObservability(config);
builder.Services.AddPaymentsPersistence(config);
builder.Services.AddPaymentsMessaging(config);

var app = builder.Build();

await CloudGames.Payments.Infra.Data.DatabaseInitializer.EnsureDatabaseMigratedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationMiddleware>();
app.UseCors("frontend");
// REMOVED: app.UseAuthentication(); - APIM handles authentication
// REMOVED: app.UseAuthorization(); - APIM handles authorization

app.MapGet("/health", () => Results.Ok("ok"));
app.MapGet("/", (HttpContext ctx) => Results.Redirect("/swagger"));

// Use MVC controllers (avoid duplicate minimal endpoints)
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
