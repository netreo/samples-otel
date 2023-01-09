using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Core.DependencyInjection;
using Samples.Database;
using Samples.Model;
using Samples.OTel;
using Samples.Services;
using Samples.Services.BlogConsumerServices;
using Samples.Services.BlogPosterServices;
using Samples.Services.Handlers;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;

var resource = ResourceBuilder
    .CreateDefault()
    .AddService("Samples.Otel.NETCORE")
    .AddTelemetrySdk()
    .AddEnvironmentVariableDetector();

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, configure) =>
    {
        // add redis config
        configure
            .AddJsonFile("./redis.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"./redis.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: true);

        // add rabbitmq config
        configure
            .AddJsonFile("./rabbitmq.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"./rabbitmq.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: true);

        // add open telemetry config
        configure
            .AddJsonFile("./opentelemetry.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"./opentelemetry.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: true);
    })
    .ConfigureLogging((context, builder) =>
    {
        builder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resource);
            var logsExporter = context.Configuration
                .GetSection("OpenTelemetry:Logs");
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            options.IncludeScopes = false;
            options.AddOtlpExporter(opts => logsExporter.Bind(opts));
            options.AddConsoleExporter();
            //options.AttachLogsToActivityEvent();
        });

        builder.AddConsole();
    })
    .ConfigureServices((context, services) =>
    {
        var rabbitMqImpl = context.Configuration.GetValue<string>("RabbitMqService");

        if ("SIMPLE".Equals(rabbitMqImpl, StringComparison.OrdinalIgnoreCase))
        {
            // add rabbitmq
            var rabbitFactory = new ConnectionFactory();
            context.Configuration.GetSection("RabbitMq").Bind(rabbitFactory);
            services.AddSingleton<IConnectionFactory>(rabbitFactory)
                .AddSingleton<IAsyncConnectionFactory>(rabbitFactory)
                .AddSingleton<RabbitMqService>();
        }

        if ("DI".Equals(rabbitMqImpl, StringComparison.OrdinalIgnoreCase))
        {
            // add rabbitmq di
            var exchange = BlogPost.QueueName;
            services.AddRabbitMqServices(context.Configuration.GetSection("RabbitMq"))
                .AddConsumptionExchange(exchange, context.Configuration.GetSection("RabbitMqExchange"))
                .AddProductionExchange(exchange, context.Configuration.GetSection("RabbitMqExchange"))
                .AddAsyncMessageHandlerTransient<RabbitMqBlogPostMessageHandler>("*.key", exchange);
        }

        // add redis
        var redisConfig = context.Configuration.GetSection("Redis")
            .Get<RedisConfiguration>()!;
        services
            .AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConfig.ConfigurationOptions));

        // add postgres
        services.AddDbContextFactory<BloggingContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString(BloggingContext.ConnectionStringKey)));

        // message receiver handler
        services.AddSingleton<BlogPostHandler>();
        services.AddHttpClient();
        
        // redis poster
        services.AddHostedService<RedisBlogPosterService>();
        services.AddHostedService<RedisConsumerService>();

        if ("SIMPLE".Equals(rabbitMqImpl, StringComparison.OrdinalIgnoreCase))
        {
            // rabbitmq poster
            services.AddHostedService<RabbitMqBlogPosterService>();
            services.AddHostedService<RabbitMqConsumerService>();
        }

        if ("DI".Equals(rabbitMqImpl, StringComparison.OrdinalIgnoreCase))
        {
            // rabbitmq di poster
            services.AddHostedService<RabbitMqProducerBlogPosterService>();
            services.AddHostedService<RabbitMqDiConsumerService>();
        }

    })
    .UseConsoleLifetime()
    .Build();


// add opentelemetry tracing
using (var provider = Sdk.CreateTracerProviderBuilder()
           .AddAppTracing(host.Services, resource)
           .Build(host.Services))
{

    using (App.Application.StartActivity("Setup Db"))
    {
        try
        {
            var context = await host.Services
                .GetRequiredService<IDbContextFactory<BloggingContext>>()
                .CreateDbContextAsync()
                .ConfigureAwait(false);
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        }
        catch (Exception e)
        {
            var logger = host.Services.GetRequiredService<ILogger<BloggingContext>>();
            logger.LogError(e, "While ensuring the postgres db");
            throw;
        }
    };

    await host.RunAsync().ConfigureAwait(false);

    provider.ForceFlush();
}