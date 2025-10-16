using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace CloudGames.Payments.Web.Configurations;

public static class SwaggerConfig
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "CloudGames - API de Pagamentos",
                Version = "v1",
                Description = "Microserviço de pagamentos. Autenticação é gerenciada pelo API Management (APIM)."
            });
            
            // Authentication is handled by APIM - no JWT security scheme needed
            // In production, APIM validates tokens and forwards userId via headers
            
            // Incluir comentários XML
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
        return services;
    }
}
