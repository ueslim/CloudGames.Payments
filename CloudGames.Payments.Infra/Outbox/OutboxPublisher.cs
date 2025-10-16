using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CloudGames.Payments.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudGames.Payments.Infra.Outbox;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly ServiceBusClient _busClient;

    public OutboxPublisher(IServiceProvider services, ILogger<OutboxPublisher> logger, ServiceBusClient busClient)
    {
        _services = services;
        _logger = logger;
        _busClient = busClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var pending = await db.OutboxMessages
                    .Where(x => x.ProcessedOn == null)
                    .OrderBy(x => x.OccurredOn)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var topic = configuration["ServiceBus:Topic"] ?? "payments-events";
                await using var sender = _busClient.CreateSender(topic);

                foreach (var msg in pending)
                {
                    try
                    {
                        var sbMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(msg.Payload))
                        {
                            Subject = msg.Type,
                            ContentType = "application/json"
                        };
                        var activity = Activity.Current;
                        if (activity != null)
                        {
                            sbMessage.ApplicationProperties["traceparent"] = activity.Id;
                            if (activity.TraceStateString != null)
                                sbMessage.ApplicationProperties["tracestate"] = activity.TraceStateString;
                        }
                        await sender.SendMessageAsync(sbMessage, stoppingToken);
                        msg.ProcessedOn = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.AttemptCount += 1;
                        _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
