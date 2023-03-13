namespace Samples.Model;

public class BlogPost
{
    internal const string QueueName = "blog-post";
    public string Blog { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}