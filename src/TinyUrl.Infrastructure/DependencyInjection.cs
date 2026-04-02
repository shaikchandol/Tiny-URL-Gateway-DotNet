using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyUrl.Application.Interfaces;
using TinyUrl.Infrastructure.Data;
using TinyUrl.Infrastructure.Repositories;
using TinyUrl.Infrastructure.Services;

namespace TinyUrl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        });

        services.AddScoped<IUrlRepository, UrlRepository>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();

        return services;
    }
}
