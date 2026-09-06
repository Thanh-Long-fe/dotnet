using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MyApi.Data;

/// <summary>
/// Bọc kết nối MongoDB. Đăng ký dạng Singleton vì MongoClient đã tự quản lý
/// connection pool và an toàn đa luồng — tạo mới mỗi request là phản tác dụng.
/// </summary>
public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;

        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    /// <summary>
    /// Lấy một collection theo tên. Mỗi feature tự gọi hàm này cho entity của mình,
    /// nhờ vậy tầng Data không cần biết gì về Features.
    /// </summary>
    public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
}
