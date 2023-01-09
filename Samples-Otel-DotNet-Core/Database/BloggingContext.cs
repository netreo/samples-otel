using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Samples.Database;

public class BloggingContext : DbContext
{
    private readonly IConfiguration _config;
    public const string ConnectionStringKey = nameof(BloggingContext);

    public BloggingContext(IConfiguration config)
    {
        _config = config;
    }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql(_config.GetConnectionString(ConnectionStringKey), options =>
        {
            options.UseAdminDatabase("postgres");
        });
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.UseIdentityColumns();
    }
}