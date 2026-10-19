# Kiểm kê giao diện (Bước 0)

Ngày kiểm kê: 17/10/2026, nhánh `feature/foundation` (sau B7 `dd1e8ba` + `51e6865`).
Chỉ đọc code, không sửa file nào ngoài `docs/ui/`. Đường dẫn trong file tính từ `HomestayBooking.Web/`.

## 0. Tóm tắt số liệu

| Hạng mục | Số lượng |
|---|---|
| File `.cshtml` | **67** = 39 trang + 18 partial/layout + 10 `_ViewImports`/`_ViewStart` |
| Controller MVC (không tính `Controllers/Api`) | 22 lớp có action (+ 2 lớp cơ sở trừu tượng `AdminController`, `AccountingController`) |
| Route = action public | **92** (47 GET/không gắn verb, 45 POST). Ngoài phạm vi: `Api/BankWebhookController` 1 action |
| CSS | 6 file, 2.764 dòng; 57 mã hex nằm ngoài `tokens.css` |
| JS | `site.js` 162 dòng + 5 khối `<script>` viết trong view |
| `style="` trong view | 3 chỗ (`_Layout` ×2, `_DashboardLayout` ×1) |
| Test đọc HTML/ViewResult | 4 file integration + 3 file service (mục 6) |

Quy ước cột "Quyền":
- **Admin** = `[Authorize(Policy = Management)]` kế thừa từ `AdminController` (chức vụ Manager/Administrator).
- **Accounting** = `[Authorize(Policy = Accounting)]` kế thừa từ `AccountingController` (Accountant/Manager/Administrator).
- **Anti-forgery:** mọi POST đều được `AutoValidateAntiforgeryTokenAttribute` toàn cục kiểm tra (`Program.cs:160`). Riêng `Home/StatusCode` có `[IgnoreAntiforgeryToken]`.
- **"GET\*"**: action không gắn `[HttpGet]`, nên nhận mọi method. Bản POST cùng tên có `[HttpPost]` được ưu tiên.

## 1. Bảng route

Route mặc định là `{area:exists}/{controller=Dashboard}/{action=Index}/{id?}` và `{controller=Home}/{action=Index}/{id?}`. Cột "View · @model" ghi view được render. "→" nghĩa là redirect (PRG) hoặc trả về JSON/file.

### 1.1. Khu khách (không area): 33 route

| # | URL | Controller.Action | Method | Quyền | View · @model | Partial | Trường form | Hook `data-*` | FR |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `/` | Home.Index | GET\* | công khai | `Home/Index` · `HomeIndexViewModel` | — (section `Head`, `Hero`) | GET→`/Homestays`: `AreaId`, `CheckIn`, `CheckOut`, `Guests` | `data-date-range` | FR-01 |
| 2 | `/Home/StatusCode/{code}` | Home.StatusCodePage | mọi method | công khai, `[IgnoreAntiforgeryToken]` | `Home/StatusCode` · `int` | — | — | — | — |
| 3 | `/Home/Error` | Home.Error | GET\* | công khai | `Shared/Error` · `ErrorViewModel` | — | — | — | — |
| 4 | `/Homestays` | Homestays.Index | GET\* | công khai | `Homestays/Index` · `HomestaySearchViewModel` | `_Pager` | GET: `Q`, `AreaId`, `CheckIn`, `CheckOut`, `Guests`, `MaxPrice` | `data-date-range` | FR-01 |
| 5 | `/Homestays/Details/{id}` | Homestays.Details | GET\* | công khai | `Homestays/Details` · `HomestayDetailsViewModel` | `_PolicyRules` | GET: `CheckIn`, `CheckOut`, `Guests`; link `Bookings/Create?roomTypeId&checkIn&checkOut&guests` | `data-lightbox-src/-index/-alt/-step/-view/-counter`, `data-date-range`, `data-count` | FR-02 |
| 6 | `/Bookings/Quote` | Bookings.Quote | GET | công khai | → JSON (`roomTypeId`, `checkIn`, `checkOut`, `voucherId`) | — | — | gọi từ script của `Bookings/Create` | FR-02 |
| 7 | `/Bookings/Create` | Bookings.Create | GET | công khai (nhân viên bị chuyển về `/Homestays`) | `Bookings/Create` · `BookingCreateViewModel` | `_ValidationScriptsPartial` | `RoomTypeId`(hidden), `CheckIn`, `CheckOut`, `Guests`, `ContactName`, `ContactPhone`, `SpecialRequests`, `VoucherId` | `data-quote`, `data-empty`, `data-loading-text`; `id="bookingForm"`, `id="quoteBox"` | FR-03 |
| 8 | `/Bookings/Create` | Bookings.Create | POST | công khai (nhân viên → 403) | → `Details/{code}` hoặc `Bookings/Create` | như #7 | như #7 | như #7 | FR-03 |
| 9 | `/Bookings/Details/{code}` | Bookings.Details | GET | khách của đơn hoặc mã có trong session tra cứu | `Bookings/Details` · `BookingDetailsViewModel` | `_CustomerChanges` (→`_ChangeHistory`) | `UploadReceipt`: `receipt`, `transferContent`; `Cancel`: `reason`; `UploadChangeReceipt`: `requestId`, `receipt`, `transferContent` | `data-copy`, `data-expires`, `data-change-countdown`, `data-additional-payment`, `data-confirm`, `data-loading-text`; `onerror` viết trong thẻ ảnh QR | FR-04, FR-05, FR-17 |
| 10 | `/Bookings/Status/{code}` | Bookings.Status | GET | như #9 | → JSON `{status, holdExpiresAt, changeVersion}` (trang hỏi lại mỗi 5 giây) | — | — | — | FR-04 |
| 11 | `/Bookings/UploadReceipt/{code}` | Bookings.UploadReceipt | POST | như #9 | → `Details` | — | `receipt` (file), `transferContent` | — | FR-17 |
| 12 | `/Bookings/Receipt?paymentId=` | Bookings.Receipt | GET | khách của đơn, hoặc Accountant/Manager/Administrator | → file ảnh bill | — | — | — | FR-17 |
| 13 | `/Bookings/Cancel/{code}` | Bookings.Cancel | POST | như #9 | → `Details` | — | `reason` | `data-confirm` | FR-05 |
| 14 | `/Bookings/Mine` | Bookings.Mine | GET | `[Authorize]` | `Bookings/Mine` · `PaginatedList<MyBookingRow>` + `ViewBag.Vouchers` (`List<VoucherOption>`) | `_Pager` | `page` | — | FR-04 |
| 15 | `/Bookings/Lookup` | Bookings.Lookup | GET | công khai | `Bookings/Lookup` · `BookingLookupViewModel` | `_ValidationScriptsPartial` | `Code`, `PhoneNumber` | `data-loading-text` | FR-04 |
| 16 | `/Bookings/Lookup` | Bookings.Lookup | POST | công khai | → `Details` hoặc `Bookings/Lookup` | như #15 | như #15 | như #15 | FR-04 |
| 17 | `/Bookings/Change/{code}` | Bookings.Change | GET | Role `Customer`, đúng chủ đơn | `Bookings/Change` · `ChangeRequestForm` (layout theo `IsStaff`) | `_PageHead`, `_ChangeQuote` | `RequestType`, `NewCheckIn`, `NewCheckOut`, `NewRoomTypeId`, `PreviewToken` | `data-change-type`, `data-change-dates`, `data-change-room`, `data-quote`, `data-confirm`, `data-quote-total` | FR-05 |
| 18 | `/Bookings/QuoteChange/{code}` | Bookings.QuoteChange | POST | như #17 | `Bookings/Change` | như #17 | như #17 | như #17 | FR-05 |
| 19 | `/Bookings/SubmitChange/{code}` | Bookings.SubmitChange | POST (`formaction`) | như #17 | → `Details` hoặc `Bookings/Change` | như #17 | như #17 | như #17 | FR-05 |
| 20 | `/Bookings/UploadChangeReceipt/{code}` | Bookings.UploadChangeReceipt | POST | như #17 | → `Details` | — | `requestId`, `receipt`, `transferContent` | — | FR-05 |
| 21 | `/Account/Login` | Account.Login | GET | `[AllowAnonymous]` (returnUrl `/Admin`, `/Staff` → `EmployeeLogin`) | `Account/Login` · `LoginViewModel` | `_ValidationScriptsPartial` | `PhoneNumber`, `Password`, `RememberMe`, `ReturnUrl` | `data-toggle-password`, `data-label-show/-hide`, `data-loading-text` | v5 §8 |
| 22 | `/Account/Login` | Account.Login | POST | `[AllowAnonymous]` | → theo vai trò hoặc `Account/Login` | như #21 | như #21 | như #21 | v5 §8 |
| 23 | `/Account/Register` | Account.Register | GET | `[AllowAnonymous]` | `Account/Register` · `RegisterViewModel` | `_ValidationScriptsPartial` | `FullName`, `PhoneNumber`, `Email`, `Password`, `ConfirmPassword`, `ReturnUrl` | `data-loading-text` | v5 §8 |
| 24 | `/Account/Register` | Account.Register | POST | `[AllowAnonymous]` | → `returnUrl` hoặc `Account/Register` | như #23 | như #23 | như #23 | v5 §8 |
| 25 | `/Account/EmployeeLogin` | Account.EmployeeLogin | GET | `[AllowAnonymous]` | `Account/EmployeeLogin` · `EmployeeLoginViewModel` | `_ValidationScriptsPartial` | `EmployeeCode`, `Password`, `RememberMe`, `ReturnUrl` | như #21 | v5 §8 |
| 26 | `/Account/EmployeeLogin` | Account.EmployeeLogin | POST | `[AllowAnonymous]` | → theo vai trò hoặc `Account/EmployeeLogin` | như #25 | như #25 | như #25 | v5 §8 |
| 27 | `/Account/Logout` | Account.Logout | POST | `[Authorize]` | → `/` (form trong `_LoginPartial`) | — | — | — | — |
| 28 | `/Account/ChangePassword` | Account.ChangePassword | GET | `[Authorize]` | `Account/ChangePassword` · `ChangePasswordViewModel` | `_ValidationScriptsPartial` | `CurrentPassword`, `NewPassword`, `ConfirmPassword` | `data-loading-text` | v5 §8 |
| 29 | `/Account/ChangePassword` | Account.ChangePassword | POST | `[Authorize]` | → `ChangePassword` hoặc trang này | như #28 | như #28 | như #28 | v5 §8 |
| 30 | `/Account/AccessDenied` | Account.AccessDenied | GET | `[AllowAnonymous]`, trả 403 | `Account/AccessDenied` · không model | — | — | — | — |
| 31 | `/Language/Set` | Language.Set | POST | công khai | → `returnUrl` (form trong `_LanguageSwitch`) | — | `culture`, `returnUrl` | — | NFR đa ngôn ngữ |
| 32 | `/Dev/SimulateBank` | Dev.SimulateBank | GET | `Management`, chỉ Development (môi trường khác trả 404) | `Dev/SimulateBank` · `SimulateBankViewModel` | `_ValidationScriptsPartial` | `Content`, `Amount`, `AccountNumber` (+ query `code`, `amount`) | — | FR-24 |
| 33 | `/Dev/SimulateBank` | Dev.SimulateBank | POST | như #32 | → `SimulateBank` | như #32 | như #32 | — | FR-24 |


