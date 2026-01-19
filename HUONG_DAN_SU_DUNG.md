# HỆ THỐNG KIỂM TRA ĐẠO VĂN - HỌC VIỆN NGÂN HÀNG

## 📊 THÔNG TIN DỮ LIỆU MẪU

### Database đã được tạo với 10 tài liệu mẫu về các đề tài Ngân hàng:

1. **Quản trị rủi ro tín dụng tại ngân hàng thương mại** - Nguyễn Văn A (21A4010001)
2. **Phân tích hiệu quả hoạt động kinh doanh tại BIDV** - Trần Thị B (22A4010002)
3. **Ứng dụng Blockchain trong thanh toán quốc tế** - Lê Văn C (23A4010003)
4. **Nâng cao chất lượng dịch vụ ngân hàng điện tử** - Phạm Quỳnh D (20A4010004)
5. **Tác động của lạm phát đến thị trường chứng khoán Việt Nam** - Hoàng Minh E (21A4010005)
6. **Phát triển Fintech và thách thức đối với ngân hàng truyền thống** - Vũ Thị F (22A4010006)
7. **Quản lý thanh khoản tại các ngân hàng thương mại Việt Nam** - Đỗ Văn G (23A4010007)
8. **Vai trò của Ngân hàng Nhà nước trong ổn định kinh tế vĩ mô** - Bùi Thị H (20A4010008)
9. **Phân tích tín dụng xanh tại Việt Nam** - Ngô Văn I (21A4010009)
10. **Chiến lược Marketing ngân hàng trong kỷ nguyên số** - Lý Thị K (22A4010010)

---

## 🚀 HƯỚNG DẪN SỬ DỤNG

### Bước 1: Truy cập hệ thống
Mở trình duyệt và truy cập: **http://localhost:5000/index.html**

### Bước 2: Kiểm tra đạo văn
1. Tải lên file Word (.docx) cần kiểm tra
2. Nhập mã sinh viên
3. Nhấn "Bắt đầu Phân tích"
4. Xem kết quả: % trùng khớp và nguồn gốc

### Bước 3: Thêm tài liệu vào kho
1. Chọn tab "Nạp Kho Dữ liệu"
2. Nhập tiêu đề, tác giả
3. Tải file Word
4. Nhấn "Lưu vào Hệ thống"

---

## 📚 NGUỒN DỮ LIỆU BỔ SUNG

### Nơi tìm dữ liệu đồ án thực tế:

#### 1. **Thư viện số Học viện Ngân hàng**
- Website: https://lib.hvnh.edu.vn
- Tìm kiếm: Luận văn tốt nghiệp, Tiểu luận

#### 2. **Thư viện Quốc gia Việt Nam**
- Website: https://nlv.gov.vn
- Mục: Luận án, Luận văn

#### 3. **Google Scholar (Tiếng Việt)**
- Tìm kiếm: "đồ án tốt nghiệp ngân hàng" OR "luận văn tài chính ngân hàng"
- Lọc: Ngôn ngữ Tiếng Việt

#### 4. **ResearchGate**
- Tìm các nghiên cứu của giảng viên Học viện Ngân hàng
- Keyword: "Banking Vietnam", "Finance Vietnam"

#### 5. **Thư viện các trường Kinh tế**
- NEU (Đại học Kinh tế Quốc dân): https://lib.neu.edu.vn
- UEH (Đại học Kinh tế TP.HCM): https://lib.ueh.edu.vn

#### 6. **Tạo dữ liệu mẫu bằng AI**
Bạn có thể dùng ChatGPT/Gemini để tạo nội dung mẫu:
```
Prompt: "Viết một đoạn tóm tắt 300 từ về đề tài 'Quản trị rủi ro tín dụng tại ngân hàng thương mại Việt Nam' cho luận văn tốt nghiệp"
```

---

## 🔧 CẤU HÌNH DATABASE

**Server:** DONG2004  
**Database:** BAU_Plagiarism_DB  
**User:** sa  
**Password:** 2004  

### Xem dữ liệu trong SQL Server:
```sql
USE BAU_Plagiarism_DB;
SELECT Id, Title, Author, StudentId, UploadDate 
FROM Documents 
ORDER BY UploadDate DESC;
```

---

## 💡 GỢI Ý MỞ RỘNG ĐỒ ÁN

1. **Thêm chức năng xuất báo cáo PDF** với highlight các đoạn trùng lặp
2. **Tích hợp OCR** để đọc file PDF scan
3. **Phân tích ngữ nghĩa** bằng AI để phát hiện paraphrase
4. **Dashboard thống kê** cho giảng viên
5. **API cho mobile app**
6. **Tích hợp với Google Scholar** để tìm kiếm online

---

## 📞 HỖ TRỢ

Nếu gặp lỗi, kiểm tra:
1. SQL Server đã chạy chưa?
2. Connection string đúng chưa?
3. Port 5000 có bị chiếm không?

**Chúc bạn thành công với đồ án tốt nghiệp!** 🎓
