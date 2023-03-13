using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Samples.Model;
using Samples.OTel;
using Samples.Services.Handlers;
using StackExchange.Redis;

namespace Samples.Services.BlogConsumerServices;

public class RedisConsumerService : IHostedService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly BlogPostHandler _handler;
    private readonly ILogger<RedisConsumerService> _logger;
    private ISubscriber? _subscriber;

    public RedisConsumerService(IConnectionMultiplexer redis, BlogPostHandler handler,
        ILogger<RedisConsumerService> logger)
    {
        _redis = redis;
        _handler = handler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
       _subscriber = _redis.GetSubscriber();
       await _subscriber.SubscribeAsync(BlogPost.QueueName, Handler).ConfigureAwait(false);
    }

    private async void Handler(RedisChannel channel, RedisValue value)
    {
        var message = GetValue(value);
        if (message == null) return;
        
        var links = new List<ActivityLink>();
        if (!string.IsNullOrWhiteSpace(message.TraceId) &&
            !string.IsNullOrWhiteSpace(message.SpanId))
        {
            var ctx = new ActivityContext(ActivityTraceId.CreateFromString(message.TraceId),
                ActivitySpanId.CreateFromString(message.SpanId), ActivityTraceFlags.Recorded, isRemote: true);
            links.Add(new ActivityLink(ctx));
        }
        using (var redisActivity = App.Redis.StartActivity(ActivityKind.Consumer, name: $"MESSAGE {channel}", links:links))
        {
            redisActivity?.AddTags(_subscriber, channel);
            redisActivity?.AddTag(PropertyNames.DbStatement, $"MESSAGE {channel} {value.ToString(100)}");
            using (App.Application.StartActivity("Handle Redis Message"))
            {
                if (message != null)
                    await _handler.HandleAsync(message).ConfigureAwait(false);
            }
        }
    }

    private RedisBlogPost? GetValue(RedisValue value)
    {
        if (!value.HasValue || value.IsInteger || value.IsNullOrEmpty) return null;
        return JsonConvert.DeserializeObject<RedisBlogPost>(value)!;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscriber != null)
            await _subscriber.UnsubscribeAsync(BlogPost.QueueName, Handler).ConfigureAwait(false);
    }
}