namespace MyApi.Data;

/// <summary>
/// Ánh xạ 1-1 với section "MongoDb" trong appsettings.json.
/// Đây là Options pattern — tương đương ConfigService của NestJS nhưng có kiểu rõ ràng.
/// </summary>
public class MongoDbSettings
{
    /// <summary>Tên section trong file cấu hình.</summary>
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
}
