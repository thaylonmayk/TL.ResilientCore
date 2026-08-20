using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TL.ResilientCore.Application.Abstractions.Data;
using TL.ResilientCore.Infrastructure.Outbox;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.Infrastructure.Persistence.Interceptors;

namespace TL.ResilientCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();
            
            var connectionString = configuration.GetConnectionString("Database") 
                ?? throw new ArgumentNullException("Connection string 'Database' not found.");

            options.UseNpgsql(connectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHostedService<ProcessOutboxMessagesJob>();

        return services;
    }
}