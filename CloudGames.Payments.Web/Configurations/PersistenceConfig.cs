using CloudGames.Payments.Application.Handlers;
using CloudGames.Payments.Domain.Events;
using CloudGames.Payments.Domain.Repositories;
using CloudGames.Payments.Infra.EventStore;
using CloudGames.Payments.Infra.Persistence;
using CloudGames.Payments.Infra.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudGames.Payments.Web.Configurations;

public static class PersistenceConfig
{
	public static IServiceCollection AddPaymentsPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<PaymentsDbContext>(opt =>
			opt.UseSqlServer(configuration.GetConnectionString("PaymentsDb")));

		services.AddDbContext<EventStoreSqlContext>(opt =>
			opt.UseSqlServer(configuration.GetConnectionString("PaymentsDb")));

		services.AddScoped<IPaymentRepository, PaymentRepository>();
		services.AddScoped<IEventStore, EventStoreRepository>();

		services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<InitiatePaymentHandler>());

		return services;
	}
}


