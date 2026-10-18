# Ảnh trang trí

Theo `ui-rebuild.md`: ảnh trang trí (không phải ảnh homestay) chỉ dùng ảnh CC0 hoặc public domain, tối đa 4 ảnh, lưu ở `wwwroot/images/`. Ảnh homestay luôn lấy từ dữ liệu; nguồn của ảnh mẫu seed ở `wwwroot/images/demo/CREDITS.md`.

| File | Dùng ở | Nội dung | Tác giả | Giấy phép | Nguồn |
|---|---|---|---|---|---|
| `images/hero-hoan-kiem.jpg` (1600×1067), `images/hero-hoan-kiem-sm.jpg` (1080×720) | Banner trang chủ | Hồ Hoàn Kiếm buổi sáng, tháp Rùa giữa hồ (tháng 9/2014) | vi:User:Prof MK | Public domain (Public Domain Mark 1.0) | [Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Hoan_Kiem_Lake_in_the_morning,_September_2014.jpeg) |
| `images/auth-veranda.webp` (960×640) | Cột ảnh trang đăng nhập, cổng nhân viên, đăng ký (`_AuthAside`) | Hiên nhà gỗ nhìn ra vườn | Joshua Ness | CC0 1.0 | [StockSnap](https://stocksnap.io/photo/house-home-CLD6T4J9VZ) (bản gốc trong bộ ảnh mẫu `images/demo/veranda-garden.webp`) |

Banner: ảnh gốc 1600×1200, cắt về 3:2 (bỏ bớt trời) và nén JPEG bằng `sips`. Không cần ghi công nhưng banner vẫn ghi tên tác giả. Ảnh banner cũ `images/demo/hero-wooden-cabin.webp` (CC BY 2.0) đã xóa ở bước 03.

Bước 03y (18/10/2026): tiếp tục dùng hai ảnh trang trí trên, không thêm ảnh mới và không dùng ảnh của bản mẫu Lovable. Thẻ khu vực dùng ảnh bìa của một homestay đang hoạt động trong khu vực, lấy từ truy vấn `HomeController` đã có; thiếu ảnh dùng nền xanh rừng.
