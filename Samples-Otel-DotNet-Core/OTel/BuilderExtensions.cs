using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Samples.OTel;

public static class BuilderExtensions
{

    public static TracerProviderBuilder AddAppTracing(this TracerProviderBuilder builder, IServiceProvider services, ResourceBuilder? resource = null)
    {
        resource ??= ResourceBuilder.CreateDefault()
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();
        var tracesExporter = services
            .GetRequiredService<IConfiguration>()
            .GetSection("OpenTelemetry:Traces");
        builder
            .AddSource(App.Application.Name, App.RabbitMq.Name, App.Redis.Name, "Npgsql")
            .SetResourceBuilder(resource)
            .AddRedisInstrumentation(
                connection: services.GetRequiredService<IConnectionMultiplexer>(),
                configure: redis =>
                {
                    redis.SetVerboseDatabaseStatements = true;
                    redis.EnrichActivityWithTimingEvents = true;
                })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts => tracesExporter.Bind(opts))
            .AddConsoleExporter();
        return builder;
    }
}