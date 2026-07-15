namespace FgaPoc.Options;

/// <summary>
/// Blog Postgres connection settings, bound from environment variables in Program.cs.
/// </summary>
public sealed class BlogDbOptions
{
    public required string ConnectionString { get; init; }
}
