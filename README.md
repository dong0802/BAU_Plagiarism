# 🎓 Hệ thống Kiểm tra Đạo văn & Phát hiện AI - Học viện Ngân hàng (BAU)

## 📋 Tổng quan

Hệ thống kiểm tra đạo văn và nhận diện nội dung AI (ChatGPT, Gemini,...) tự động cho Học viện Ngân hàng, hỗ trợ:
- ✅ **Kiểm tra Đạo văn**: So sánh văn bản với kho dữ liệu nội bộ và internet.
- ✅ **Nhận diện AI**: Phân tích nội dung tạo bởi trí tuệ nhân tạo (Perplexity & Burstiness).
- ✅ **Đối chiếu Song song**: Hiển thị Side-by-Side với highlight toàn bộ đoạn trùng khớp.
- ✅ **Phân quyền**: Giảng viên / Sinh viên / Admin với chức năng riêng biệt.
- ✅ **Quản lý danh mục**: Khoa, Bộ môn, Môn học, Lớp học.
- ✅ **Thuật toán NLP**: N-gram, Cosine Similarity tối ưu cho tiếng Việt.
- ✅ **Hệ thống Credits**: Giới hạn lượt kiểm tra hàng ngày cho sinh viên.

## 🏗️ Kiến trúc hệ thống

### **1. Backend (ASP.NET Core 9.0 Web API)**
```
BAU_Plagiarism/
├── BAU_Plagiarism_System.API/        # Tầng API & Cấu hình
│   ├── Controllers/                   # Các điều hướng API
│   │   ├── AuthController.cs         # Đăng nhập, đăng ký, profile
│   │   ├── CatalogController.cs      # Khoa/Bộ môn/Môn học
│   │   ├── UsersController.cs        # Quản lý người dùng & Credits
│   │   ├── DocumentsController.cs    # Upload/Download & Full-text
│   │   └── PlagiarismController.cs   # Engine kiểm tra & Lịch sử
│   └── Program.cs                    # Cấu hình Startup & Middleware
│
├── BAU_Plagiarism_System.Core/       # Tầng Xử lý Nghiệp vụ (Core)
│   ├── DTOs/                         # Đối tượng chuyển đổi dữ liệu
│   └── Services/                     # Logic xử lý chính
│       ├── PlagiarismService.cs      # Điều phối kiểm tra
│       ├── TextProcessor.cs          # Tiền xử lý (loại bỏ stop words, chuẩn hóa)
│       ├── SimilarityChecker.cs      # Engine tính toán % trùng lặp
│       ├── AiDetectionService.cs     # Engine nhận diện AI chuyên sâu
│       └── DocumentReader.cs         # Đọc trích xuất Word/PDF
│
└── BAU_Plagiarism_System.Data/       # Tầng Truy cập Dữ liệu
    ├── Models/                       # Các thực thể Database (User, Document,...)
    └── BAUDbContext.cs               # Entity Framework DbContext
```

### **2. Frontend (React 18 + Vite)**
Giao diện người dùng hiện đại, Dashboard thống kê và trình xem so khớp chi tiết.
- `frontend/src/pages/`: PlagiarismCheckPage, HistoryPage, LoginPage,...
- `frontend/src/api/`: Axios client gọi API Backend.
- `frontend/src/store/`: Redux Toolkit quản lý trạng thái (User, Credits).

## 🛠️ Công nghệ sử dụng

- **Backend**: C# 12, ASP.NET Core 9, Entity Framework Core, SQL Server.
- **Frontend**: TypeScript, React, Vite, Ant Design (AntD), Framer Motion.
- **Bảo mật**: JWT Bearer Token, Password Hashing (BCrypt).
- **Phát hiện AI**: Thuật toán phân tích Perplexity (độ dễ đoán) và Burstiness (biến thiên câu).

## 🚀 Hướng dẫn cài đặt

### **Yêu cầu hệ thống**
- .NET 9.0 SDK
- Node.js 18+ & npm
- SQL Server 2019+
- Visual Studio 2022

### **Bước 1: Cấu hình Database**
1. Mở SQL Server Management Studio.
2. Chạy script `CreateDatabase.sql` để tạo database `BAU_Plagiarism_DB`.
3. Cập nhật Connection String trong `appsettings.json` của project API.

### **Bước 2: Chạy Backend**
```bash
# Mở solution bằng Visual Studio 2022
# Hoặc dùng command line:
cd BAU_Plagiarism_System.API
dotnet run
```
API mặc định chạy tại: `https://localhost:7167` (hoặc tùy cấu hình launchSettings).

### **Bước 3: Chạy Frontend**
```bash
cd frontend
npm install
npm run dev
```
Truy cập tại: `http://localhost:5173`.

## � API Endpoints chính

### **Xác thực & Người dùng**
- `POST /api/auth/login` - Đăng nhập
- `GET /api/users/me/credits` - Kiểm tra lượt dùng còn lại

### **Tài liệu & Kiểm tra**
- `POST /api/documents/upload` - Tải lên tài liệu mới
- `POST /api/plagiarism/check` - Thực hiện kiểm tra đạo văn & AI
- `GET /api/documents/{id}/content` - Lấy toàn văn tài liệu đối soát

## 👥 Tài khoản mặc định

| Vai trò | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `admin` | `admin123` |
| **Giảng viên** | `gv001` | `gv001` |
| **Sinh viên** | `21a4010001` | `student123` |

## 📊 Phân tích kết quả

- **Trùng khớp (%)**: Tính theo giải thuật N-gram so với kho dữ liệu BAU.
- **Xác suất AI**: 
    - **Thấp (< 30%)**: Có khả năng người viết.
    - **Trung bình (30-70%)**: Nghi ngờ có sự hỗ trợ của AI.
    - **Cao (> 70%)**: Khả năng cao văn bản được tạo bởi máy.

## 🎯 Đánh giá đạo văn (Quy chuẩn BAU)

- **An toàn**: < 20%
- **Cảnh báo**: 20% - 40%
- **Vi phạm**: > 40%

## 📞 Liên hệ hỗ trợ

- **Email**: support@bau.edu.vn
- **Website**: [https://bau.edu.vn](https://bau.edu.vn)

---
**© 2024 Banking Academy of Vietnam - BAU Plagiarism Detection System**
