using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Samples.Model;
using Samples.OTel;

namespace Samples.Services.BlogPosterServices;

public class RabbitMqBlogPosterService : RandomBlogPosterService
{
    private readonly RabbitMqService _rabbitMq;

    public RabbitMqBlogPosterService(RabbitMqService rabbitMq, IHttpClientFactory httpFactory,
        ILogger<RabbitMqBlogPosterService> logger) : base(httpFactory, logger)
    {
        _rabbitMq = rabbitMq;
    }

    protected override Task HandleMessageAsync(BlogPost value)
    {
        using (var activity = App.RabbitMq.StartActivity($"PUBLISH {BlogPost.QueueName}", ActivityKind.Producer))
        {
            activity?.AddTag(PropertyNames.MessagingSystem, "rabbitmq");
            activity?.AddTag(PropertyNames.MessagingOperation, "publish");
            activity?.AddTag(PropertyNames.MessagingDestinationKind, "queue");
            activity?.AddTag(PropertyNames.MessagingDestinationName, BlogPost.QueueName);
            activity?.AddTags(_rabbitMq.Connection);
            var properties = _rabbitMq.Model.CreateBasicProperties();
            properties.CorrelationId = Activity.Current?.TraceId.ToString();
            activity.AddTags(properties);
            _rabbitMq.Model.Publish(value, BlogPost.QueueName, properties: properties);
        }
        return Task.CompletedTask;
    }
}