### 1.2. Area `Admin` (policy `Management`): 29 route

| # | URL | Controller.Action | Method | View · @model | Partial | Trường form | Hook `data-*` | FR |
|---|---|---|---|---|---|---|---|---|
| 34 | `/Admin` | Dashboard.Index | GET\* | `Dashboard/Index` · không model; `ViewBag.HomestayCount/RoomCount/OwnerCount/StaffCount/PendingOwnerCount/PendingBookingCount` | — | — | — | FR-20/21 (tổng quan) |
| 35 | `/Admin/Amenities` | Amenities.Index | GET\* | `Amenities/Index` · `PaginatedList<Amenity>`; `ViewData["Query"]`, `ViewBag.HomestayCounts` | `_PageHead`, `_SearchBar`, `_RowActions`, `_EmptyState`, `_Pager` | GET: `q`, `page` | `data-confirm` (trong `_RowActions`) | FR-21 |
| 36 | `/Admin/Amenities/Create` | Amenities.Create | GET\* | `Amenities/Form` · `AmenityFormViewModel` | `_ValidationScriptsPartial` | `Name`, `Icon` | `data-loading-text`; script xem trước icon | FR-21 |
| 37 | `/Admin/Amenities/Create` | Amenities.Create | POST | → `Index` hoặc `Form` | như #36 | như #36 | như #36 | FR-21 |
| 38 | `/Admin/Amenities/Edit/{id}` | Amenities.Edit | GET\* | `Amenities/Form` | như #36 | như #36 | như #36 | FR-21 |
| 39 | `/Admin/Amenities/Edit/{id}` | Amenities.Edit | POST | → `Index` hoặc `Form` | như #36 | như #36 | như #36 | FR-21 |
| 40 | `/Admin/Amenities/Delete/{id}` | Amenities.Delete | POST | → `Index` (`CatalogDeletionService`) | — | — | `data-confirm` | FR-21 |
| 41 | `/Admin/Areas` | Areas.Index | GET\* | `Areas/Index` · `PaginatedList<Area>`; `ViewData["Query"]`, `ViewBag.HomestayCounts` | `_PageHead`, `_SearchBar`, `_RowActions`, `_EmptyState`, `_Pager` | GET: `q`, `page` | `data-confirm` | FR-21 |
| 42 | `/Admin/Areas/Create` | Areas.Create | GET\* | `Areas/Form` · `AreaFormViewModel` | `_ValidationScriptsPartial` | `Name`, `Province`, `AreaGroup` | `data-loading-text` | FR-21 |
| 43 | `/Admin/Areas/Create` | Areas.Create | POST | → `Index` hoặc `Form` | như #42 | như #42 | như #42 | FR-21 |
| 44 | `/Admin/Areas/Edit/{id}` | Areas.Edit | GET\* | `Areas/Form` | như #42 | như #42 | như #42 | FR-21 |
| 45 | `/Admin/Areas/Edit/{id}` | Areas.Edit | POST | → `Index` hoặc `Form` | như #42 | như #42 | như #42 | FR-21 |
| 46 | `/Admin/Areas/Delete/{id}` | Areas.Delete | POST | → `Index` (`CatalogDeletionService`) | — | — | `data-confirm` | FR-21 |
| 47 | `/Admin/Holidays` | Holidays.Index | GET\* | `Holidays/Index` · `PaginatedList<Holiday>`; `ViewData["Query"/"Year"]`, `ViewBag.Years` | `_PageHead`, `_RowActions`, `_EmptyState`, `_Pager` | GET: `year`, `q`, `page` | `data-confirm` | FR-21, BR-08 |
| 48 | `/Admin/Holidays/Create` | Holidays.Create | GET\* | `Holidays/Form` · `HolidayFormViewModel` | `_ValidationScriptsPartial` | `Date`, `Name` | `data-loading-text` | FR-21 |
| 49 | `/Admin/Holidays/Create` | Holidays.Create | POST | → `Index` hoặc `Form` | như #48 | như #48 | như #48 | FR-21 |
| 50 | `/Admin/Holidays/Edit/{yyyy-MM-dd}` | Holidays.Edit | GET\* | `Holidays/Form` (ô `Date` bị khóa) | như #48 | như #48 | như #48 | FR-21 |
| 51 | `/Admin/Holidays/Edit/{yyyy-MM-dd}` | Holidays.Edit | POST | → `Index` hoặc `Form` | như #48 | như #48 | như #48 | FR-21 |
| 52 | `/Admin/Holidays/Delete/{yyyy-MM-dd}` | Holidays.Delete | POST | → `Index` (`CatalogDeletionService`) | — | — | `data-confirm` | FR-21 |
| 53 | `/Admin/RoomCategories` | RoomCategories.Index | GET\* | `RoomCategories/Index` · `PaginatedList<RoomCategory>`; `ViewData["Query"]`, `ViewBag.RoomCounts` | `_PageHead`, `_SearchBar`, `_RowActions`, `_EmptyState`, `_Pager` | GET: `q`, `page` | `data-confirm` | FR-21 |
| 54 | `/Admin/RoomCategories/Create` | RoomCategories.Create | GET\* | `RoomCategories/Form` · `RoomCategoryFormViewModel` | `_ValidationScriptsPartial` | `Name` | `data-loading-text` | FR-21 |
| 55 | `/Admin/RoomCategories/Create` | RoomCategories.Create | POST | → `Index` hoặc `Form` | như #54 | như #54 | như #54 | FR-21 |
| 56 | `/Admin/RoomCategories/Edit/{id}` | RoomCategories.Edit | GET\* | `RoomCategories/Form` | như #54 | như #54 | như #54 | FR-21 |
| 57 | `/Admin/RoomCategories/Edit/{id}` | RoomCategories.Edit | POST | → `Index` hoặc `Form` | như #54 | như #54 | như #54 | FR-21 |
| 58 | `/Admin/RoomCategories/Delete/{id}` | RoomCategories.Delete | POST | → `Index` (`CatalogDeletionService`) | — | — | `data-confirm` | FR-21 |
| 59 | `/Admin/Vouchers` | Vouchers.Index | GET\* | `Vouchers/Index` · `PaginatedList<VoucherRow>`; `ViewData["Query"/"Status"]` | `_PageHead`, `_EmptyState`, `_Pager` | GET: `status`, `q`, `page`; `Revoke`: `reason` (hidden "Quản lý thu hồi") | `data-confirm` | FR-10 (BR-11, D14) |
| 60 | `/Admin/Vouchers/Create` | Vouchers.Create | GET\* | `Vouchers/Create` · `VoucherIssueViewModel` (+ `@inject IOptions<VoucherOptions>`) | `_ValidationScriptsPartial` | `BookingCode`, `Reason` (+ query `bookingCode`) | `data-loading-text` | FR-10 |
| 61 | `/Admin/Vouchers/Create` | Vouchers.Create | POST | → `Index` hoặc `Create` | như #60 | như #60 | như #60 | FR-10 |
| 62 | `/Admin/Vouchers/Revoke/{id}` | Vouchers.Revoke | POST | → `Index` | — | `reason` | `data-confirm` | FR-10 |

### 1.3. Area `Accounting` (policy `Accounting`): 14 route

Mọi trang đều nhận `ViewData["Counts"]` (`AccountingCounts`) từ `LoadCountsAsync`, dùng cho `_AccountingTabs` và sidebar.

