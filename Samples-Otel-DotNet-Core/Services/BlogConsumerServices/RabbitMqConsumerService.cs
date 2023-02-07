using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Samples.Model;
using Samples.OTel;
using Samples.Services.Handlers;

namespace Samples.Services.BlogConsumerServices;

public class RabbitMqConsumerService : IHostedService
{
    private readonly RabbitMqService _consumer;
    private readonly BlogPostHandler _handler;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private string? _consumerTag;

    public RabbitMqConsumerService(RabbitMqService consumer, BlogPostHandler handler, ILogger<RabbitMqConsumerService> logger)
    {
        _consumer = consumer;
        _handler = handler;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_consumer.Model);
        consumer.Received += Handler;
        _consumerTag = _consumer.Model.BasicConsume(BlogPost.QueueName, (bool) true,
#pragma warning disable CS8600
            (string) nameof(BlogPostHandler), (bool) false, (bool) false, (IDictionary<string, object>) null,
#pragma warning restore CS8600
            (IBasicConsumer) consumer);
        return Task.CompletedTask;
    }

    private async void Handler(object? sender, BasicDeliverEventArgs @event)
    {
        var links = new List<ActivityLink>();
        if (@event.BasicProperties.Headers.TryGetValue("traceid", out var traceid) &&
            @event.BasicProperties.Headers.TryGetValue("spanid", out var spanid))
        {
            var ctx = new ActivityContext(ActivityTraceId.CreateFromString(traceid!.ToString()),
                ActivitySpanId.CreateFromString(spanid.ToString()), ActivityTraceFlags.Recorded, isRemote: true);
            links.Add(new ActivityLink(ctx));
        }

        using (App.Application.StartActivity(ActivityKind.Internal, name: "Handle RabbitMq Message", links: links))
        {
            _logger.LogInformation(
                $"Received message from {@event.Exchange} on {@event.RoutingKey} {JsonConvert.SerializeObject(@event.BasicProperties)}");
            var body = GetValue(@event);
            await _handler.HandleAsync(body).ConfigureAwait(false);
        }
    }

    private BlogPost GetValue(BasicDeliverEventArgs @event)
    {
        using (var activity = App.RabbitMq.StartActivity($"RECEIVE {BlogPost.QueueName}", ActivityKind.Consumer))
        {
            activity?.AddTags(@event);
            activity?.AddTag(PropertyNames.MessagingSystem, "rabbitmq");
            activity?.AddTag(PropertyNames.MessagingOperation, "receive");
            activity?.AddTag(PropertyNames.MessagingSourceKind, "queue");
            activity?.AddTag(PropertyNames.MessagingSourceName, BlogPost.QueueName);
            activity?.AddTags(_consumer.Connection);
            return JsonConvert.DeserializeObject<BlogPost>(Encoding.UTF8.GetString(@event.Body.Span))!;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_consumerTag))
            _consumer.Model.BasicCancel(_consumerTag);
        return Task.CompletedTask;
    }
}