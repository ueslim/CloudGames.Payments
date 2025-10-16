using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CloudGames.Payments.Web.Configurations;

public static class JwtConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Jwt:Authority"];
        var secret = configuration["Jwt:Secret"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var devBypass = configuration.GetValue<bool>("Auth:DevBypass");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                if (devBypass)
                {
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = false,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        SignatureValidator = (token, parameters) => new JsonWebToken(token),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };
                }
                else if (!string.IsNullOrWhiteSpace(authority))
                {
                    o.Authority = authority;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };
                }
                else if (!string.IsNullOrWhiteSpace(secret))
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier,
                        ClockSkew = TimeSpan.Zero
                    };
                }
            });
        services.AddAuthorization();
        return services;
    }
}
