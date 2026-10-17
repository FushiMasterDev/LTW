# Bước 3 – Trang dành cho khách

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/design/airbnb-reference.md` (chỉ lấy bố cục), `docs/ui/inventory.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit` để dùng lại component đã có.

Skill: `design-taste-frontend` (chạy đủ vòng chụp → sửa), `ui-animation`, `full-output-enforcement`. Nếu chạy trong Codex có thể dùng thêm `image-to-code`, nhưng ảnh thiết kế phải theo `DESIGN.md` và `tokens.css`.

## Nguyên tắc

- View chỉ ghép component. Nếu cần mẫu UI mới, thêm vào thư viện component + UI Kit trước, rồi mới dùng.
- Giữ nguyên `@model`, tên trường form, `asp-action`/`asp-route-*`, antiforgery, validation.
- Ảnh homestay lấy từ dữ liệu. Ảnh trang trí tối đa 4 ảnh CC0/public domain, lưu `wwwroot/images/`, ghi nguồn vào `docs/ui/credits.md`.
- Tiêu đề lớn dùng serif (Lora). Còn lại dùng sans.

## Các trang

1. **`Home/Index`:** banner ảnh + `_SearchBar` biến thể `hero` đè lên mép dưới banner. Tiếp theo là khu vực nổi bật (link theo `AreaGroupExtensions`), homestay nổi bật (`_HomestayCard`), 3 bước đặt phòng có đánh số, cam kết ngắn, lời mời "Trở thành đối tác".
2. **`Homestays/Index`:** `_SearchBar` compact + `_FilterBar` (giá, loại phòng, số người, tiện nghi, sắp xếp `HomestaySort`). Danh mục loại phòng/tiện nghi lấy từ hàm lọc B8 thêm (G-06). Lưới `_HomestayCard`, `_Pager`, `_EmptyState` khi không có kết quả.
3. **`Homestays/Details`:** `_Gallery`, tên + khu vực + rating, tiện nghi, danh sách phòng/loại phòng còn trống theo ngày đã chọn. Bên phải là `_BookingPanel` sticky. Nếu B8 đã có `AvailabilityService.BlockedDatesAsync` (G-07) thì Flatpickr bôi các đêm hết phòng; nếu chưa thì chỉ hiện "Còn N phòng" sau khi chọn ngày. Có chính sách hủy (`_PolicyRules`), đánh giá (danh sách + điểm tổng). Địa chỉ/bản đồ chỉ hiện theo đúng quy tắc `BookingContactVisibility`.
4. **`Bookings/Create`:** form đặt phòng bằng `form-field`, có `_PriceBreakdown`.
   - **Phải chạy khi chưa đăng nhập (FR-03):** khách nhập SĐT, không thấy ô voucher. Sau khi đặt, mã đơn vào session lookup.
   - Đã đăng nhập thì điền sẵn thông tin và cho dùng voucher.
5. **`Bookings/Details`:** `_BookingStepper`, `_DescriptionList`, `_PriceBreakdown`, `_PaymentQr` khi chờ cọc, tải ảnh bill (`_UploadZone`), `_Timeline`. Có nút đổi/hủy khi được phép.
6. **`Bookings/Lookup`:** form mã đơn + SĐT, gọn ở giữa trang.
7. **`Bookings/Mine`:** tabs Sắp tới / Đã qua / Đã hủy, mỗi đơn là một thẻ.
8. **`Bookings/Change`, `_CustomerChanges`:** chọn đổi ngày/loại phòng hoặc hủy, xem trước số tiền hoàn/phí bằng `_PriceBreakdown` trước khi gửi.
9. **Viết đánh giá (FR-06):** trong `Bookings/Details`, khi đơn đã trả phòng và `HasReview == false` (B8, G-09), hiện form điểm + nội dung. Đã đánh giá thì hiện lại đánh giá của khách. Gọi `ReviewService.CreateAsync` (đã đăng nhập) hoặc `CreateByLookupAsync` (tra cứu). Đọc service để biết điều kiện BR-09.
10. **Đăng ký đối tác (FR-25):** trang công khai "Trở thành đối tác". Controller mỏng gọi `OwnerRegistrationService` (`RegisterAsync`, `UploadDocumentAsync`, `SubmitAsync`; đọc service để biết đúng thứ tự và tham số). Form nhiều bước: thông tin cơ sở → số phòng/khu vực → liên hệ → ảnh/giấy tờ → gửi. Sau khi gửi, hiện trang trạng thái hồ sơ bằng `OwnerRegistrationQueryService.MineAsync`, có thể xem lại giấy tờ qua `OpenDocumentAsync`.
11. **`Account/Login`, `Register`, `EmployeeLogin`, `ChangePassword`, `AccessDenied`:** dùng `_AuthLayout`. Khách/chủ đăng nhập bằng SĐT, nhân viên bằng mã nhân viên (xem `fuij_homestay_login_design_v5.md`). Có link qua lại giữa hai trang đăng nhập, `data-toggle-password`.
12. **`Home/StatusCode`, `Shared/Error`.**

Sau khi chuyển xong, xóa class legacy mà các trang này không còn dùng.

## Kết quả bàn giao

- Commit theo nhóm:
  - `feat(ui): trang chủ, tìm kiếm và chi tiết homestay`
  - `feat(ui): đặt phòng, tra cứu và đổi/hủy đơn`
  - `feat(ui): đánh giá, đăng ký đối tác và tài khoản`
- `docs/ui/credits.md`.
- Ảnh `playwright-cli` của luồng: trang chủ → tìm → chi tiết → đặt (**chưa đăng nhập**) → chi tiết đơn có QR → tra cứu → gửi đổi/hủy. Chụp ở 375/768/1280 × sáng/tối, thêm 1 bộ tiếng Anh ở 1280.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đi trọn luồng trên bằng `playwright-cli` với khách chưa đăng nhập, rồi với `0987000001` / `Fuji@2026`.
- Chạy `web-design-guidelines` trên các view đã sửa và sửa hết lỗi mức cao.
- Không có `style="` hay mã hex mới.

## Báo cáo rồi dừng

Báo cáo trang đã làm, hướng thiết kế đã chọn (từ `design-taste-frontend`), ảnh đã dùng, chỗ nào lệch `DESIGN.md`. **Không bắt đầu bước 4.**
