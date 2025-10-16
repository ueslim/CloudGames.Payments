using System.Threading.Tasks;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Handlers;
using CloudGames.Payments.Application.Queries;
using CloudGames.Payments.Domain.Repositories;
using CloudGames.Payments.Infra.EventStore;
using CloudGames.Payments.Infra.Persistence;
using CloudGames.Payments.Infra.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class PaymentsTests
{
    private ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PaymentsDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddDbContext<EventStoreSqlContext>(o => o.UseInMemoryDatabase(dbName + "_events"));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<CloudGames.Payments.Domain.Events.IEventStore, EventStoreRepository>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<InitiatePaymentHandler>());
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Initiate_PersistsPaymentAndEvent()
    {
        var sp = BuildProvider("payments-tests-init");
        var mediator = sp.GetRequiredService<IMediator>();
        var userId = Guid.NewGuid();
        var dto = new InitiatePaymentRequestDto(Guid.NewGuid(), 19.9m);

        var resp = await mediator.Send(new InitiatePaymentCommand(userId, dto));

        resp.Status.Should().Be("Succeeded");
        using var scope = sp.CreateScope();
        var eventsDb = scope.ServiceProvider.GetRequiredService<EventStoreSqlContext>();
        (await eventsDb.StoredEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Status_ReturnsCurrentState()
    {
        var sp = BuildProvider("payments-tests-status");
        var mediator = sp.GetRequiredService<IMediator>();
        var userId = Guid.NewGuid();
        var dto = new InitiatePaymentRequestDto(Guid.NewGuid(), 5m);
        var created = await mediator.Send(new InitiatePaymentCommand(userId, dto));

        var resp = await mediator.Send(new GetPaymentStatusQuery(created.PaymentId));

        resp.PaymentId.Should().Be(created.PaymentId);
        resp.Status.Should().Be("Succeeded");
    }
}
