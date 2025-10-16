using Microsoft.Extensions.DependencyInjection;

namespace CloudGames.Payments.Web.Configurations;

public static class ApiConfig
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        return services;
    }
}
