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
        using (App.Application.StartActivity("Handle Redis Message"))
        {
            var message = GetValue(channel, value);
            if (message != null)
                await _handler.HandleAsync(message).ConfigureAwait(false);
        }
    }

    private BlogPost? GetValue(RedisChannel channel, RedisValue value)
    {
        using (var redisActivity = App.Redis.StartActivity($"MESSAGE {channel}", ActivityKind.Consumer))
        {
            redisActivity?.AddTags(_subscriber, channel);
            redisActivity?.AddTag(PropertyNames.DbStatement, $"MESSAGE {channel} {value.ToString(100)}");
            
            if (!value.HasValue || value.IsInteger || value.IsNullOrEmpty) return null;
            return JsonConvert.DeserializeObject<BlogPost>(value)!;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscriber != null)
            await _subscriber.UnsubscribeAsync(BlogPost.QueueName, Handler).ConfigureAwait(false);
    }
}