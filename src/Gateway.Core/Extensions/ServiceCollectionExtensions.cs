using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register SimpleMapper services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMapper services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure SimpleMapper options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCouchbaseSimpleMapper(
        this IServiceCollection services,
        Action<SimpleMapperOptions> configure)
    {
        var options = new SimpleMapperOptions();
        configure(options);
        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Adds SimpleMapper services to the dependency injection container using configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCouchbaseSimpleMapper(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        var options = new SimpleMapperOptions();
        configuration.Bind(options);
        services.AddSingleton(options);
        return services;
    }
}
