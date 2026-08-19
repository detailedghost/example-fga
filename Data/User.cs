namespace FgaPoc.Data;

/// <summary>
/// Identity + display name only — roles live in the selected authorization provider, not this table.
/// </summary>
public sealed record User
{
    public required int Id { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string DisplayName { get; init; }
}
