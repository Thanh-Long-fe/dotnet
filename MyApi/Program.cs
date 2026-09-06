using Microsoft.AspNetCore.Identity;
using MyApi.Auth;
using MyApi.Data;
using MyApi.Features.Auth;
using MyApi.Features.Users;

// Tiện ích dòng lệnh: sinh chuỗi hash để dán vào AdminAuth:PasswordHash.
//   dotnet run --project MyApi -- hash-password "mat-khau-cua-ban"
// Chạy trước khi dựng host nên không cần Mongo hay cấu hình gì.
if (args is ["hash-password", var plainPassword])
{
    Console.WriteLine(new PasswordHasher<AdminAccount>().HashPassword(AdminAccount.Instance, plainPassword));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// 1. ĐĂNG KÝ DỊCH VỤ  (~ phần providers/imports của AppModule)
//    Sau builder.Build() sẽ không đăng ký thêm được nữa.
// ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Biến exception chưa bắt thành response ProblemDetails chuẩn RFC 7807,
// thay vì trả HTML kèm stack trace ra ngoài.
builder.Services.AddProblemDetails();

// Hạ tầng
builder.Services.AddMongoDb(builder.Configuration);

// Xác thực Admin: đăng nhập email/mật khẩu -> access token Bearer.
// Tách hẳn với cơ chế đăng nhập của User FE sau này (cookie HttpOnly + device binding).
builder.Services.AddAdminJwtAuth(builder.Configuration);
builder.Services.AddAdminFeCors(builder.Configuration);

// Xác thực User FE (extension): đăng nhập -> gắn thiết bị -> access token riêng.
// Khóa ký và audience khác hẳn admin, nên token hai bên không dùng chéo được.
builder.Services.AddUserAuth(builder.Configuration);

// Feature modules
builder.Services.AddUsersModule();

// Tạo sẵn user demo để test ngay sau khi init — chỉ ở Development.
// (Admin không cần seed: nó nằm ở cấu hình AdminAuth, luôn có sẵn.)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevDataSeeder>();
}

var app = builder.Build();

// ─────────────────────────────────────────────────────────────
// 2. DỰNG PIPELINE HTTP  (~ middleware, thứ tự khai báo = thứ tự chạy)
// ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Ở Development thì giữ trang lỗi chi tiết mặc định cho dễ debug.
    app.UseExceptionHandler();
}

// Trong container chỉ mở cổng HTTP nên bỏ qua redirect (tránh cảnh báo lúc chạy).
// Biến DOTNET_RUNNING_IN_CONTAINER được image chính thức của Microsoft set sẵn = true.
if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    app.UseHttpsRedirection();
}

// CORS phải đứng TRƯỚC Authentication: request preflight (OPTIONS) của trình duyệt
// không mang token, nếu bị chặn ở đây thì FE không gọi được gì cả.
app.UseCors(AuthModule.AdminFeCorsPolicy);

// Thứ tự BẮT BUỘC: Authentication (anh là ai?) rồi mới tới Authorization (anh được làm gì?),
// và cả hai phải đứng trước MapControllers.
app.UseAuthentication();
app.UseAuthorization();

// Quét mọi class kế thừa ControllerBase và đăng ký route theo attribute
app.MapControllers();

// ─────────────────────────────────────────────────────────────
// 3. Endpoint mẫu của template (Minimal API) — giữ lại để đối chiếu
//    hai cách viết: Minimal API ở đây vs Controller ở Features/Users
// ─────────────────────────────────────────────────────────────
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
