namespace FgaPoc.Data;

public sealed record Post
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string AuthorUsername { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
