# 🎓 Hệ thống Kiểm tra Đạo văn & Phát hiện AI - Học viện Ngân hàng (BAU)

## 📋 Tổng quan

Hệ thống kiểm tra đạo văn và nhận diện nội dung AI (ChatGPT, Gemini,...) tự động dành cho Học viện Ngân hàng, hỗ trợ:
- ✅ **Kiểm tra Đạo văn**: So sánh văn bản với kho dữ liệu nội bộ và Internet.
- ✅ **Nhận diện AI**: Phân tích nội dung tạo bởi trí tuệ nhân tạo thông qua chỉ số Perplexity & Burstiness.
- ✅ **Đối chiếu Song song**: Hiển thị Side-by-Side với highlight toàn bộ đoạn trùng khớp (Full-text logic).
- ✅ **Phân quyền**: Giảng viên / Sinh viên / Admin với chức năng riêng biệt.
- ✅ **Quản lý danh mục**: Khoa, Bộ môn, Môn học, Lớp học.
- ✅ **Thuật toán NLP**: N-gram, Cosine Similarity tối ưu cho xử lý tiếng Việt.
- ✅ **Hệ thống Credits**: Giới hạn lượt kiểm tra bài làm hàng ngày cho sinh viên (mặc định 5 lượt/ngày).

## 🏗️ Kiến trúc hệ thống

### **1. Backend (ASP.NET Core 9.0 Web API)**
```
BAU_Plagiarism/
├── BAU_Plagiarism_System.API/        # Tầng API & Cấu hình Web
│   ├── Controllers/                   # Các điều hướng API chính
│   │   ├── AuthController.cs         # Đăng nhập, đăng ký, profile
│   │   ├── CatalogController.cs      # Quản lý Khoa/Bộ môn/Môn học
│   │   ├── UsersController.cs        # Quản lý người dùng & Credits
│   │   ├── DocumentsController.cs    # Upload/Download & Full-text source
│   │   └── PlagiarismController.cs   # Engine kiểm tra & Lịch sử bài làm
│   └── Program.cs                    # Cấu hình Startup, JWT & DI
│
├── BAU_Plagiarism_System.Core/       # Tầng Xử lý Nghiệp vụ Logic
│   ├── Services/                     # Application Services
│   │   ├── PlagiarismService.cs      # Điều phối tiến trình kiểm tra
│   │   ├── TextProcessor.cs          # Tiền xử lý (Tokenizing, stop words)
│   │   ├── SimilarityChecker.cs      # Engine tính toán % trùng lặp
│   │   ├── AiDetectionService.cs     # Engine nhận diện AI nội bộ
│   │   └── DocumentReader.cs         # Đọc trích xuất file Word/PDF
│
└── BAU_Plagiarism_System.Data/       # Tầng Truy cập Dữ liệu (ORM)
    ├── Models/                       # Các thực thể Database (User, Document,...)
    └── BAUDbContext.cs               # Entity Framework DbContext & Seeding
```

### **2. Frontend (React 18 + Vite)**
Giao diện người dùng hiện đại, Dashboard thống kê và trình xem so khớp chi tiết.
- `frontend/src/pages/`: PlagiarismCheckPage, HistoryPage, LoginPage,...
- `frontend/src/api/`: Axios client cấu hình gọi API.
- `frontend/src/store/`: Redux Toolkit quản lý User-Session & Credits.

## 🛠️ Công nghệ sử dụng

- **Backend**: C# 12, ASP.NET Core 9, Entity Framework Core, SQL Server 2022.
- **Frontend**: TypeScript, React 18, Vite, Ant Design (AntD), Framer Motion.
- **Bảo mật**: JWT Bearer Token, Password Hashing (BCrypt).
- **Phát hiện AI**: Thuật toán phân tích Perplexity (độ dễ đoán) và Burstiness (biến thiên văn phong).

## 🚀 Hướng dẫn cài đặt

### **Yêu cầu hệ thống**
- .NET 9.0 SDK
- Node.js 18+ & rpm/npm
- SQL Server 2019+
- Visual Studio 2022

### **Bước 1: Cấu hình Database**
1. Mở SQL Server Management Studio (SSMS).
2. Chạy script [CreateDatabase.sql](file:///c:/BAU_Plagiarism/CreateDatabase.sql) để tạo database.
3. Cập nhật Connection String trong `appsettings.json` tại thư mục API.

### **Bước 2: Chạy Backend**
```bash
# Sử dụng Visual Studio 2022
# Hoặc dùng command line:
cd BAU_Plagiarism_System.API
dotnet run
```
Backend sẽ được kích hoạt tại: **`http://127.0.0.1:5200`**

### **Bước 3: Chạy Frontend**
```bash
cd frontend
npm install
npm run dev
```
Giao diện ứng dụng sẽ chạy tại: **`http://localhost:3000`**

---

## 📚 API Endpoints chính

### **Xác thực & Người dùng**
- `POST /api/auth/login` - Đăng nhập tài khoản
- `GET /api/users/me/credits` - Lấy thông tin lượt dùng (Credits) hiện tại

### **Tài liệu & Kiểm tra**
- `POST /api/documents/upload` - Tải lên văn bản/file Word mới
- `POST /api/plagiarism/check` - Thực hiện kiểm tra đạo văn & AI tổng hợp
- `GET /api/documents/{id}/content` - Lấy toàn văn tài liệu gốc để đối soát full-text

---

## 👥 Tài khoản thử nghiệm (Mặc định sau Seed)

| Vai trò | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `admin` | `admin123` |
| **Giảng viên** | `gv001` | `gv001` |
| **Sinh viên** | `21a4010001` | `student123` |

## 🎯 Quy chuẩn đánh giá (BAU Regulation)

- **An toàn (Safe)**: < 20% trùng khớp.
- **Cảnh báo (Warning)**: 20% - 40% trùng khớp.
- **Vi phạm (Violation)**: > 40% trùng khớp.

## 📊 Phân tích Phát hiện AI

- **Xác suất AI (AI Probability)**: 
    - **Thấp (< 30%)**: Văn bản tự nhiên.
    - **Trung bình (30-70%)**: Nghi ngờ có sự can thiệp của AI Tools.
    - **Cao (> 70%)**: Khả năng cao được tạo hoàn toàn bởi AI.

---

## 📞 Liên hệ hỗ trợ

- **Email**: support@bau.edu.vn
- **Website**: [https://bau.edu.vn](https://bau.edu.vn)

---
**© 2024 Banking Academy of Vietnam - BAU Plagiarism Detection System**
