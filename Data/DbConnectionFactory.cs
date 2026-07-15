using FgaPoc.Options;
using Npgsql;

namespace FgaPoc.Data;

public sealed class DbConnectionFactory(BlogDbOptions options)
{
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
