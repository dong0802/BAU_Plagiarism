# 🎓 Hệ thống Kiểm tra Đạo văn BAV - Banking Academy of Vietnam

Hệ thống kiểm tra đạo văn chuyên nghiệp được thiết kế riêng cho **Học viện Ngân hàng (BAV)**, giúp đảm bảo tính liêm chính học thuật và hỗ trợ giảng viên, sinh viên trong việc đối soát tài liệu.

---

## ✨ Tính năng nổi bật

*   🚀 **Phát hiện Đạo văn Thông minh**: So khớp văn bản với kho dữ liệu nội bộ cực lớn (luận văn, tiểu luận, giáo trình) và Internet.
*   🔍 **So sánh Chéo 1-vs-1**: Công cụ đối soát trực tiếp 2 tài liệu bất kỳ trong hệ thống để tìm kiếm sự trùng lặp chi tiết.
*   📄 **Báo cáo Chuyên nghiệp**: Xuất báo cáo kết quả kiểm tra dưới định dạng HTML/PDF đẹp mắt, đầy đủ các chỉ số và đoạn hội thoại trùng khớp.
*   🎛️ **Bộ lọc Nâng cao (Advanced Filters)**:
    *   Loại trừ trích dẫn (Quotes).
    *   Loại trừ mục lục tham khảo (Bibliography).
    *   Loại trừ các nguồn có tỷ lệ trùng khớp thấp (dưới ngưỡng tối thiểu).
*   🖥️ **Trình xem Kết quả Focus Mode**: Giao diện side-by-side hiển thị trực quan đoạn văn trùng khớp và nguồn tương ứng, tự động tối ưu không gian làm việc.
*   📊 **Phân tích Chất lượng (Quality Analysis)**: Đánh giá độ mạch lạc, vốn từ, lỗi trình bày và cấu trúc của tài liệu.
*   🛡️ **Phân quyền & Bảo mật**: Phân quyền chi tiết cho Admin, Giảng viên và Sinh viên. Bảo vệ quyền riêng tư cho các bài nộp của sinh viên.
*   💳 **Quản lý Credit**: Hệ thống giới hạn lượt kiểm tra hàng ngày cho sinh viên để tối ưu tài nguyên hệ thống.

---

## 🏗️ Kiến trúc Hệ thống

### **1. Backend (ASP.NET Core Web API)**
*   `BAU_Plagiarism_System.API`: Tầng xử lý Request, quản lý JWT Auth, Controllers và cấu hình Web.
*   `BAU_Plagiarism_System.Core`: Tầng Business Logic chính.
    *   `SimilarityChecker`: Engine thực hiện giải thuật N-gram song song để tìm kiếm trùng lặp.
    *   `PlagiarismService`: Điều phối nghiệp vụ kiểm tra và quản lý lịch sử.
    *   `DocumentReader`: Trích xuất nội dung từ file `.docx`, `.pdf`, `.txt`.
    *   `DocumentQualityService`: Engine đánh giá chất lượng văn bản tự động.
*   `BAU_Plagiarism_System.Data`: Tầng ORM (Entity Framework Core) quản lý kết nối SQL Server và Migrations.

### **2. Frontend (React + TypeScript)**
*   Sử dụng **Vite** để tối ưu tốc độ build.
*   Sử dụng **Ant Design (AntD) 5** cho giao diện hiện đại, cao cấp.
*   Quản lý State bằng **Redux Toolkit**.

---

## 🛠️ Công nghệ Sử dụng

*   **Ngôn ngữ**: C# (.NET 8.0/9.0), TypeScript.
*   **Database**: Microsoft SQL Server 2022.
*   **Thư viện chính**:
    *   Backend: Entity Framework Core, Hangfire (xử lý nền), iTextSharp (PDF), DocX (Word).
    *   Frontend: React 18, Ant Design, Framer Motion (hiệu ứng), React-Redux.

---

## 🚀 Hướng dẫn Cài đặt

### **Yêu cầu**
*   .NET SDK (phiên bản 8.0 trở lên).
*   Node.js (phiên bản 18 trở lên).
*   SQL Server.

### **Bước 1: Khởi tạo Cơ sở dữ liệu**
1.  Kết nối tới SQL Server của bạn.
2.  Chạy script khởi tạo: [CreateDatabase.sql](file:///c:/BAU_Plagiarism/CreateDatabase.sql). 
    *   *Lưu ý: Script này đã được cập nhật để loại bỏ các tính năng không cần thiết (Check AI).*
3.  Cập nhật Connection String trong `BAU_Plagiarism_System.API/appsettings.json`.

### **Bước 2: Khởi chạy Backend**
```bash
cd BAU_Plagiarism_System.API
dotnet run
```

### **Bước 3: Khởi chạy Frontend**
```bash
cd frontend
npm install
npm run dev
```

---

## 👥 Tài khoản Thử nghiệm (Mặc định)

| Cấp độ | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `admin` | `admin123` |
| **Giảng viên** | `gv001` | `gv001` |
| **Sinh viên** | `21a4010001` | `student123` |

---

## 🎯 Quy định về Đạo văn (BAV Regulations)

*   **Dưới 20%**: An toàn (✅).
*   **Từ 20% - 40%**: Cần xem xét/Cảnh báo (⚠️).
*   **Trên 40%**: Vi phạm nghiêm trọng (❌).

---
**© 2024 Banking Academy of Vietnam - Developed by BAV Tech Team**
