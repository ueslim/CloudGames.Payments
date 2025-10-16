using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace CloudGames.Payments.Web.Configurations;

public static class ObservabilityConfig
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service.name", "CloudGames.Payments")
            .WriteTo.Console()
            .CreateLogger();

        var otlpEndpoint = configuration["OTLP:Endpoint"]; // e.g., http://otel-collector:4317

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CloudGames.Payments"))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddEntityFrameworkCoreInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
                else
                {
                    t.AddOtlpExporter();
                }
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddRuntimeInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
                else
                {
                    m.AddOtlpExporter();
                }
            });

        // Note: Serilog handles application logs; OTel log exporter is disabled to avoid package conflicts.
        return services;
    }
}