| # | URL | Controller.Action | Method | Quyền thêm | View · @model | Partial | Trường form | Hook `data-*` | FR |
|---|---|---|---|---|---|---|---|---|---|
| 63 | `/Accounting` | Dashboard.Index | GET\* | — | `Dashboard/Index` · không model | `_AccountingTabs` | — | — | FR-17 |
| 64 | `/Accounting/AdditionalDeposits` | AdditionalDeposits.Index | GET\* | `ChangeRequestFinance` (chỉ Accountant) | `AdditionalDeposits/Index` · `PaginatedList<Payment>` | `_PageHead`, `_AccountingTabs`, `_EmptyState`, `_Pager` | `page` | `data-additional-review` | FR-05, FR-17 |
| 65 | `/Accounting/AdditionalDeposits/Confirm/{id}` | AdditionalDeposits.Confirm | POST | như #64 | → `Index` | — | `Note` (`ChangeRequestDecisionForm`) | — | FR-05 |
| 66 | `/Accounting/AdditionalDeposits/Reject/{id}` | AdditionalDeposits.Reject | POST | như #64 | → `Index` | — | `Note` | — | FR-05 |
| 67 | `/Accounting/Receipts` | Receipts.Index | GET\* | — | `Receipts/Index` · `PaginatedList<ReceiptRow>`; `ViewData["Status"/"AlertMinutes"/"MaxRejections"]` | `_PageHead`, `_AccountingTabs`, `_EmptyState`, `_Pager` | GET: `status`, `page` | `data-confirm`, `data-loading-text`; `<datalist id="reject-reasons">` | FR-17 |
| 68 | `/Accounting/Receipts/Approve/{id}` | Receipts.Approve | POST | — | → `Index` | — | — | `data-confirm` | FR-17 |
| 69 | `/Accounting/Receipts/Reject/{id}` | Receipts.Reject | POST | — | → `Index` | — | `reason` | `data-confirm` | FR-17 |
| 70 | `/Accounting/Refunds` | Refunds.Index | GET\* | — | `Refunds/Index` · `PaginatedList<RefundRow>`; `ViewData["Done"/"Query"]` | `_PageHead`, `_AccountingTabs`, `_EmptyState`, `_Pager` | GET: `done`, `q`, `page` | `data-confirm` | FR-17 |
| 71 | `/Accounting/Refunds/Record/{id}` | Refunds.Record | POST | — | → `Index` | — | `bankRef` | `data-confirm` | FR-17 |
| 72 | `/Accounting/Transactions` | Transactions.Index | GET\* | — | `Transactions/Index` · `PaginatedList<BankTransactionRow>`; `ViewData["Status"/"All"/"Query"]` | `_PageHead`, `_AccountingTabs`, `_EmptyState`, `_Pager` | GET: `status`, `all`, `q`, `page` | — | FR-17 |
| 73 | `/Accounting/Transactions/Process/{id}` | Transactions.Process | GET\* | — | `Transactions/Process` · `TransactionProcessViewModel` | `_AccountingTabs` | `Match`: `bookingCode`, `note`; `Refund`: `bankRef`, `note`; `Ignore`: `note` | `data-fill`, `data-value`, `data-confirm`, `data-loading-text` | FR-17 |
| 74 | `/Accounting/Transactions/Match/{id}` | Transactions.Match | POST | — | → `Index` hoặc `Process` | — | `bookingCode`, `note` | — | FR-17 |
| 75 | `/Accounting/Transactions/Refund/{id}` | Transactions.Refund | POST | — | → `Index` hoặc `Process` | — | `bankRef`, `note` | `data-confirm` | FR-17 |
| 76 | `/Accounting/Transactions/Ignore/{id}` | Transactions.Ignore | POST | — | → `Index` hoặc `Process` | — | `note` | `data-confirm` | FR-17 |

### 1.4. Area `Owner`: 8 route

| # | URL | Controller.Action | Method | Quyền | View · @model | Partial | Trường form | Hook `data-*` | FR |
|---|---|---|---|---|---|---|---|---|---|
| 77 | `/Owner` | Dashboard.Index | GET\* | Role `Owner` (chưa duyệt vẫn vào được) | `Dashboard/Index` · `OwnerDashboardViewModel` (record đặt trong file controller) | `_EmptyState` | — | — | FR-25 (trạng thái hồ sơ), FR-16 |
| 78 | `/Owner/Bookings` | Bookings.Index | GET\* | `ApprovedOwner` | `Bookings/Index` · `PaginatedList<OwnerBookingRow>`; `ViewData["Status"/"Query"]` | `_PageHead`, `_EmptyState`, `_Pager` | GET: `status`, `q`, `page` | — | FR-16 |
| 79 | `/Owner/Bookings/Details/{id}` | Bookings.Details | GET\* | `ApprovedOwner` | `Bookings/Details` · `OwnerBookingDetailsViewModel` | `_PageHead`, `_ChangeHistory` | `Confirm`: `roomId`; `Reject`: `reason` | `data-confirm` | FR-15, FR-16 |
| 80 | `/Owner/Bookings/Confirm/{id}` | Bookings.Confirm | POST | `ApprovedOwner` | → `Details` | — | `roomId` | — | FR-15 |
| 81 | `/Owner/Bookings/Reject/{id}` | Bookings.Reject | POST | `ApprovedOwner` | → `Details` | — | `reason` | `data-confirm` | FR-15 |
| 82 | `/Owner/Bookings/CheckIn/{id}` | Bookings.CheckIn | POST | `ApprovedOwner` | → `Details` | — | — | — | FR-15 |
| 83 | `/Owner/Bookings/Complete/{id}` | Bookings.Complete | POST | `ApprovedOwner` | → `Details` | — | — | `data-confirm` | FR-15 |
| 84 | `/Owner/Bookings/NoShow/{id}` | Bookings.NoShow | POST | `ApprovedOwner` | → `Details` | — | — | `data-confirm` | FR-15 |

### 1.5. Area `Staff`: 8 route

| # | URL | Controller.Action | Method | Quyền | View · @model | Partial | Trường form | Hook `data-*` | FR |
|---|---|---|---|---|---|---|---|---|---|
| 85 | `/Staff` | Dashboard.Index | GET\* | `Employee` | `Dashboard/Index` · `Dictionary<BookingStatus,int>`; `ViewBag.ActiveHomestayCount` | — | — | — | FR-09, FR-11 |
| 86 | `/Staff/ChangeRequests` | ChangeRequests.Index | GET\* | `Employee` | `ChangeRequests/Index` · `PaginatedList<Booking>`; `ViewData["Query"]` | `_PageHead`, `_EmptyState`, `_Pager` | GET: `q`, `page` | — | FR-10 |
| 87 | `/Staff/ChangeRequests/Create/{id}` | ChangeRequests.Create | GET\* | `Employee` | `~/Views/Bookings/Change.cshtml` · `ChangeRequestForm` (`IsStaff` → `_DashboardLayout`) | `_PageHead`, `_ChangeQuote` | như #17 (có thêm loại `Cancel`) | như #17 | FR-05, FR-10 |
| 88 | `/Staff/ChangeRequests/Quote/{id}` | ChangeRequests.Quote | POST | `Employee` | `~/Views/Bookings/Change.cshtml` | như #87 | như #87 | như #87 | FR-10 |
| 89 | `/Staff/ChangeRequests/Submit/{id}` | ChangeRequests.Submit | POST (`formaction`) | `Employee` | → `Index?q={code}` hoặc `Change` | như #87 | như #87 | như #87 | FR-10 |
| 90 | `/Staff/ChangeApprovals` | ChangeApprovals.Index | GET\* | `ChangeRequestApprove` (chỉ Manager) | `ChangeApprovals/Index` · `PaginatedList<ChangeRequestHistoryRow>` | `_PageHead`, `_ChangeHistory`, `_ChangeQuote`, `_EmptyState`, `_Pager` | `Note` | `data-approval` | FR-05, FR-10 |
| 91 | `/Staff/ChangeApprovals/Approve/{id}` | ChangeApprovals.Approve | POST | như #90 | → `Index` | — | `Note` | — | FR-05 |
| 92 | `/Staff/ChangeApprovals/Reject/{id}` | ChangeApprovals.Reject | POST | như #90 | → `Index` | — | `Note` | — | FR-05 |

**Đối chiếu số route:** khu khách 33 + Admin 29 + Accounting 14 + Owner 8 + Staff 8 = **92**. Con số này bằng số action public trích từ controller, không tính `Controllers/Api` (cách đếm ở mục 7).

### 1.6. View không phải trang (partial/layout)

| File | Model | Được gọi ở | Ghi chú |
|---|---|---|---|
| `Shared/_Layout` | — | `_ViewStart` khu khách, `Bookings/Change` (khách) | Header, footer, section `Head`/`Hero`/`Scripts`, `ViewData["MainClass"]`; `style="z-index"` và `style="border-color:rgba"` |
| `Shared/_DashboardLayout` | — | `_ViewStart` 4 area, `Bookings/Change` (nhân viên) | `@inject ApplicationDbContext` và `IAuthorizationService` để đếm yêu cầu đổi/cọc bổ sung **ngay trong layout**; dựng sidebar `SidebarLink` theo `Workspaces.For(User)` |
| `Shared/_HeadAssets` | — | 2 layout | Script chống nháy theme (`fx-theme`, `fx-sidebar`, `fx-intro`), Google Fonts, Bootstrap (local), Bootstrap Icons (CDN jsdelivr), `tokens.css`, `site.css`; `meta theme-color` có 2 mã hex |
| `Shared/_Alerts` | TempData `Success`/`Error` | 2 layout | `alert tone-emerald/rose` |
| `Shared/_LoginPartial` | — | 2 layout | `ViewData["UserMenuCompact"]`; form `Account/Logout` |
| `Shared/_LanguageSwitch` | — | 2 layout | Form `Language/Set`; xử lý cả URL gốc khi trang được re-execute |
| `Shared/_ThemeToggle` | — | 2 layout | `data-theme-toggle`, `data-label-dark/-light`; `ViewData["ThemeToggleClass"]` |
| `Shared/_PageHead` | `PageHeadModel` | 13 trang | Nút "Thêm mới" luôn trỏ `asp-action="Create"` |
| `Shared/_Pager` | `IPaginatedList` | 15 trang | Giữ query string, bỏ các key route |
| `Shared/_RowActions` | `RowActionsModel` | 4 trang Index danh mục | Sửa/Xóa, `data-confirm` |
| `Shared/_SearchBar` | `ViewData["Query"]` | 3 trang Index danh mục | **Trùng tên** với `_SearchBar` (thanh tìm phòng) mà `ui-rebuild.md` yêu cầu, cần quyết định ở bước 1 |
| `Shared/_EmptyState` | — | 12 trang | Chỉ có một câu cố định "Không có dữ liệu." |
| `Shared/_ChangeHistory` | `IReadOnlyList<ChangeRequestHistoryRow>` | `_CustomerChanges`, Owner `Bookings/Details`, `ChangeApprovals/Index` | `data-change-request`, `data-status`; badge trạng thái cố định `tone-sky` |
| `Shared/_ChangeQuote` | `BookingChangeQuote` | `Bookings/Change`, `ChangeApprovals/Index` | `data-quote-total` (test đọc thuộc tính này) |
| `Shared/_PolicyRules` | `IReadOnlyList<PolicyRuleView>` | `Homestays/Details` | badge % hoàn |
| `Shared/_ValidationScriptsPartial` | — | 11 trang | jQuery Validation + Unobtrusive |
| `Bookings/_CustomerChanges` | `BookingDetailsViewModel` | `Bookings/Details` | QR cọc bổ sung, upload bill CR |
| `Accounting/Shared/_AccountingTabs` | `ViewData["Counts"]` | 6 trang Accounting | Tab có số đếm; ẩn "Cọc bổ sung" nếu không phải Accountant |

