# Mẫu – Sửa lỗi sau review

> Dán phần dưới cho **cùng phiên Codex** vừa làm bước đó (hoặc phiên mới nếu đã đóng). Thay `<...>` và dán danh sách lỗi từ review.

---

Đọc lại `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/prompts/ui/<tên file bước>.md`.

Review bước `<tên bước>` tìm ra các lỗi dưới đây. Sửa **đúng các lỗi này**, không mở rộng phạm vi, không sửa `Services/`, `Data/`, `Models/Entities/`.

```
<dán danh sách lỗi: file:dòng – mô tả – cách tái hiện>
```

Yêu cầu:

1. Mỗi lỗi: tìm nguyên nhân gốc. Nếu lỗi nằm trong component dùng chung, sửa ở component (và UI Kit), không vá riêng từng view.
2. Lỗi nào bạn cho là không đúng hoặc không nên sửa: không sửa, ghi lý do vào báo cáo.
3. Chạy lại `dotnet build`, `dotnet test`. Chụp lại bằng `playwright-cli` đúng các trang bị ảnh hưởng ở kích thước và chế độ màu đã báo lỗi.
4. Commit: `fix(ui): sửa lỗi review bước <tên bước>`.

Báo cáo bảng: lỗi → đã sửa/không sửa → file đã đổi → ảnh chụp lại. Rồi dừng.
