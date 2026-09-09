using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimlifiezYaml.Core.DependencyInjection;

/// <summary>
/// Extension methods for adding logging to SimlifiezYaml services.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds logging support to SimlifiezYaml services with appropriate configuration.
    /// </summary>
    public static IServiceCollection AddSimlifiezYamlLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }

    /// <summary>
    /// Configures logging specifically for SimlifiezYaml with custom settings.
    /// </summary>
    public static IServiceCollection ConfigureSimlifiezYamlLogging(
        this IServiceCollection services,
        Action<ILoggingBuilder>? configure = null)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            
            // Set minimum log levels per category
            builder.AddFilter("SimlifiezYaml.Core.Services", LogLevel.Information);
            builder.AddFilter("SimlifiezYaml.Core.Generators", LogLevel.Debug);
            builder.AddFilter("SimlifiezYaml.Core.Yaml", LogLevel.Warning);
            
            // Allow custom configuration
            configure?.Invoke(builder);
        });

        return services;
    }
}