### 1.7. Đường dẫn service đã dùng trong thông báo nhưng chưa có route

Các service ghi URL sau vào `Notification.Url`. Màn mới ở bước 5 nên dùng **đúng đường dẫn này**, nếu không thì phải ghi vào `decisions.md`.

| URL trong service | Nguồn | Bước dự kiến |
|---|---|---|
| `/Owner/Homestays/{id}` | `HomestayReviewService` | 05a |
| `/Owner/Settlements/{id}` | `SettlementService` | 05a |
| `/Owner/Bookings?ownerId={id}&arrivals={yyyy-MM-dd}` | `ReminderService.OwnerUrl` (route có sẵn nhưng chưa đọc 2 tham số này) | 05a |
| `/Staff/Bookings/{id}` | `StaffBookingService`, `BookingService` | 05b |
| `/Staff/Homestays/{id}` | `HomestayContentService`/`HomestayReviewService` | 05b |
| `/Staff/OwnerRegistrations/{id}` | `OwnerRegistrationService.RegistrationUrl` | 05b |
| `/Staff/Contracts/{id}` | `ContractService.ExpireAsync` | 05d (đặt hợp đồng ở area Staff hay Admin cần quyết định) |
| `/Accounting/Settlements/{id}` | `SettlementService` | 05c |
| `/Bookings/Details/{id số}` | `OverbookingService` (dòng 73, 108, 123) | **lỗi backend**, xem `backend-gaps.md` G-08 |

## 2. Mẫu UI lặp lại (≥ 2 view)

Xếp theo số view dùng. Cột "Component" lấy tên trong danh mục của `ui-rebuild.md`.

| # | Mẫu | Số view | View đang dùng | Component đề xuất |
|---|---|---|---|---|
| 1 | Tiền `x.ToString("N0") + "đ"`, `{0:N0}đ`, hàm cục bộ `Money()` | 18 view | Home/Index, Homestays/Index, Homestays/Details, Bookings/Create (+JS), Bookings/Details, Bookings/Mine, `_CustomerChanges`, `_ChangeQuote`, Accounting Dashboard/AdditionalDeposits/Receipts/Refunds/Transactions Index/Process, Owner Bookings Index/Details, Admin Vouchers Index/Create | `<money>` |
| 2 | Khối nhập `div.mb-3 > label asp-for + input asp-for + span asp-validation-for.text-danger.small` | 13 view | Account Login/Register/EmployeeLogin/ChangePassword, Bookings Lookup/Create, Admin Amenities/Areas/Holidays/RoomCategories Form, Vouchers/Create, Dev/SimulateBank, Bookings/Change (không có span validation) | `form-field` |
| 3 | Badge trạng thái `span.badge-tone @X.ToneClass()` + `L[X.DisplayName()]` | 11 view (+`_ChangeHistory` tự đặt `tone-sky`) | Bookings Details/Mine, Accounting Receipts/Refunds/Transactions Index/Process, Owner Bookings Index/Details, Owner Dashboard, Admin Vouchers, Staff Dashboard | `<status-badge>` |
| 4 | Bảng `card-soft overflow-hidden > table-responsive > table.table-modern` + `_EmptyState` + `_Pager` | 10 view | Bookings/Mine, Accounting Refunds/Transactions Index, Admin Amenities/Areas/Holidays/RoomCategories/Vouchers Index, Owner Bookings Index, Owner Dashboard | `_DataTable` (+ `_RowActions`, `_Pager`) |
| 5 | Callout `div.alert.tone-* > i.bi + div` | 10 view | Bookings/Details (7 trạng thái), Bookings/Create, Homestays/Details, `_CustomerChanges`, Staff Dashboard, Staff ChangeApprovals, Owner Dashboard; validation summary `alert tone-rose` ở 7 form | `_Alerts`/callout (mục "Phản hồi") |
| 6 | Khoảng ngày `CheckIn.ToString("dd/MM") – CheckOut.ToString("dd/MM/yyyy")`, kèm số đêm | 9 view | Bookings Mine/Details/Change, `_ChangeHistory`, Owner Bookings Index/Details, Staff ChangeRequests, Homestays/Index (search summary), Admin Holidays (ngày đơn) | `<date-range>` |
| 7 | Thanh lọc `form.card-soft.filter-bar` gồm select trạng thái + ô tìm `input-group` + nút "Lọc" | 8 view | Accounting Refunds/Transactions, Admin Holidays/Vouchers, Owner Bookings, Homestays/Index, `_SearchBar` (Admin), Staff ChangeRequests (biến thể tự viết) | `_FilterBar` |
| 8 | Danh sách mô tả `dl.row > dt.col-sm-4 + dd.col-sm-8` và `dl.receipt-facts`, `dl.transfer-info` | 7 view | Bookings/Details (3 khối), Owner Bookings/Details, Accounting AdditionalDeposits/Receipts/Process, `_ChangeQuote` | `_DescriptionList` |
| 9 | Đầu trang tiêu đề + mô tả + nút chính (`_PageHead` hoặc `div.page-head` tự viết) | 15 view | `_PageHead` ở 13 view; tự viết ở Homestays/Index, Bookings/Mine | `_PageHead` (bỏ `icon-chip` màu theo module) |
| 10 | Form quyết định một dòng `form.d-flex.gap-2 > label.visually-hidden + input name=Note/reason/bankRef required + button` | 5 view | Accounting AdditionalDeposits (×2), Receipts (reject), Refunds (record), Staff ChangeApprovals (×2), Transactions/Process (ignore) | Hộp xác nhận + `_RowActions` biến thể "kèm ghi chú" |
| 11 | Thẻ số liệu `card-soft stat-card tone-*` (icon → số → nhãn) | 3 view (+ biến thể `status-group` ở Staff) | Admin/Owner/Accounting Dashboard; Staff Dashboard | `_StatCard` |
| 12 | Banner đầu dashboard `page-banner banner-*` (gradient) | 4 view | Admin/Staff/Owner/Accounting Dashboard | Bỏ, thay bằng `_PageHead` (DESIGN.md: không gradient) |
| 13 | Thẻ form có tiêu đề `card-soft (card-accent-top tone-*) > icon-chip + h1` | 9 view | Admin 4 Form, Vouchers/Create, Account/ChangePassword, Bookings/Lookup, Dev/SimulateBank, Transactions/Process | `_PageHead` + bố cục form 40rem |
| 14 | Bộ chọn ngày `input[type=date] CheckIn/CheckOut` + JS chặn ngày | 5 view | Home/Index, Homestays Index/Details, Bookings/Create, Bookings/Change (`NewCheckIn/NewCheckOut`) | `_DateRangePicker` (Flatpickr) |
| 15 | Thẻ homestay `a.stay-card > .stay-media + .stay-body` | 2 view | Home/Index (có rating sao), Homestays/Index | `_HomestayCard` |
| 16 | Bảng giá theo đêm `ul.night-list` + dòng tổng/cọc | 3 view (+JS) | Bookings/Create (server + JS dựng lại), Bookings/Details, `_ChangeQuote` | `_PriceBreakdown` |
| 17 | Khối QR + thông tin chuyển khoản | 2 view | Bookings/Details, `_CustomerChanges` | `_PaymentQr` |
| 18 | Form tải ảnh bill `input[type=file][accept=image/*]` + `transferContent` | 2 view | Bookings/Details, `_CustomerChanges` | `_UploadZone` |
| 19 | Đếm ngược `data-expires` (2 bản JS gần giống nhau) | 2 chỗ | Bookings/Details (`#countdown`, `[data-change-countdown]`) | `js/components/countdown.js` |
| 20 | Lịch sử trạng thái (danh sách theo thời gian) | 2 view | Owner Bookings/Details (`Histories`), `_ChangeHistory` | `_Timeline` |
| 21 | Thẻ đăng nhập hai cột `auth-card > auth-side + auth-form` | 3 view | Account Login/Register/EmployeeLogin | `_AuthLayout` |
| 22 | Trang trạng thái `status-page > status-code` | 2 view | Home/StatusCode, Account/AccessDenied | `_AuthLayout` + trang lỗi |
| 23 | Tab/bộ chọn trạng thái dạng nút | 2 view | `_AccountingTabs`, Accounting Receipts (nút lọc) | `_Tabs` |
| 24 | Breadcrumb `nav > ol.breadcrumb` | 3 view | Homestays/Details, Bookings/Create, `_DashboardLayout` | `_Breadcrumb` |
| 25 | `icon-chip(-lg) tone-*` trang trí | 19 view | Hầu hết trang nội bộ và trang form | `<icon>` + một tông sage (DESIGN.md: icon không mang nghĩa trạng thái thì không tô màu) |

