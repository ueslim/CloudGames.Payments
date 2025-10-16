using Azure.Messaging.ServiceBus;
using CloudGames.Payments.Infra.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CloudGames.Payments.Web.Configurations;

public static class MessagingConfig
{
	public static IServiceCollection AddPaymentsMessaging(this IServiceCollection services, IConfiguration configuration)
	{
		var sbConn = configuration.GetConnectionString("ServiceBus");
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
			services.AddSingleton(sbClient);
			services.AddHostedService<OutboxPublisher>();
		}
		else
		{
			Log.Warning("ServiceBus connection string is not configured. Outbox publisher is disabled.");
		}
		return services;
	}
}


