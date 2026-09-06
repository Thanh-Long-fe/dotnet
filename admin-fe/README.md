# Admin FE

Trang quản trị cho MyApi. React + TypeScript + Vite, không dùng thư viện UI hay state
management nào — toàn bộ là `useState` và `fetch`.

## Chạy

```bash
npm install
npm run dev          # http://localhost:5173
```

Backend phải chạy trước. Mặc định FE gọi `http://localhost:5133` (`dotnet run`).
Nếu backend chạy bằng Docker (cổng 8080):

```bash
cp .env.example .env.local     # rồi sửa VITE_API_BASE_URL thành http://localhost:8080
```

Đăng nhập local: `admin@example.com` / `Admin@123456`

> Cổng 5173 phải khớp `Cors:AllowedOrigins` trong `MyApi/appsettings.json`,
> nếu không trình duyệt sẽ chặn mọi request.

## Cấu trúc

```
src/
├── main.tsx                    điểm vào
├── App.tsx                     quyết định hiện màn đăng nhập hay màn quản lý
├── types.ts                    kiểu dữ liệu khớp DTO của backend
├── datetime.ts                 chuyển đổi UTC <-> giờ địa phương (chỉ ở đây)
├── api/
│   ├── client.ts               fetch + gắn token + bóc lỗi ProblemDetails
│   └── admin.ts                từng endpoint một
└── components/
    ├── LoginPage.tsx
    ├── UsersPage.tsx           bảng + tìm kiếm + gỡ thiết bị + xóa
    └── UserFormDialog.tsx      dùng chung cho tạo mới và sửa
```

## Vài điểm cần biết

**Token lưu ở `localStorage`.** Đánh đổi có ý thức: XSS đọc được token. Chấp nhận vì
đây là trang nội bộ. **User FE thì không được làm vậy** — chỗ đó cần cookie HttpOnly
để làm device binding.

**Không có refresh token.** Access token sống 24 giờ, hết hạn thì đăng nhập lại.
"Đăng xuất" chỉ xóa token ở máy client — server không giữ danh sách phiên nên không
hủy được token đã phát. Token bị lộ vẫn dùng được tới lúc hết hạn.

**Ngày giờ.** Backend chỉ nói UTC; `<input type="datetime-local">` chỉ nói giờ địa
phương. Mọi chuyển đổi nằm trong `datetime.ts` — đừng gọi `new Date()` rải rác trong
component, đó là cách chắc chắn nhất để có bug lệch 7 tiếng.

**`isExpired` do backend tính,** FE chỉ hiển thị. Đồng hồ máy client sai thì cũng
không ảnh hưởng gì tới quyết định của server.