**Top 10 dùng cho báo cáo:** `<money>`, `form-field`, `<status-badge>`, `_DataTable`, callout/`_Alerts`, `<date-range>`, `_FilterBar`, `_DescriptionList`, `_PageHead`, form quyết định một dòng.

## 3. CSS và JS

### 3.1. File CSS

| File | Dòng | Nạp ở | Class | Còn dùng | Chết thật | Mã hex |
|---|---|---|---|---|---|---|
| `tokens.css` | 127 | `_HeadAssets` | 10 (`tone-*`) | 10 | 0 | 56 (đúng chỗ) |
| `site.css` | 620 | `_HeadAssets` | 66 | 61 | 2 | 20 |
| `public.css` | 1193 | `_Layout` | 100 | 95 | 5 | 33 |
| `dashboard.css` | 446 | `_DashboardLayout` | 44 | 44 | 0 | 2 |
| `accounting.css` | 165 | `_DashboardLayout` (mọi area, không riêng Accounting) | 14 | 14 | 0 | 2 |
| `motion.css` | 213 | 2 layout | 25 | 25 | 0 | 0 |

Cách đếm: tách selector `.class` trong từng file, bỏ comment, rồi tìm nguyên từ trong `**/*.cshtml`, `**/*.cs` và `wwwroot/js/*.js`. Script ở `scratchpad/css_usage.py` không lưu vào repo.

**Class chết thật** (xóa được ở bước 1):
- `public.css`: `display-title`, `stay-cover`, `stay-cover-empty`, `summary-due`, `summary-thumb`.
- `site.css`: `form-select-sm`, `text-body-secondary` (ghi đè utility Bootstrap nhưng không view nào dùng).

**Trông như chết nhưng phải giữ** vì class được sinh lúc chạy:
- `field-validation-error`, `input-validation-error`, `validation-summary-valid` do Tag Helper validation sinh ra.
- `modal-backdrop`, `offcanvas-backdrop` do Bootstrap JS sinh ra.
- `is-loading`, `is-swapping`, `is-broken`, `sidebar-collapsed` do `site.js` và `_HeadAssets` gắn vào.

**Class đang dùng, theo nhóm** (bản đầy đủ từng class → view ở mục 7):
- **`tokens.css`:** `tone-amber/emerald/indigo/orange/pink/rose/sky/slate/teal/violet`. Được dùng qua `*StatusExtensions.ToneClass()` và tô màu tùy ý theo module (Admin Amenities xoay vòng 8 tone theo `Id`).
- **`site.css`:** nền Bootstrap đã chỉnh (`btn-brand`, `btn-accent`, `btn-ghost`, `btn-icon`, `btn-light-glass`, `card-soft`, `card-accent-top`, `badge-tone`, `count-pill`, `icon-chip(-lg)`, `filter-bar`, `empty-state`, `page-head`, `section-title`, `table-modern`, `rich-text`, `booking-code`, `price`, `text-brand`, `text-accent`, `avatar`, `lang-switch`, `theme-toggle`, `user-menu-name`, `alert`, `pagination`…).
- **`public.css`:** trang khách:
  - header/footer: `site-header`, `site-nav`, `site-footer`, `footer-muted`, `brand-mark`, `brand-name`;
  - trang chủ: `hero-*`, `promise-list`, `area-grid`, `area-tile`, `home-section`, `section-head`, `steps`, `step-number`;
  - thẻ homestay: `stay-card`, `stay-media`, `stay-body`, `stay-title`, `stay-meta`, `stay-price`, `stay-rating`, `stay-fallback`, `stay-grid`;
  - chi tiết homestay: `stay-heading`, `stay-facts`, `stay-gallery`, `gallery-*`, `lightbox(-nav)`, `room-offer`, `room-quote`, `price-table`, `amenity-list`, `policy-rules`, `search-summary`;
  - đặt phòng và đơn: `booking-panel`, `form-section`, `night-list`, `order-head`, `order-progress`, `is-done`, `is-current`, `pay-card`, `pay-steps`, `qr-frame`, `qr-image`, `transfer-info`, `countdown`, `is-urgent`, `receipt-box`, `dev-box`, `summary-row`, `summary-total`;
  - đăng nhập và trang lỗi: `auth-card`, `auth-side`, `auth-form`, `narrow-card`, `status-page`, `status-code`.
- **`dashboard.css`:** khung nội bộ: `app-shell`, `app-sidebar*`, `app-topbar`, `app-main`, `app-content`, `sidebar-*`, `topbar-actions`, `page-banner`, `banner-admin/staff/owner/accounting`, `stat-card`, `stat-value`, `stat-label`, `status-group`, `status-list`, `status-total`, `tile-link`, `tile-arrow`, `min-w-0`.
- **`accounting.css`:** `accounting-tabs`, `accounting-tab`, `accounting-count`, `receipt-card`, `receipt-thumb`, `receipt-body`, `receipt-facts`, `is-overdue`, `refund-ref`, `suggestion(-list)`, `stat-link`, `tx-ref`. Riêng `receipt-facts` còn được dùng ở `Transactions/Process`.
- **`motion.css`:** gắn hiệu ứng vào class có sẵn (`btn`, `dropdown-menu`, `modal`, `app-sidebar`, `stay-card`, `stay-media`, `gallery-item`, `hero-copy`, `hero-search`, `lightbox`, `alert-dismissible`, `fx-intro`, `is-loading`, `is-swapping`…).

**Chỗ lệch `DESIGN.md` đang có** (để bước 1–4 sửa):
- 57 mã hex ngoài `tokens.css`: `site.css` 20, `public.css` 33, `dashboard.css` 2, `accounting.css` 2. Ngoài ra `_HeadAssets` có 2 mã hex trong `meta theme-color`.
- 3 chỗ `style="` trong view (`_Layout` ×2, `_DashboardLayout` ×1).
- `page-banner banner-*` dùng gradient. Nhiều `icon-chip tone-*` tô màu theo module, trái quy tắc "yên tĩnh ở dashboard".
- `Home/Index` hiện điểm đánh giá bằng `bi-star-fill` (DESIGN.md cấm sao vàng).
- Ảnh hero `images/demo/hero-wooden-cabin.webp` có giấy phép **CC BY 2.0**, không thuộc CC0/public domain như `ui-rebuild.md` yêu cầu. Ảnh demo homestay (`images/demo/*`, nguồn ở `images/demo/CREDITS.md`) là dữ liệu seed, đi qua `ImageUrlExtensions`.
- `Shared/Error.cshtml` là template mặc định, chữ tiếng Anh viết cứng. `Dev/SimulateBank` cố ý không dịch.
- Chưa có Flatpickr và Chart.js trong `wwwroot/lib` (chỉ có `bootstrap`, `jquery`, `jquery-validation`, `jquery-validation-unobtrusive`). Bootstrap Icons nạp từ CDN.
- Một số view lấy "hôm nay" bằng `DateTime.Today` của máy chủ (Home/Index, Homestays Index/Details, Bookings/Create) thay vì `VietnamTime.Today`.

### 3.2. Hành vi trong `site.js`

| Hành vi (dòng) | Hook | View đang gọi |
|---|---|---|
| Xác nhận trước khi submit (1–6) | `form[data-confirm]` | `_RowActions` (4 Index danh mục), Admin Vouchers/Index, Accounting Receipts/Refunds/Transactions Process, Owner Bookings/Details, Bookings Details/Change |
| Nút đang xử lý, chặn bấm hai lần (8–21) | `button[type=submit][data-loading-text]` | Account ×4, Bookings Create/Details/Lookup, Admin 4 Form + Vouchers/Create, Accounting Receipts/Process |
| Hiện/ẩn mật khẩu (23–32) | `[data-toggle-password]` + `data-label-show/-hide` | Account Login, EmployeeLogin |
| Sáng/tối (34–64) | `[data-theme-toggle]` + `data-label-dark/-light`; localStorage `fx-theme` | `_ThemeToggle` (2 layout) |
| Thu gọn sidebar (66–73) | `[data-sidebar-collapse]`; localStorage `fx-sidebar` | `_DashboardLayout` |
| Ảnh lỗi → khung thay thế (75–82) | `.stay-media > img`, `.gallery-item img` | Home/Index, Homestays Index/Details |
| Lightbox (84–113) | `#lightbox`, `[data-lightbox-src/-index/-alt/-step/-view/-counter]` | Homestays/Details |
| Chặn ngày trả ≤ ngày nhận (115–124) | `form[data-date-range] input[name=CheckIn]` | Home/Index, Homestays Index/Details |
| Hỗ trợ `motion.css` (126–162) | `[data-theme-toggle]` (capture), `img[loading=lazy]`, `#lightbox` | như trên |

**Script viết trong view** (đưa sang `wwwroot/js/components/` khi dài hơn khoảng 40 dòng, theo `ui-rebuild.md`):

| View | Việc | Dòng | Hook |
|---|---|---|---|
| Bookings/Create | Báo giá AJAX `Bookings/Quote`, dựng `#quoteBox` | ~60 | `#CheckIn`, `#CheckOut`, `#VoucherId`, `#quoteBox` |
| Bookings/Details | Sao chép, đếm ngược ×2, hỏi `Bookings/Status` 5 giây/lần | ~55 | `data-copy`, `#countdown[data-expires]`, `[data-change-countdown]` |
| Bookings/Change | Bật/tắt ô ngày/loại phòng theo loại yêu cầu | ~10 | `[data-change-type]`, `[data-change-dates]`, `[data-change-room]` |
| Accounting Transactions/Process | Điền mã đơn từ gợi ý | ~8 | `[data-fill][data-value]` |
| Admin Amenities/Form | Xem trước icon | ~5 | `#Icon`, `#iconPreview` |

