# Build context là THƯ MỤC GỐC của solution (xem docker-compose.yml),
# nên mọi đường dẫn COPY dưới đây tính từ d:\project

# ───────────── Stage 1: build ─────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy trước file .csproj rồi restore: Docker cache lại lớp này, nên chỉ khi
# thay đổi package mới phải tải lại — sửa code .cs thì restore không chạy lại.
COPY ["MyApi/MyApi.csproj", "MyApi/"]
RUN dotnet restore "MyApi/MyApi.csproj"

# Giờ mới copy toàn bộ source
COPY . .

WORKDIR "/src/MyApi"
RUN dotnet publish "MyApi.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ───────────── Stage 2: runtime ─────────────
# Image này chỉ có ASP.NET runtime (~110MB), không kèm SDK (~800MB)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Chạy bằng user không phải root (image chính thức tạo sẵn user này)
USER $APP_UID

ENTRYPOINT ["dotnet", "MyApi.dll"]
