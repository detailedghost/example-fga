using Dapper;

namespace FgaPoc.Data;

public sealed class PostRepository(DbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<Post>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        var posts = await connection.QueryAsync<Post>(
            "select id, title, body, author_username as AuthorUsername, created_at as CreatedAt "
                + "from posts order by created_at desc"
        );
        return posts.AsList();
    }

    public async Task<Post?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Post>(
            "select id, title, body, author_username as AuthorUsername, created_at as CreatedAt "
                + "from posts where id = @Id",
            new { Id = id }
        );
    }

    public async Task<int> CreateAsync(
        string title,
        string body,
        string authorUsername,
        CancellationToken ct = default
    )
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(
            "insert into posts (title, body, author_username) values (@Title, @Body, @AuthorUsername) "
                + "returning id",
            new
            {
                Title = title,
                Body = body,
                AuthorUsername = authorUsername,
            }
        );
    }

    public async Task UpdateAsync(int id, string title, string body, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        await connection.ExecuteAsync(
            "update posts set title = @Title, body = @Body where id = @Id",
            new
            {
                Id = id,
                Title = title,
                Body = body,
            }
        );
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        await connection.ExecuteAsync("delete from posts where id = @Id", new { Id = id });
    }
}