`onerror="..."` viết trong thẻ `img.qr-image` (Bookings/Details) trái quy tắc "không dùng inline handler". Các request AJAX hiện có đều là GET nên chưa cần header `RequestVerificationToken`.

## 4. Màn còn thiếu theo FR-01…FR-27

Cách đọc bảng:
- **Có**: màn đã có, chỉ cần đổi giao diện ở bước 3–4.
- **Một phần**: màn đã có nhưng thiếu chức năng FR yêu cầu.
- **Thiếu**: chưa có controller/view.
- **Hệ thống**: FR do job hoặc service tự chạy, không cần màn.

Cột "Policy nên dùng" ghi policy cho controller. Service tự kiểm chức vụ chi tiết nên view chỉ ẩn/hiện nút, không quyết định quyền. Mã gap trỏ sang `backend-gaps.md`.

| FR | Màn | Vai trò | Hiện trạng | Service + method public đã có | Policy nên dùng | Bước | Gap |
|---|---|---|---|---|---|---|---|
| FR-01 | Tìm homestay: lọc giá min, loại phòng, tiện nghi, sắp xếp | Khách | Một phần (view chỉ có `Q`, `AreaId`, ngày, `Guests`, `MaxPrice`) | `AvailabilityService.SearchAsync(HomestaySearchQuery)` đã nhận `MinPrice`, `CategoryId`, `AmenityIds`, `Sort` | công khai | 03 | G-06 |
| FR-02 | Chi tiết: đánh giá, điểm tổng, lịch trống | Khách | Một phần (chưa hiện đánh giá, chưa có lịch) | `ReviewService.ListAsync(homestayId, page)`; `PricingService.QuoteAsync`; `RefundService`; `AvailabilityService` | công khai | 03 | G-07 (lịch công khai) |
| FR-03 | Đặt phòng không đăng nhập | Khách | Có | `BookingsController.Create` + `GuestCustomerService.FindOrCreateAsync` + `BookingService.CreateAsync` | công khai | 03 | — |
| FR-04 | Tra cứu, xem đơn, trạng thái cọc | Khách | Có (Zalo/SMS thật: rút gọn đã chấp thuận) | `Bookings.Lookup/Details/Status` | công khai | 03 | — |
| FR-05 | Đổi/hủy, xem trước tiền hoàn | Khách | Có | `ChangeRequestUiService.PrepareAsync/QuoteAsync/SubmitAsync/CustomerAsync`; `RefundService.QuoteCancellationAsync` | Role `Customer` | 03 | — |
| FR-06 | Khách viết đánh giá sau trả phòng | Khách | Thiếu | `ReviewService.CreateAsync(bookingId, rating, comment, actor)`, `CreateByLookupAsync(code, phone, rating, comment)` | đúng khách của đơn hoặc mã trong session tra cứu (như `Bookings.Details`) | 03 | G-09 (biết đơn đã đánh giá chưa) |
| FR-06 | Chủ xem và trả lời đánh giá | Chủ | Thiếu | `ReviewService.ListAsync(homestayId, page, includeHidden:false, actor)`, `ReplyAsync(id, reply, actor)` | `ApprovedOwner` | 05a | G-02 (danh sách homestay của chủ) |
| FR-06 | Quản lý ẩn/hiện đánh giá | Quản lý | Thiếu | `ReviewService.SetHiddenAsync(id, hidden, reason, actor)`, `ListAsync(..., includeHidden:true, actor)` | `Management` | 05d | — |
| FR-07 | Đặt hộ khách | Nhân viên | Thiếu | `StaffBookingService.CreateForCustomerAsync(StaffBookingInput, actor)` (tìm/tạo khách theo SĐT bên trong); `CustomerService.SearchAsync`; `AvailabilityService.StaffAvailabilityAsync`; `PricingService.QuoteAsync` | `Employee` (service: Consultant/Manager/Administrator) | 05b | — |
| FR-08 | Phòng trống toàn hệ thống | Nhân viên | Thiếu | `AvailabilityService.StaffAvailabilityAsync(checkIn, checkOut, areaId, guests, actor)` (≤ 31 đêm) | `Employee` | 05b | G-05 (lịch tháng cho Tư vấn) |
| FR-09 | Quản lý đơn: tìm, chi tiết, giao, đổi trạng thái | Nhân viên | Thiếu | `StaffBookingService.SearchAsync(StaffBookingQuery, actor)`, `DetailsAsync(id, actor)`, `AssignAsync(id, employeeId, actor)`, `ChangeStatusAsync(id, to, newRoomId, note, actor)` | `Employee` | 05b | G-04 (danh sách người nhận) |
| FR-10 | Trùng phòng và phương án thay thế | Nhân viên/Quản lý | Thiếu (gửi đổi/hủy thay khách đã có) | `OverbookingService.ReportAsync`, `AlternativesAsync`, `MoveRoomAsync`, `ReplaceAsync`, `RefundAsync` (chỉ Manager/Administrator); `ChangeRequestUiService` | `Employee`; nút hoàn chỉ hiện cho Manager | 05b | G-08 (link thông báo) |
| FR-11 | Cảnh báo đơn quá 30 phút chưa xác nhận | Nhân viên | Thiếu | `BookingService.AlertOverdueConfirmationsAsync()` (job, tạo `Notification`); có thể lọc `StaffBookingService.SearchAsync(Status = Deposited)` rồi so `ConfirmDeadlineAt` | `Employee` | 05b | G-01 |
| FR-12 | Khách hàng | Nhân viên | Thiếu | `CustomerService.SearchAsync(q, page, actor)`, `DetailsAsync(id, actor)`, `UpdateAsync(id, CustomerUpdate, actor)` | `Employee` (service: Consultant/Manager/Administrator) | 05b | — |
| FR-13 | Homestay của tôi: thông tin, phòng, giá, ảnh, tiện nghi, chính sách, giấy tờ | Chủ | Thiếu | `HomestayContentService.UpdateAsync/UpdateLocationAsync`; `RoomService.CreateTypeAsync/UpdateTypeAsync/SetTypeActiveAsync/AddRoomAsync/RenameRoomAsync/SetRoomActiveAsync/SetPriceAsync`; `HomestayImageService.UploadAsync/SetCoverAsync/UpdateAsync/DeleteAsync`; `HomestayPolicyService.ReplaceAsync/RemoveAsync`; `HomestayDocumentService.UploadAsync/DeleteAsync`; `HomestayReviewService.CreateAsync/SubmitAsync`; `HomestayAccessService.AuthorizeAsync` | `ApprovedOwner` | 05a | **G-02** |
| FR-14 | Lịch phòng, khóa/mở ngày | Chủ | Thiếu | `RoomBlockService.CalendarAsync(homeId, start, end, actor)` (≤ 62 ngày, ô khóa có `Code = "BL{id}"`), `CreateAsync(roomId, start, end, reason, note, actor)`, `UnlockAsync(blockId, actor)` | `ApprovedOwner` (service cho thêm Manager/Administrator) | 05a | G-02 (danh sách homestay) |
| FR-15 | Thông báo; xác nhận/từ chối đơn | Chủ | Một phần (xác nhận đơn đã có, chưa có danh sách thông báo) | `BookingService.ConfirmAsync/ChangeStatusAsync` (đang dùng); thông báo: **chưa có hàm đọc** | `ApprovedOwner` | 05a | **G-01** |
| FR-16 | Lịch nhận–trả | Chủ | Thiếu | `OwnerScheduleService.ReadAsync(ownerId, from, through, actor, homestayId?)` (≤ 31 ngày) | `ApprovedOwner` | 05a | G-03 (cần `ownerId`) |
| FR-16 | Đối soát của chủ | Chủ | Thiếu | `SettlementService.ListAsync(SettlementQuery, actor)`, `DetailsAsync(id, actor)`, `ConfirmAsync(id, actor)`, `ReportDiscrepancyAsync(id, note, actor)` | `ApprovedOwner` | 05a | — |
| FR-17 | Cọc, bill, giao dịch, hoàn cọc | Kế toán | Có | `AccountingService.*`, `ReceiptReviewService.ApproveAsync/RejectAsync`, `BookingChangeRequestService.ConfirmAsync/RejectFinanceAsync` | `Accounting` | 04 | — |
| FR-18 | Bảng đối soát theo kỳ | Kế toán | Thiếu | `SettlementService.GeneratePeriodAsync(period, actor)`, `GenerateAsync(homestayId, period, actor)`, `ListAsync`, `DetailsAsync`, `SendAsync`, `ReopenAsync`, `RecordPaidAsync(id, bankRef, actor)`; `SettlementService.CanTransition` | `Accounting`; thao tác ghi chỉ Accountant | 05c | G-10 |
| FR-18 | Khoản bù trùng phòng | Kế toán | Thiếu | `CompensationService.PendingAsync(actor)`, `RecordPaidAsync(id, bankRef, actor)`, `RejectAsync(id, reason, actor)` | `Accounting` | 05c | — |
| FR-19 | Đối tác, hợp đồng, hoa hồng | Quản lý | Thiếu | `ContractService.CreateAsync(CreateContractRequest, actor)`, `ExtendAsync(id, endDate, actor)`, `TerminateAsync(id, reason, actor)`; `HomestayReviewService.SuspendAsync/ResumeAsync/EndAsync` | `Management` | 05d | **G-11** |
| FR-20 | Báo cáo | Quản lý (+ NV xem của mình, KT xem tài chính) | Thiếu | `ReportService.ManagementAsync`, `BookingsAsync`, `FinancialAsync`, `EmployeesAsync(q, actor, employeeId?)`, `HomestaysAsync` | `Management` (trang chính); Staff Dashboard gọi `EmployeesAsync` cho chính mình | 05d (+05b dashboard) | — |
| FR-21 | Tài khoản nhân viên | Quản trị | Thiếu | `AccountAdminService.ListAsync(AccountQuery, actor)`, `CreateEmployeeAsync(EmployeeAccountInput, actor)`, `ChangeEmploymentAsync`, `SetLockedAsync`, `ResetEmployeePasswordAsync` | `Management` (service: chỉ Administrator ghi, Manager chỉ đọc) | 05d | G-12 |
| FR-21 | Danh mục | Quản trị | Có | `CatalogDeletionService.DeleteAsync` + CRUD controller cũ | `Management` | 04 | — |
| FR-22 | Khóa/mở lịch tự động | Hệ thống | Hệ thống | `BookingService` | — | — | — |
| FR-23 | Nhắc lịch | Hệ thống | Hệ thống (Zalo/SMS thật rút gọn) | `ReminderService` | — | — | — |
| FR-24 | Môi trường chạy thử | Quản trị | Có (`Dev/SimulateBank`); thêm `Dev/UiKit` ở bước 1 | `SeedData`, `BankMatchingService` | `Management` + chỉ Development | 01 | — |
| FR-25 | Đăng ký đối tác + theo dõi hồ sơ | Chủ chưa hợp tác | Thiếu (Owner Dashboard chỉ hiện trạng thái) | `OwnerRegistrationService.RegisterAsync(input, password)`, `UpdateDraftAsync`, `UploadDocumentAsync`, `DeleteDocumentAsync`, `SubmitAsync`, `ActivateAsync(id, phone, token, password)`; `OwnerRegistrationQueryService.MineAsync(actor)`, `OpenDocumentAsync(documentId, actor)` | `[AllowAnonymous]` (đăng ký, kích hoạt); Role `Owner` (hồ sơ của tôi) | 03 | — |
| FR-26 | Thẩm định hồ sơ đối tác | Nhân viên/Quản lý | Thiếu | `OwnerRegistrationQueryService.ListAsync(RegistrationQuery, actor)`, `DetailsAsync(id, actor)`, `OpenDocumentAsync`; `OwnerRegistrationService.TakeAsync`, `RequestRevisionAsync`, `ApproveAsync`, `RejectAsync`, `InviteAsync`, `ResendInviteAsync`, `CreateAssistedAsync` | `Employee` (service: Consultant/Manager/Administrator; duyệt/từ chối Manager/Administrator) | 05b (+ lối vào 05d) | — |
| FR-27 | Duyệt nội dung homestay | Quản lý | Thiếu | `HomestayReviewService.PendingAsync(actor)`, `ApproveAsync`, `RejectAsync(id, reason, actor)`, `ResumeAsync`; SLA hồ sơ: `OwnerRegistrationService.AlertOverdueAsync` (job) | `Employee` + kiểm Manager/Administrator (service chặn Tư vấn) | 05b | **G-13** |

