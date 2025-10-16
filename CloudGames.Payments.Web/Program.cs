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

// Autenticação é gerenciada pelo API Management (APIM) em produção
// Nenhum middleware de autenticação é necessário no microserviço
Log.Information("Autenticação: Desabilitada - APIM gerencia autenticação e autorização");
Log.Information("Segurança: Todos os endpoints são acessíveis sem validação JWT neste microserviço");

builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
// REMOVIDO: builder.Services.AddJwtAuthentication(config); - APIM gerencia autenticação
builder.Services.AddObservability(config);
builder.Services.AddPaymentsPersistence(config);
builder.Services.AddPaymentsMessaging(config);

// Registra serviço de confirmação de pagamento em background
builder.Services.AddHostedService<CloudGames.Payments.Web.Services.PaymentConfirmationService>();

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
// REMOVIDO: app.UseAuthentication(); - APIM gerencia autenticação
// REMOVIDO: app.UseAuthorization(); - APIM gerencia autorização

app.MapGet("/health", () => Results.Ok("ok"));
app.MapGet("/", (HttpContext ctx) => Results.Redirect("/swagger"));

// Usa controllers MVC (evita endpoints mínimos duplicados)
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host encerrado inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
