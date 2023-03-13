using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Samples.Database;
using Samples.Model;
using Samples.OTel;
using StackExchange.Redis;

namespace Samples.Services.Handlers;

public class BlogPostHandler
{
    private const string MessageReceivedCount = "blog-posts-received";
    private readonly IConnectionMultiplexer _redis;
    private readonly IDbContextFactory<BloggingContext> _dbFactory;
    private readonly ILogger<BlogPostHandler> _logger;

    private readonly IRandomizerNumber<int> _throwError = RandomizerFactory.GetRandomizer(new FieldOptionsInteger
        {Min = 0, Max = 10, UseNullValues = false, ValueAsString = false});

    private const int ThrowErrorValue = 8;

    public BlogPostHandler(IConnectionMultiplexer redis, IDbContextFactory<BloggingContext> dbFactory, ILogger<BlogPostHandler> logger)
    {
        _redis = redis;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task HandleAsync(BlogPost value)
    {
        using (var activity = App.Application.StartActivity($"{nameof(BlogPostHandler)}.{nameof(HandleAsync)}"))
        {
            try
            {
                ThrowErrorRoulette(_throwError.Generate() ?? -1);
                
                _logger.LogInformation($"Handling Blog Post for {value.Blog}");
                
                // increment the redis counter for number of posts received
                var total = await _redis.GetDatabase().StringIncrementAsync(MessageReceivedCount).ConfigureAwait(false);
                _logger.LogInformation($"Handles {total} messages since last redis reset");

                ThrowErrorRoulette(_throwError.Generate() ?? -1);
                
                await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
                var transaction = await db.Database.BeginTransactionAsync().ConfigureAwait(false);

                var blogUrl = value.Blog.ToLowerInvariant();

                var blog = await db.Blogs
                               .FirstOrDefaultAsync(i =>
                                   i.Url == blogUrl)
                               .ConfigureAwait(false)
                           ?? new Blog {Url = blogUrl,};

                blog.Posts.Add(new Post
                {
                    Title = value.Title,
                    Content = value.Title,
                });

                try
                {
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    ThrowErrorRoulette(_throwError.Generate() ?? -1);
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw;
                }
            }
            catch (Exception e)
            {
                activity.RecordException(e);
                _logger.LogError(e, "When receiving message from blog post");
            }
        }
    }

    private static void ThrowErrorRoulette(int throwError)
    {
        if (throwError >= ThrowErrorValue)
        {
            throw new ArgumentOutOfRangeException(nameof(throwError), throwError,
                $"Value must be >= {ThrowErrorValue}");
        }
    }
}