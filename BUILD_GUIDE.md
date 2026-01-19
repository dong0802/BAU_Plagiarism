# 🔧 Hướng dẫn Build và Chạy Project

## Bước 1: Tạo Database
```sql
-- Chạy file CreateDatabase.sql trong SQL Server Management Studio
-- Hoặc dùng command:
sqlcmd -S DONG2004 -U sa -P 2004 -i CreateDatabase.sql
```

## Bước 2: Build Project
```bash
cd BAU_Plagiarism_System
dotnet clean
dotnet restore
dotnet build --configuration Release
```

## Bước 3: Seed dữ liệu mẫu (Optional)
Mở file `Program.cs` và thêm code seed vào `Main`:
```csharp
// Sau dòng: var app = builder.Build();
// Thêm:
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BAUDbContext>();
    await BAU_Plagiarism_System.API.Data.SeedData.SeedAsync(context);
}
```

## Bước 4: Chạy API
```bash
cd BAU_Plagiarism_System.API
dotnet run
```

API sẽ chạy tại: `https://localhost:7xxx`

## Bước 5: Test API với Postman/Swagger

### Login
```http
POST https://localhost:7xxx/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

Response sẽ trả về JWT token. Copy token này.

### Test Plagiarism Check
```http
POST https://localhost:7xxx/api/plagiarism/check
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "sourceDocumentId": 1,
  "notes": "Kiểm tra đạo văn lần đầu"
}
```

## 📝 Các API Endpoints chính

### Authentication
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/register` - Đăng ký
- `GET /api/auth/profile` - Xem profile

### Catalog (Danh mục)
- `GET /api/catalog/faculties` - Danh sách Khoa
- `GET /api/catalog/departments?facultyId={id}` - Danh sách Bộ môn
- `GET /api/catalog/subjects?departmentId={id}` - Danh sách Môn học

### Documents
- `GET /api/documents` - Danh sách tài liệu
- `POST /api/documents/upload` - Upload tài liệu (multipart/form-data)
- `GET /api/documents/{id}/download` - Download tài liệu

### Plagiarism
- `POST /api/plagiarism/check` - Kiểm tra đạo văn
- `GET /api/plagiarism/history` - Lịch sử kiểm tra
- `GET /api/plagiarism/statistics` - Thống kê (Lecturer/Admin)

## 🎯 Tài khoản mặc định

**Admin:**
- Username: `admin`
- Password: `admin123`

**Giảng viên:**
- Username: `gv001`
- Password: `gv001`

**Sinh viên:**
- Username: `21a4010001`
- Password: `student123`

## 🚀 Phát triển tiếp

### Frontend (React + TypeScript)
Bạn có thể phát triển frontend với:
- React 18 + TypeScript
- Redux Toolkit (state management)
- Ant Design (UI components)
- Axios (HTTP client)

### Cải tiến NLP Engine
- Tích hợp PhoBERT cho tiếng Việt
- Sử dụng SimCSE cho semantic similarity
- Thêm PyVi cho tokenization tiếng Việt

### Tính năng mở rộng
- Export báo cáo PDF/Excel
- Email notification
- Real-time plagiarism checking
- Batch upload documents
- Advanced analytics dashboard

## ⚠️ Lưu ý

- Đảm bảo SQL Server đang chạy
- Cập nhật connection string trong `appsettings.json`
- JWT Secret nên thay đổi trong production
- File upload được lưu trong thư mục `uploads/`

---

**Chúc bạn thành công với đồ án!** 🎓
