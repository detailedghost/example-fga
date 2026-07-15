using Dapper;

namespace FgaPoc.Data;

public sealed class UserRepository(DbConnectionFactory connectionFactory)
{
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<User>(
            "select id, username, password, display_name as DisplayName from users where username = @Username",
            new { Username = username }
        );
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        var users = await connection.QueryAsync<User>(
            "select id, username, password, display_name as DisplayName from users order by username"
        );
        return users.AsList();
    }
}
