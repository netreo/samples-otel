namespace Samples.Model;

public class RedisBlogPost : BlogPost
{
    public string TraceId { get; set; }
    public string SpanId { get; set; }
}