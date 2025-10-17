using CloudGames.Payments.Domain.Repositories;
using CloudGames.Payments.Domain.Events;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudGames.Payments.Web.Services;

public class PaymentConfirmationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentConfirmationService> _logger;

    public PaymentConfirmationService(
        IServiceProvider serviceProvider,
        ILogger<PaymentConfirmationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Confirmação de Pagamento iniciado - simulando gateway de pagamento");

        // Aguarda um pouco antes de começar a processar
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken); // Processa a cada 5 segundos

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider
                    .GetRequiredService<IPaymentRepository>();
                var eventStore = scope.ServiceProvider
                    .GetRequiredService<IEventStore>();

                var pendingPayments = await repository
                    .GetPendingPaymentsAsync(stoppingToken);

                var paymentsList = pendingPayments.ToList();
                
                if (!paymentsList.Any())
                    continue;

                foreach (var payment in paymentsList)
                {
                    // Simula processamento do gateway de pagamento (90% de aprovação)
                    var random = new Random();
                    var isApproved = random.Next(100) < 90;

                    if (isApproved)
                    {
                        payment.Approve();
                        _logger.LogInformation(
                            "Pagamento {PaymentId} APROVADO - Usuario: {UserId}, Jogo: {GameId}, Valor: {Amount:C}",
                            payment.Id, payment.UserId, payment.GameId, payment.Amount);
                    }
                    else
                    {
                        payment.Decline("Saldo insuficiente");
                        _logger.LogWarning(
                            "Pagamento {PaymentId} RECUSADO - Usuario: {UserId}, Motivo: Saldo insuficiente",
                            payment.Id, payment.UserId);
                    }

                    // Salva os domain events no EventStore e Outbox (para serem publicados)
                    foreach (var evt in payment.DomainEvents)
                    {
                        var type = evt.GetType().Name;
                        var data = JsonSerializer.Serialize(evt);
                        await eventStore.AppendAsync(new StoredEvent
                        {
                            AggregateId = payment.Id,
                            Type = type,
                            Data = data,
                            OccurredOn = DateTime.UtcNow
                        }, stoppingToken);
                    }
                    payment.ClearDomainEvents();

                    await repository.UpdateAsync(payment, stoppingToken);
                }

                await repository.SaveChangesAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Processados {Count} pagamento(s) pendente(s)", 
                    paymentsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pagamentos pendentes");
            }
        }

        _logger.LogInformation("Serviço de Confirmação de Pagamento parado");
    }
}

