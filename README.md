# 🎓 Hệ thống Kiểm tra Đạo văn - Học viện Ngân hàng (BAU)

## 📋 Tổng quan

Hệ thống kiểm tra đạo văn tự động cho Học viện Ngân hàng, hỗ trợ:
- ✅ Kiểm tra tính chính trực của bài luận, đồ án tốt nghiệp
- ✅ Phân quyền Giảng viên / Sinh viên / Admin
- ✅ Quản lý danh mục: Khoa, Bộ môn, Môn học
- ✅ Thuật toán NLP so sánh văn bản tiếng Việt
- ✅ Báo cáo chi tiết tỷ lệ tương đồng (%)
- ✅ Thống kê theo môn học, lớp

## 🏗️ Kiến trúc hệ thống

### **Backend (ASP.NET Core Web API)**
```
BAU_Plagiarism_System/
├── BAU_Plagiarism_System.API/        # Web API Layer
│   ├── Controllers/                   # REST API Controllers
│   │   ├── AuthController.cs         # Đăng nhập, đăng ký
│   │   ├── CatalogController.cs      # Quản lý Khoa/Bộ môn/Môn học
│   │   ├── UsersController.cs        # Quản lý người dùng
│   │   ├── DocumentsController.cs    # Upload/Download tài liệu
│   │   └── PlagiarismController.cs   # Kiểm tra đạo văn
│   ├── Data/                         # Data Seeding
│   └── Program.cs                    # Startup configuration
│
├── BAU_Plagiarism_System.Core/       # Business Logic Layer
│   ├── DTOs/                         # Data Transfer Objects
│   │   ├── CatalogDtos.cs
│   │   ├── UserDtos.cs
│   │   ├── DocumentDtos.cs
│   │   └── PlagiarismDtos.cs
│   └── Services/                     # Application Services
│       ├── CatalogService.cs         # Quản lý danh mục
│       ├── UserService.cs            # Quản lý người dùng
│       ├── AuthService.cs            # JWT Authentication
│       ├── DocumentService.cs        # Quản lý tài liệu
│       ├── PlagiarismService.cs      # NLP Engine
│       ├── TextProcessor.cs          # Tiền xử lý văn bản
│       ├── SimilarityChecker.cs      # Tính toán độ tương đồng
│       └── DocumentReader.cs         # Đọc file Word/PDF
│
└── BAU_Plagiarism_System.Data/       # Data Access Layer
    ├── Models/                       # Domain Models
    │   ├── Faculty.cs                # Khoa
    │   ├── Department.cs             # Bộ môn
    │   ├── Subject.cs                # Môn học
    │   ├── User.cs                   # Người dùng
    │   ├── Document.cs               # Tài liệu
    │   └── PlagiarismCheck.cs        # Kết quả kiểm tra
    └── BAUDbContext.cs               # Entity Framework DbContext
```

## 🛠️ Công nghệ sử dụng

### **Backend**
- **Framework**: ASP.NET Core 9.0 Web API
- **Language**: C# 12
- **Database**: Microsoft SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: JWT Bearer Token
- **NLP**: Custom Text Processing (PhoBERT ready)

### **Database Schema**
- Faculties (Khoa)
- Departments (Bộ môn)
- Subjects (Môn học)
- Users (Người dùng - Giảng viên/Sinh viên/Admin)
- Documents (Tài liệu)
- PlagiarismChecks (Kết quả kiểm tra)
- PlagiarismMatches (Chi tiết đoạn văn trùng)

## 🚀 Hướng dẫn cài đặt

### **Yêu cầu hệ thống**
- .NET 9.0 SDK
- SQL Server 2019+
- Visual Studio 2022 / VS Code

### **Bước 1: Clone Repository**
```bash
git clone <repository-url>
cd BAU_Plagiarism_System
```

### **Bước 2: Cấu hình Database**
1. Mở SQL Server Management Studio
2. Chạy script `CreateDatabase.sql` để tạo database
3. Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BAU_Plagiarism_DB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### **Bước 3: Restore NuGet Packages**
```bash
cd BAU_Plagiarism_System.API
dotnet restore
```

### **Bước 4: Seed dữ liệu mẫu**
```bash
dotnet run --seed
```

### **Bước 5: Chạy API**
```bash
dotnet run
```

API sẽ chạy tại: `https://localhost:7xxx`

## 📚 API Endpoints

