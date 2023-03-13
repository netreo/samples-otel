using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Core.DependencyInjection;
using RabbitMQ.Client.Core.DependencyInjection.MessageHandlers;
using RabbitMQ.Client.Core.DependencyInjection.Models;
using Samples.Model;
using Samples.OTel;
using Samples.Services.Handlers;

namespace Samples.Services.BlogConsumerServices;

public class RabbitMqBlogPostMessageHandler : IAsyncMessageHandler
{
    private readonly BlogPostHandler _handler;
    private readonly ILogger<RabbitMqBlogPostMessageHandler> _logger;

    public RabbitMqBlogPostMessageHandler(BlogPostHandler handler, ILogger<RabbitMqBlogPostMessageHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public Task Handle(MessageHandlingContext context, string matchingRoute) => Handler(context, matchingRoute);
    private async Task Handler(MessageHandlingContext context, string matchingRoute)
    {


        var links = new List<ActivityLink>();
        if (context.Message.BasicProperties.Headers.TryGetValue("traceid", out var traceid) &&
            context.Message.BasicProperties.Headers.TryGetValue("spanid", out var spanid))
        {
            var ctx = new ActivityContext(ActivityTraceId.CreateFromString(traceid!.ToString()),
                ActivitySpanId.CreateFromString(spanid.ToString()), ActivityTraceFlags.Recorded, isRemote: true);
            links.Add(new ActivityLink(ctx));
        }

        using (App.Application.StartActivity(ActivityKind.Internal, name: "Handle RabbitMq DI Message", links: links))
        {
            _logger.LogInformation("RabbitMq received message on {matchingRoute} from {exchange}", matchingRoute,
                context.Message.Exchange);
            var post = GetValue(context);
            await _handler.HandleAsync(post).ConfigureAwait(false);
        }
    }

    private BlogPost GetValue(MessageHandlingContext context)
    {
        using (var activity = App.RabbitMq.StartActivity($"RECEIVE {BlogPost.QueueName}", ActivityKind.Consumer))
        {
            activity?.AddTags(context.Message);
            activity?.AddTag(PropertyNames.MessagingSystem, "rabbitmq");
            activity?.AddTag(PropertyNames.MessagingOperation, "receive");
            activity?.AddTag(PropertyNames.MessagingSourceKind, "queue");
            activity?.AddTag(PropertyNames.MessagingSourceName, BlogPost.QueueName);
            return context.Message.GetPayload<BlogPost>();
        }
    }
}