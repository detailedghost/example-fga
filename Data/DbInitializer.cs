using Dapper;

namespace FgaPoc.Data;

/// <summary>
/// Idempotent schema + seed step run once at startup. Roles live in OpenFGA —
/// this only seeds identity rows and a couple of demo posts for ownership.
/// </summary>
public sealed class DbInitializer(DbConnectionFactory connectionFactory)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            create table if not exists users (
                id serial primary key,
                username text unique not null,
                password text not null,
                display_name text not null
            );

            create table if not exists posts (
                id serial primary key,
                title text not null,
                body text not null,
                author_username text not null,
                created_at timestamptz not null default now()
            );
            """
        );

        var userCount = await connection.ExecuteScalarAsync<long>("select count(*) from users");
        if (userCount == 0)
        {
            (string Username, string DisplayName)[] users =
            [
                ("alice", "Alice Admin"),
                ("bob", "Bob Editor"),
                ("carol", "Carol Writer"),
                ("dave", "Dave Reader"),
                ("erin", "Erin Writer"),
            ];

            // Fake POC credentials — password intentionally equals username.
            await connection.ExecuteAsync(
                "insert into users (username, password, display_name) values (@Username, @Username, @DisplayName)",
                users.Select(u => new { u.Username, u.DisplayName })
            );
        }

        var postCount = await connection.ExecuteScalarAsync<long>("select count(*) from posts");
        if (postCount == 0)
        {
            (string Title, string Body, string AuthorUsername)[] posts =
            [
                (
                    "First Light on Eagle Ridge",
                    "Left the trailhead at 5am to catch sunrise from the ridge. Frost on the grass, "
                        + "breath fogging in the headlamp beam. Worth every switchback — the whole valley "
                        + "went gold in about ninety seconds.",
                    "carol"
                ),
                (
                    "Ultralight Gear Notes from the Sierra",
                    "Shaved another pound off the base weight this season, mostly by swapping the "
                        + "trowel and cook kit. Still debating whether the quilt is warm enough below "
                        + "freezing — next trip will tell.",
                    "erin"
                ),
            ];

            await connection.ExecuteAsync(
                "insert into posts (title, body, author_username) values (@Title, @Body, @AuthorUsername)",
                posts.Select(p => new
                {
                    p.Title,
                    p.Body,
                    p.AuthorUsername,
                })
            );
        }
    }
}
