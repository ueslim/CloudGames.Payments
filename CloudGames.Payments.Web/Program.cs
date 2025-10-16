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

builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(config);
builder.Services.AddObservability(config);
builder.Services.AddPaymentsPersistence(config);
builder.Services.AddPaymentsMessaging(config);

var app = builder.Build();

await app.InitializeDatabasesAsync();

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
