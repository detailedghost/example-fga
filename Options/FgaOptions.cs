namespace FgaPoc.Options;

/// <summary>
/// OpenFGA connection settings, bound from environment variables in Program.cs.
/// </summary>
public sealed class FgaOptions
{
    public required string ApiUrl { get; init; }
    public required string StoreName { get; init; }
}
