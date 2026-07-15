namespace FgaPoc.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddBlogData(this IServiceCollection services)
    {
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<DbInitializer>();
        services.AddSingleton<PostRepository>();
        services.AddSingleton<UserRepository>();
        return services;
    }
}
