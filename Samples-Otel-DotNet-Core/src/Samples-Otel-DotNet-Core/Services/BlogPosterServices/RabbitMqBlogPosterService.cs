using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samples.Model;
using Samples.OTel;

namespace Samples.Services.BlogPosterServices;

public class RabbitMqBlogPosterService : RandomBlogPosterService
{
    private readonly RabbitMqService _rabbitMq;

    public RabbitMqBlogPosterService(RabbitMqService rabbitMq, IHttpClientFactory httpFactory,
        IOptions<BlogPosterServiceOptions> options,
        ILogger<RabbitMqBlogPosterService> logger) : base(httpFactory, options, logger)
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
            if (activity != null)
            {
                properties.Headers.Add("traceid", activity.TraceId.ToHexString());
                properties.Headers.Add("spanid", activity.SpanId.ToHexString());
            }
            properties.CorrelationId = Activity.Current?.TraceId.ToString();
            activity.AddTags(properties);
            _rabbitMq.Model.Publish(value, BlogPost.QueueName, properties: properties);
        }
        return Task.CompletedTask;
    }
}