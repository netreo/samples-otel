using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Samples.Model;
using Samples.OTel;
using StackExchange.Redis;

namespace Samples.Services.BlogPosterServices;

public class RedisBlogPosterService : RandomBlogPosterService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisBlogPosterService(IConnectionMultiplexer redis, IHttpClientFactory httpFactory,
        IOptions<BlogPosterServiceOptions> options,
        ILogger<RedisBlogPosterService> logger) : base(httpFactory, options, logger)
    {
        _redis = redis;
    }

    protected override Task HandleMessageAsync(BlogPost value)
    {
        using (var activity = App.Redis.StartActivity($"PUBLISH {BlogPost.QueueName}", ActivityKind.Producer))
        {
            activity?.AddTags(_redis);
            var db = _redis.GetDatabase();
            activity?.AddTags(db, BlogPost.QueueName);
            var redisValue = JsonConvert.SerializeObject(value).ToRedisValue();
            activity?.AddTag(PropertyNames.DbStatement,
                $"PUBLISH {BlogPost.QueueName} {redisValue.ToString(100)}");
            db.Publish(BlogPost.QueueName, redisValue);
            return Task.CompletedTask;
        }
    }
}