### **Authentication**
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/register` - Đăng ký
- `GET /api/auth/profile` - Lấy thông tin profile
- `PUT /api/auth/profile` - Cập nhật profile
- `POST /api/auth/change-password` - Đổi mật khẩu

### **Catalog Management**
- `GET /api/catalog/faculties` - Danh sách Khoa
- `GET /api/catalog/departments?facultyId={id}` - Danh sách Bộ môn
- `GET /api/catalog/subjects?departmentId={id}` - Danh sách Môn học
- `POST /api/catalog/faculties` - Tạo Khoa (Admin)
- `POST /api/catalog/departments` - Tạo Bộ môn (Admin)
- `POST /api/catalog/subjects` - Tạo Môn học (Admin/Lecturer)

### **User Management**
- `GET /api/users` - Danh sách người dùng (Admin)
- `GET /api/users/{id}` - Chi tiết người dùng
- `POST /api/users` - Tạo người dùng (Admin)
- `PUT /api/users/{id}` - Cập nhật người dùng
- `DELETE /api/users/{id}` - Xóa người dùng

### **Document Management**
- `GET /api/documents` - Danh sách tài liệu
- `GET /api/documents/{id}` - Chi tiết tài liệu
- `POST /api/documents/upload` - Upload tài liệu (multipart/form-data)
- `GET /api/documents/{id}/download` - Download tài liệu
- `GET /api/documents/{id}/content` - Lấy nội dung văn bản
- `PUT /api/documents/{id}` - Cập nhật metadata
- `DELETE /api/documents/{id}` - Xóa tài liệu

### **Plagiarism Detection**
- `POST /api/plagiarism/check` - Kiểm tra đạo văn
- `GET /api/plagiarism/history` - Lịch sử kiểm tra
- `GET /api/plagiarism/checks/{checkId}` - Chi tiết kết quả
- `GET /api/plagiarism/statistics` - Thống kê (Lecturer/Admin)

## 👥 Tài khoản mặc định

Sau khi seed data, bạn có thể đăng nhập với:

### **Admin**
- Username: `admin`
- Password: `admin123`

### **Giảng viên**
- Username: `gv001`
- Password: `gv001`

### **Sinh viên**
- Username: `21a4010001`
- Password: `student123`

## 🔐 Phân quyền

### **Admin**
- Quản lý toàn bộ hệ thống
- Quản lý Khoa, Bộ môn, Môn học
- Quản lý người dùng
- Xem thống kê toàn hệ thống

### **Lecturer (Giảng viên)**
- Quản lý Môn học của mình
- Kiểm tra bài của sinh viên trong lớp
- Xem thống kê theo môn học
- Duyệt tài liệu công khai

### **Student (Sinh viên)**
- Upload tài liệu cá nhân
- Kiểm tra đạo văn bài của mình
- Xem lịch sử kiểm tra của mình

## 📊 Quy trình kiểm tra đạo văn

1. **Upload tài liệu**: Sinh viên/Giảng viên upload file Word/PDF
2. **Trích xuất văn bản**: Hệ thống đọc và trích xuất nội dung
3. **Tiền xử lý**: Loại bỏ stop words, chuẩn hóa tiếng Việt
4. **So sánh**: Tính toán độ tương đồng với tài liệu công khai
5. **Báo cáo**: Xuất kết quả với tỷ lệ % và đoạn văn trùng lặp

## 🎯 Đánh giá mức độ đạo văn

- **Cao (> 30%)**: ⚠️ Nguy cơ cao
- **Trung bình (15-30%)**: ⚡ Cần xem xét
- **Thấp (< 15%)**: ✅ An toàn

## 📝 Ghi chú

- File upload được lưu trong thư mục `uploads/`
- Database sử dụng soft delete (IsActive = false)
- JWT token có thời hạn 8 giờ (480 phút)
- Hỗ trợ file: .docx, .pdf, .txt

## 🔧 Troubleshooting

### Lỗi kết nối database
```bash
# Kiểm tra connection string
# Đảm bảo SQL Server đang chạy
# Kiểm tra firewall
```

### Lỗi JWT token
```bash
# Kiểm tra Jwt:Secret trong appsettings.json
# Đảm bảo token chưa hết hạn
```

## 📞 Liên hệ

- **Tác giả**: Sinh viên Học viện Ngân hàng
- **Email**: support@bau.edu.vn
- **Website**: https://bau.edu.vn

---

**© 2024 Học viện Ngân hàng - BAU Plagiarism Detection System**
