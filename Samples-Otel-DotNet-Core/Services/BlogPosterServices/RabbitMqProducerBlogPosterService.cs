using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client.Core.DependencyInjection.Services.Interfaces;
using Samples.Model;
using Samples.OTel;

namespace Samples.Services.BlogPosterServices;

public class RabbitMqProducerBlogPosterService : RandomBlogPosterService
{
    private readonly IProducingService _rabbitMq;

    public RabbitMqProducerBlogPosterService(IProducingService rabbitMq, IHttpClientFactory httpFactory,
        ILogger<RabbitMqProducerBlogPosterService> logger) : base(httpFactory, logger)
    {
        _rabbitMq = rabbitMq;
    }

    protected override async Task HandleMessageAsync(BlogPost value)
    {
        using (var activity = App.RabbitMq.StartActivity($"PUBLISH {BlogPost.QueueName}", ActivityKind.Producer))
        {
            const string routingKey = "default.key";
            activity?.AddTag(PropertyNames.MessagingSystem, "rabbitmq");
            activity?.AddTag(PropertyNames.MessagingOperation, "publish");
            activity?.AddTag(PropertyNames.MessagingDestinationKind, "queue");
            activity?.AddTag(PropertyNames.MessagingDestinationName, BlogPost.QueueName);
            activity?.AddTag(PropertyNames.MessagingRabbitmqRouting_key, routingKey);
            activity?.AddTags(_rabbitMq.Connection);
            var v = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            var properties = _rabbitMq.Channel?.CreateBasicProperties()!;
            properties.ContentType = "application/json";
            properties.CorrelationId = Activity.Current?.TraceId.ToString();
            activity.AddTags(properties);
            await _rabbitMq.SendAsync(v, properties, BlogPost.QueueName, routingKey).ConfigureAwait(false);
        }
    }
}