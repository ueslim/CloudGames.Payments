using Azure.Storage.Queues;
using CloudGames.Payments.Infra.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudGames.Payments.Web.Configurations;

public static class MessagingConfig
{
	public static IServiceCollection AddPaymentsMessaging(this IServiceCollection services, IConfiguration configuration)
	{
		// Usa Azure Storage Queue (mesmo padrão do Users service)
		var storageConn = configuration.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true";
		var queueName = configuration["Queues:Payments"] ?? "payments-events";
		
		services.AddSingleton(sp => new QueueClient(storageConn, queueName, new QueueClientOptions
		{
			MessageEncoding = QueueMessageEncoding.Base64
		}));
		
		// Registra o OutboxPublisher para publicar eventos automaticamente
		services.AddHostedService<OutboxPublisher>();
		
		return services;
	}
}

