using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Samples.Database;

public class Post
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PostId { get; set; }

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
}