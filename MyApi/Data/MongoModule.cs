namespace MyApi.Data;

/// <summary>
/// Gom việc đăng ký hạ tầng MongoDB vào một extension method,
/// để Program.cs chỉ còn một dòng: builder.Services.AddMongoDb(...)
/// </summary>
public static class MongoModule
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind section "MongoDb" -> MongoDbSettings, inject được qua IOptions<MongoDbSettings>
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton<MongoContext>();

        return services;
    }
}