**Màn thiếu theo bước:**
- **03 (khách):**
  - bổ sung bộ lọc FR-01;
  - đánh giá trên trang chi tiết FR-02;
  - viết đánh giá FR-06;
  - đăng ký đối tác, kích hoạt tài khoản chủ, trang hồ sơ của tôi FR-25.
- **05a (chủ):**
  - Homestay của tôi + 6 tab FR-13;
  - Lịch phòng FR-14;
  - Lịch nhận–trả FR-16;
  - Đối soát FR-16;
  - Đánh giá và trả lời FR-06;
  - Thông báo FR-15;
  - Dashboard mới.
- **05b (nhân viên):**
  - Quản lý đơn FR-09;
  - Đặt hộ FR-07;
  - Phòng trống FR-08;
  - Khách hàng FR-12;
  - Trùng phòng FR-10;
  - Duyệt nội dung FR-27;
  - Thẩm định hồ sơ FR-26;
  - Cảnh báo quá hạn FR-11;
  - Dashboard.
- **05c (kế toán):**
  - Đối soát theo kỳ FR-18;
  - Khoản bù FR-18;
  - Dashboard.
- **05d (quản lý/quản trị):**
  - Đối tác và hợp đồng FR-19;
  - Báo cáo FR-20;
  - Tài khoản nhân viên FR-21;
  - Ẩn/hiện đánh giá FR-06;
  - lối vào FR-26;
  - Dashboard.

## 5. Ràng buộc từ view sang các bước sau

Danh sách sau phải giữ nguyên khi viết lại (ngoài các mục đã ghi trong `ui-rebuild.md`):
- **Tên trường và giá trị test đọc:** `id="bookingForm"`, `name="ContactPhone"`, `name="PreviewToken"`, `data-quote-total`, `data-additional-payment`, `data-change-countdown`, `data-status` (trên khối yêu cầu đổi), `data-approval` (bọc trong `<section>`), `class="status-code"`. Chi tiết ở mục 6.
- **Form tag helper:** phải sinh `<input name="__RequestVerificationToken" type="hidden" value="...">` đúng thứ tự thuộc tính này, vì `TestWebApplicationFactory.TokenFromAsync` tìm bằng regex.
- **Sidebar:** chỉ Manager thấy link `/Staff/ChangeApprovals`, chỉ Accountant thấy `/Accounting/AdditionalDeposits` (`SidebarShowsOnlyPermittedChangeActions`).
- **`Bookings/Change` dùng chung hai layout.** `ChangeRequests.Create/Quote/Submit` (Staff) render `~/Views/Bookings/Change.cshtml`; view tự chọn layout và action theo `Model.IsStaff`.
- **Chuỗi tiếng Anh mà test so khớp:** "Change dates / room type", "Review quote", "Please use the Cancel booking button", "at least 11 days before check-in".
- **Chuỗi tiếng Việt mà test so khớp:** "Chờ quản lý duyệt", "Yêu cầu đổi", "Lịch cũ", "Lịch mới", "Đã nộp cọc bổ sung, chờ kế toán xác nhận.", "Đơn dùng voucher, vui lòng liên hệ nhân viên", "Yêu cầu cũ, cần gửi lại", "Chưa nhận được tiền", "Không tìm thấy trang".

## 6. Test phụ thuộc view

| File test | Loại | Phụ thuộc |
|---|---|---|
| `Integration/AnonymousBookingPageTests.cs` | Render HTML `/Bookings/Create` | `id="bookingForm"`, `name="ContactPhone"`; POST thiếu token phải bị chặn |
| `Integration/ChangeRequestUiTests.cs` | Render HTML nhiều trang (Change, Details, Staff ChangeRequests/ChangeApprovals, Accounting AdditionalDeposits, Owner Bookings/Details) | `name="PreviewToken"` (regex), `data-quote-total`, khối `data-approval="{id}"…</section>`, chuỗi "Chờ quản lý duyệt", "Yêu cầu đổi", "Lịch cũ/Lịch mới", "Yêu cầu cũ, cần gửi lại", "Vui lòng dùng nút hủy đơn"; URL form `/Bookings/SubmitChange/{code}`, `/Staff/ChangeApprovals/Approve/{id}`; redirect `/Account/AccessDenied`, `/Account/Login` |
| `Integration/ChangeRequestUiDisplayTests.cs` | Render HTML | `data-additional-payment`, `data-change-countdown`, `data-status`, `data-quote-total`, chuỗi `CR{id} {code}`, `amount=300000` (URL QR), link `/Bookings/Change/{code}`, sidebar `/Staff/ChangeApprovals` và `/Accounting/AdditionalDeposits`, bản dịch tiếng Anh |
| `Integration/OwnerBookingsAuthorizationTests.cs` | Render HTML trang lỗi | `class="status-code"`, "Không tìm thấy trang" |
| `Integration/TestWebApplicationFactory.cs` | Helper | Đăng nhập qua form thật: trường `PhoneNumber`/`EmployeeCode`/`Password`, regex token antiforgery |
| `Integration/ChangeRequestPoliciesTests.cs` | Chỉ kiểm policy | không đọc HTML |
| `Services/OwnerBookingTests.cs` | Gọi controller, kiểm `ViewResult.Model` | `Areas.Owner.Controllers.BookingsController`; `IEnumerable<OwnerBookingRow>` (record đặt trong file controller) |
| `Services/RoomSearchSelectionTests.cs` | Gọi controller, kiểm `ViewResult.Model` | `HomestaysController.Index/Details` → `HomestaySearchViewModel`, `HomestayDetailsViewModel`; `HomeController.Index` → `HomeIndexViewModel`; giữ chữ ký constructor `HomestaysController(db, PricingService, clock, options)` và `HomeController(db, clock)` |
| `Services/ContractServiceTests.cs` | Gọi `HomestaysController.Index` | `HomestaySearchViewModel` |
| `Services/AvailabilityServiceTests.cs`, `AvailabilitySeedRegressionTests.cs`, `AvailabilityPerformanceTests.cs`, `AnonymousBookingTests.cs`, `BookingContactVisibilityTests.cs` | Dùng ViewModel làm input/output của service | `HomestaySearchQuery`, `HomestaySearchViewModel`, `HomestayCard` (không đọc HTML) |

Hệ quả:
- Đổi tên hoặc chuyển `OwnerBookingRow`, `OwnerBookingDetailsViewModel`, `OwnerDashboardViewModel` khỏi namespace `Areas.Owner.Controllers` sẽ làm hỏng test.
- Thêm tham số bắt buộc vào constructor của `HomestaysController` hoặc `HomeController` cũng làm hỏng test.

## 7. Cách kiểm và tái lập

