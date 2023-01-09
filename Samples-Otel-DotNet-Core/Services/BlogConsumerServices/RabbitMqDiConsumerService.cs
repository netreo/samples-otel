using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RabbitMQ.Client.Core.DependencyInjection.Services.Interfaces;

namespace Samples.Services.BlogConsumerServices;

public class RabbitMqDiConsumerService : IHostedService
{
    private readonly IConsumingService _consumer;
    private readonly ILogger<RabbitMqDiConsumerService> _logger;

    public RabbitMqDiConsumerService(IConsumingService consumer, ILogger<RabbitMqDiConsumerService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_consumer.Connection != null)
            _consumer.Connection.CallbackException += (sender, args) =>
            {
                _logger.LogError(args.Exception, "RabbitMq Connection Exception");
                Activity.Current?.RecordException(args.Exception);
            };
        _consumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.StopConsuming();
        return Task.CompletedTask;
    }
}