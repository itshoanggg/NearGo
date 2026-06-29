# NearGo

Nền tảng săn giảm giá, deal hời từ các siêu thị uy tín tại Việt Nam.


## 📋 Yêu cầu

- .NET 8 SDK
- SQL Server (LocalDB hoặc SQL Server Express/Developer)
- Visual Studio 2022 hoặc VS Code

## 🔧 Cài đặt

### 1. Clone và restore packages

```bash
cd NearGo
dotnet restore
```

### 2. Cấu hình database

Sửa file `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=NearGo;User Id=sa;Password=123;TrustServerCertificate=True"
}
```

### 3. Chạy migrations và seed data

Database sẽ tự động được migrate và seed khi chạy ứng dụng lần đầu tiên.

```bash
dotnet run
```