- **Đếm route:** tìm `public (async )?(Task<IActionResult>|IActionResult) X(` trong `Controllers/*.cs` và `Areas/*/Controllers/*.cs`, bỏ `Controllers/Api`. Kết quả là 92, khớp mục 1.
- **Đếm view:** `find Views Areas -name '*.cshtml' | wc -l` cho ra 67.
- **CSS:** script `css_usage.py` (scratchpad, không commit) tách class rồi tìm nguyên từ trong `.cshtml`, `.cs` và `wwwroot/js`. Phần "chết thật" đã loại các class do Tag Helper, Bootstrap hoặc `site.js` sinh lúc chạy.
- **Mã hex:** `grep -c '#[0-9a-fA-F]\{3,6\}\b' wwwroot/css/*.css`.

## Cập nhật sau 05a — 19/10/2026

Bảng ở mục 4 là kiểm kê nền trước triển khai. Phía Owner đã có các route sau, cùng policy `ApprovedOwner`; riêng Dashboard vẫn giữ role Owner để chủ chưa duyệt xem hồ sơ.

| Màn | Route GET | POST | Trạng thái |
|---|---|---|---|
| Homestay của tôi | `/Owner/Homestays`, `/Owner/Homestays/Edit/{id}?tab=…`, alias `/Owner/Homestays/{id}` | Content, Location, Price, RoomType, AddRoom, Room, Active, Upload, Image, Policy, RemovePolicy, Document, DeleteDocument | Có; tiện nghi/tạo loại phòng/đổi danh mục còn G-16 |
| Giấy tờ riêng tư | `/Owner/Homestays/OpenDocument/{id}` | — | Có, quyền qua OwnerRegistrationQueryService |
| Lịch phòng | `/Owner/Calendar?homestayId=…&roomId=…&month=yyyy-MM` | Create, Unlock | Có, khóa/mở 3 thao tác |
| Lịch nhận–trả | `/Owner/Schedule?period=today|tomorrow|week`, hỗ trợ `date` cho link nhắc cũ | — | Có |
| Đối soát | `/Owner/Settlements`, `/Owner/Settlements/Details/{id}`, alias `/Owner/Settlements/{id}` | Confirm, Discrepancy | Có; chủ không thấy nháp |
| Đánh giá | `/Owner/Reviews?homestayId=…&page=…` | Reply | Có; tên khách vẫn dùng fallback G-15 |
| Dashboard | `/Owner/Dashboard` | — | Có lối tắt và lượt nhận hôm nay; thiếu số đơn Deposited qua service (G-17) |
| Thông báo | `/Notifications` + chuông dùng chung | Giữ các action đọc của bước 02 | Đã rà điện thoại; URL có sẵn mở đúng, dữ liệu cũ thiếu URL ghi G-18 |

Chi tiết nghiệm thu và dữ liệu thử: `docs/ui/05a-chu-homestay-report.md`.

## Cập nhật sau 05b — 19/10/2026

Các màn mới trong Staff dùng policy Employee; service chỉ cho Consultant/Manager/Administrator, controller chặn Accountant ở các màn nghiệp vụ này; Staff/Dashboard chuyển kế toán về Accounting/Dashboard. Không sửa policy hiện có.

| Màn | Route GET | POST | Service | Trạng thái |
|---|---|---|---|---|
| Quản lý đơn | `/Staff/Bookings?Q&Status&HomestayId&CheckInFrom&CheckInTo&AssignedEmployeeId&Unassigned&Page` | — | StaffBookingService.SearchAsync, AssigneesAsync | Có, G-20 về nhãn catalog |
| Chi tiết đơn | `/Staff/Bookings/{id}`, `/Staff/Bookings/Details/{id}` | Assign (`employeeId`), Status (`to`, `newRoomId`, `note`) | DetailsAsync, AssignAsync, ChangeStatusAsync | Có; Tư vấn nhận đơn chưa giao/giao lại đơn mình; xác nhận đổi trạng thái |
| Đặt hộ | `/Staff/Bookings/Create?Phone&FullName&Email&Channel&CheckIn&CheckOut&Guests&AreaId&RoomTypeId&SpecialRequests` | Create cùng trường | CustomerService.SearchAsync, AvailabilityService.StaffAvailabilityAsync/SearchAsync, PricingService.QuoteAsync, StaffBookingService.CreateForCustomerAsync | Có; GET tìm và xem giá, POST PRG; không sửa nghiệp vụ |
| Phòng trống | `/Staff/Availability?checkIn&checkOut&areaId&guests` | — | AvailabilityService.StaffAvailabilityAsync, SearchAsync (Areas) | Có theo đêm, NoStore, Làm mới; G-21 |
| Khách hàng | `/Staff/Customers?q&page`, `/Staff/Customers/Details/{id}` | Update (`FullName`, `Email`, `Note`) | CustomerService.SearchAsync/DetailsAsync/UpdateAsync | Có; lịch sử đơn, ghi chú, không đổi định danh người có tài khoản |
| Trùng phòng | `/Staff/Overbooking?id` | Report (`note`), Move (`roomId`, `note`), Replace (`roomTypeId`, `note`), Refund (`note`) | OverbookingService.ReportAsync/AlternativesAsync/MoveRoomAsync/ReplaceAsync/RefundAsync; AvailabilityService.FreeRoomIdsAsync | Có; Refund thêm Management, tất cả có xác nhận |
| Thẩm định đối tác | `/Staff/OwnerRegistrations?Status&OverdueOnly&Term&Page`, `/{id}`, `/Details/{id}`, `/Document/{id}` | Take, Revision (`note`), Approve, Reject (`note`), Invite, ResendInvite | OwnerRegistrationQueryService.ListAsync/DetailsAsync/OpenDocumentAsync; OwnerRegistrationService.TakeAsync/RequestRevisionAsync/ApproveAsync/RejectAsync/InviteAsync/ResendInviteAsync | Có; Approve/Reject thêm Management; lời mời một lần ở `/Staff/OwnerRegistrations/Invitation/{id}` |
| Dashboard | `/Staff/Dashboard?from&to` | — | StaffBookingService.SearchAsync, BookingChangeRequestService.PendingCountAsync, OwnerRegistrationQueryService.ListAsync, ReportService.EmployeesAsync | Có; không hiện duyệt nội dung; chuông NotificationService dùng lại bước 02 |
| URL duyệt nội dung cũ | `/Staff/Homestays`, `/{id}`, `/Details/{id}` | — | Không gọi | NV bị chặn; QL/AD 404 tới bước 05d; không dựng màn FR-27 |

Các trang dùng PageHead/FilterBar/DataTable/Pager, DescriptionList/PriceBreakdown/Timeline/BookingStepper/Steps/DateRangePicker và form-field; mẫu mới `_OverbookingAlternative` + `_RegistrationDocuments` có trong UI Kit. Tất cả POST mới có ValidateAntiForgeryToken và PRG/TempData. Hook mới của đặt hộ: `data-staff-quote-form`, `data-staff-confirmation`, `data-staff-quote-changed`.

Kích hoạt lời mời (phần nối tiếp cần thiết của FR-26): GET/POST `/Partner/Activate/{id}` (POST có thể `/Partner/Activate` với hidden `Id`, `Token`, `PhoneNumber`, `Password`, `ConfirmPassword`). Gọi OwnerRegistrationService.ActivateAsync, NoStore/no-referrer; nhân viên bị chặn, chủ tự đặt mật khẩu, POST PRG và không lưu lại mật khẩu. Không gửi tin ngoài app.

## Cập nhật sau 05c — 19/10/2026

Các route kế toán kế thừa area Accounting và policy Accounting. Chỉ Accountant gửi/mở lại/ghi nhận đã chi; Accountant/Manager/Administrator đọc và lập nháp theo quyền service.

| Màn | GET | POST (antiforgery + PRG/TempData) | Service | Trạng thái |
|---|---|---|---|---|
| Đối soát chủ homestay | `/Accounting/Settlements?year&month&periodNo&homestayId&status&page` | GenerateRecent; Generate (`year`, `month`, `periodNo`, `homestayId?`) | SettlementService.ListAsync/GeneratePeriodAsync/GenerateAsync | Có; kỳ gần nhất lùi đúng grace, không gọi hàm job |
| Chi tiết đối soát | `/Accounting/Settlements/{id}`, `/Details/{id}` | Send, RecordPaid (`bankRef`), Reopen (`reason`) | DetailsAsync/SendAsync/RecordPaidAsync/ReopenAsync | Có; snapshot, khoản bù chi riêng, Timeline audit; xác nhận chủ vẫn tại Owner (G-23) |
| Khoản bù trùng phòng | `/Accounting/Compensations` | RecordPaid (`bankRef`); Reject (`reason`) chỉ nối endpoint, không hiện form trên Completed | CompensationService.PendingAsync/RecordPaidAsync/RejectAsync | Chi được; thiếu read danh sách trước Completed cho form từ chối (G-24) |
| Dashboard Accounting | `/Accounting/Dashboard` | — | AccountingService cũ; SettlementService.ListAsync tất cả trang Draft/Sent/Confirmed | Có số kỳ và tổng tiền chờ gửi/chờ chi |
| Rà bước 04 | Transactions, Receipts, Refunds, AdditionalDeposits giữ route/model/form cũ | Giữ | Giữ | Thiếu liên kết đối soát thật trong dữ liệu đã nạp, chưa thêm shortcut (G-25) |

Các màn mới dùng PageHead, Tabs, FilterBar, DataTable/Pager, PriceBreakdown, Breadcrumb, Timeline, Alert, EmptyState, money/status-badge. Không thêm component/CSS/JS/thư viện/ảnh. Báo cáo nghiệm thu: `docs/ui/05c-ke-toan-report.md`.
