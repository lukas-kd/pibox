using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddEfContext<TContext>(this IServiceCollection serviceCollection,
            Action<DbContextOptionsBuilder> configureContext,
            ServiceLifetime contextLifetime = ServiceLifetime.Transient,
            ServiceLifetime optionsLifetime = ServiceLifetime.Singleton)
            where TContext : DbContext, IDbContext
        {
            serviceCollection.AddDbContext<TContext>(configureContext, contextLifetime, optionsLifetime);
            serviceCollection.AddTransient<IDbContext, TContext>();
            var dbContextInterfaces = typeof(TContext).GetInterfaces()
                .Where(x => x.IsInterface && x.IsAssignableTo(typeof(IDbContext)) && x != typeof(IDbContext));
            foreach (var dbContextInterface in dbContextInterfaces)
            {
                serviceCollection.AddTransient(dbContextInterface, typeof(TContext));
            }

            return serviceCollection;
        }

        public static IHealthChecksBuilder AddEfContext<TContext>(this IHealthChecksBuilder healthChecksBuilder)
            where TContext : DbContext
        {
            healthChecksBuilder.AddDbContextCheck<TContext>(typeof(TContext).Name,
                tags: [HealthCheckTag.Readiness.Value]);
            return healthChecksBuilder;
        }

        public static void MigrateEfContexts(this IApplicationBuilder applicationBuilder)
        {
            var logger = applicationBuilder.ApplicationServices.GetRequiredService<ILogger<EntityFrameworkPlugin>>();
            applicationBuilder.ApplicationServices.GetServices<IDbContext>().ToList()
                .ForEach(dbContext =>
                {
                    var dbContextName = dbContext.GetType().Name;
                    logger.LogDebug("Migrating '{DbContextName}'", dbContextName);
                    dbContext.Migrate();
                    logger.LogDebug("Migrated '{DbContextName}'", dbContextName);
                });
        }
    }
}
