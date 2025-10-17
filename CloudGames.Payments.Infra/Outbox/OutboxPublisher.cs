using System.Text.Json;
using Azure.Storage.Queues;
using CloudGames.Payments.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudGames.Payments.Infra.Outbox;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly QueueClient _queueClient;

    public OutboxPublisher(IServiceProvider services, ILogger<OutboxPublisher> logger, QueueClient queueClient)
    {
        _services = services;
        _logger = logger;
        _queueClient = queueClient;
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
                    .Where(x => x.ProcessedAt == null)
                    .OrderBy(x => x.OccurredAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                // Cria a fila se não existir
                await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

                foreach (var msg in pending)
                {
                    try
                    {
                        await _queueClient.SendMessageAsync(msg.Payload, stoppingToken);
                        msg.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.AttemptCount += 1;
                        _logger.LogError(ex, "Falha ao publicar mensagem outbox {Id}", msg.Id);
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
