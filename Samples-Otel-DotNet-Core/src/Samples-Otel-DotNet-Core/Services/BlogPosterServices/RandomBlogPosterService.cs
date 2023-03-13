using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Samples.Model;
using Samples.OTel;

namespace Samples.Services.BlogPosterServices;

public abstract class RandomBlogPosterService : IHostedService, IDisposable 
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<BlogPosterServiceOptions> _options;
    private readonly ILogger _logger;
    private readonly IReadOnlyList<string> _blogs;
    private readonly IRandomizerString _title = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords {Max = 20, Min = 5, UseNullValues = false});
    private readonly IRandomizerString _content = RandomizerFactory.GetRandomizer(new FieldOptionsTextLipsum {Paragraphs = 3, UseNullValues = false});
    private readonly IRandomizerNumber<int> _blog = RandomizerFactory.GetRandomizer(new FieldOptionsInteger {Min = 0, Max = 9});
    private Timer? _timer;
    protected int IntervalSeconds { get; set; } = 10;

    protected RandomBlogPosterService(IHttpClientFactory httpFactory, IOptions<BlogPosterServiceOptions> options,
        ILogger logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;
        var emails = RandomizerFactory.GetRandomizer(new FieldOptionsEmailAddress());
        _blogs = Enumerable.Range(0, 10).Select(_ => emails.Generate()!).ToList();
    }

    private async void DoWorkAsync(object? state)
    {
        using (var activity = App.Application.StartActivity($"{this.GetType().Name} Post Blog"))
        {
            var client = _httpFactory.CreateClient(this.GetType().Name);
            var content = await client.GetStringAsync(_options.Value.ContentUri)
                .ConfigureAwait(false);
            
            var message = new BlogPost
            {
                Blog = $"https://localhost.blogspot/{_blogs[_blog.Generate() ?? 0]}",
                Content = string.IsNullOrWhiteSpace(content) ? _content.Generate() ?? string.Empty : content,
                Title = _title.Generate() ?? string.Empty,
            };
            try
            {
                _logger.LogInformation($"Sending Blog Post for {message.Blog} using {this.GetType().Name}");
                await HandleMessageAsync(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                activity?.SetStatus(ActivityStatusCode.Error, e.ToString());
                activity?.RecordException(e);
            }
        }
    }

    protected abstract Task HandleMessageAsync(BlogPost value);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWorkAsync, cancellationToken, TimeSpan.Zero, TimeSpan.FromSeconds(IntervalSeconds));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer != null)
            await _timer.DisposeAsync().ConfigureAwait(false);
        _timer = null;
    }

    public void Dispose() => _timer?.Dispose();
}