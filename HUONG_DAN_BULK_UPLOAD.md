# 📦 HƯỚNG DẪN UPLOAD HÀNG LOẠT (BULK UPLOAD)

## 🎯 Tính năng mới

Hệ thống hiện đã hỗ trợ **upload nhiều file Word cùng lúc** để nạp hàng loạt vào database!

---

## 📝 Quy tắc đặt tên file

Để hệ thống tự động nhận diện thông tin, đặt tên file theo format:

```
MaSinhVien_TenTacGia_TieuDe.docx
```

### Ví dụ:
- `21A4010001_NguyenVanA_QuanTriRuiRoTinDung.docx`
- `22A4010002_TranThiB_PhanTichHieuQuaBIDV.docx`
- `23A4010003_LeVanC_UngDungBlockchain.docx`

---

## 🚀 Cách sử dụng

### Bước 1: Chuẩn bị file
1. Tạo thư mục chứa tất cả file Word cần upload
2. Đổi tên file theo format trên
3. Đảm bảo file là `.docx` (không phải `.doc`)

### Bước 2: Upload
1. Mở trình duyệt: http://localhost:5000/index.html
2. Kéo xuống phần **"📦 Nạp Hàng loạt (Bulk Upload)"**
3. Click vào ô "Chọn nhiều file Word"
4. Giữ `Ctrl` (Windows) hoặc `Cmd` (Mac) để chọn nhiều file
5. Hoặc chọn tất cả file trong thư mục bằng `Ctrl+A`
6. Click **"📤 Tải lên tất cả"**

### Bước 3: Xem kết quả
Hệ thống sẽ hiển thị:
- ✅ Số file thành công
- ❌ Số file thất bại (nếu có)
- Chi tiết từng file

---

## 💡 Mẹo hay

### 1. Tạo file mẫu nhanh bằng PowerShell
```powershell
# Tạo 5 file Word mẫu
1..5 | ForEach-Object {
    $msv = "21A401000$_"
    $name = @("NguyenVanA", "TranThiB", "LeVanC", "PhamThiD", "HoangVanE")[$_ - 1]
    $title = "DoAnMau$_"
    New-Item -Path "$msv`_$name`_$title.docx" -ItemType File
}
```

### 2. Đổi tên hàng loạt trong Windows
1. Chọn tất cả file
2. Nhấn `F2`
3. Gõ tên mới, Windows sẽ tự động đánh số

### 3. Kiểm tra file đã upload
```sql
USE BAU_Plagiarism_DB;
SELECT TOP 10 Id, Title, Author, StudentId, UploadDate 
FROM Documents 
ORDER BY UploadDate DESC;
```

---

## ⚠️ Lưu ý

1. **Giới hạn**: Không nên upload quá 50 file cùng lúc (để tránh timeout)
2. **Dung lượng**: Mỗi file nên < 5MB
3. **Format**: Chỉ hỗ trợ `.docx` (Word 2007+)
4. **Tên file**: Tránh ký tự đặc biệt như `@#$%^&*()`

---

## 🔧 Troubleshooting

### Lỗi: "Could not extract text"
- **Nguyên nhân**: File bị lỗi hoặc không phải Word thật
- **Giải pháp**: Mở file bằng Word và Save lại

### Lỗi: "No files uploaded"
- **Nguyên nhân**: Chưa chọn file
- **Giải pháp**: Đảm bảo đã chọn file trước khi click Upload

### File upload chậm
- **Nguyên nhân**: File quá lớn hoặc nhiều file
- **Giải pháp**: Chia nhỏ thành nhiều lần upload

---

## 📊 Demo Script

Tạo 10 file mẫu với nội dung thật:

```powershell
# File: CreateSampleDocs.ps1
$topics = @(
    "QuanTriRuiRoTinDung",
    "PhanTichHieuQuaBIDV",
    "UngDungBlockchain",
    "NgânHangDienTu",
    "TacDongLamPhat",
    "PhatTrienFintech",
    "QuanLyThanhKhoan",
    "VaiTroNHNN",
    "TinDungXanh",
    "MarketingNganHang"
)

$authors = @(
    "NguyenVanA", "TranThiB", "LeVanC", "PhamThiD", "HoangVanE",
    "VuThiF", "DoVanG", "BuiThiH", "NgoVanI", "LyThiK"
)

1..10 | ForEach-Object {
    $msv = "21A401000$_"
    $filename = "$msv`_$($authors[$_-1])`_$($topics[$_-1]).docx"
    Write-Host "Creating: $filename"
    # Tạo file Word thật với nội dung (cần Word installed)
    # Hoặc copy từ template
}
```

---

**Chúc bạn upload thành công!** 🎉
