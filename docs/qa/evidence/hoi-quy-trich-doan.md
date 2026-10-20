# Trích đoạn hồi quy trước và sau sửa

Các đoạn dưới chỉ giữ dòng kết luận; mỗi đoạn không quá 14 dòng. Bỏ dòng chứa chỉ báo bí mật, đường dẫn máy cá nhân và hostname triển khai. Không chép toàn bộ log.

## prelaunch/packaging-red.log

```text
  Failed HomestayBooking.Tests.Data.SeedAssetPackagingTests.DeploymentOutputContainsReadableSampleImage(fileName: "sample-receipt.png") [6 ms]
  Error Message:
  Failed HomestayBooking.Tests.Data.SeedAssetPackagingTests.DeploymentOutputContainsReadableSampleImage(fileName: "sample-document.png") [< 1 ms]
  Error Message:
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2, Duration: 120 ms - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/packaging-green.log

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 35 ms - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/webhook-red.log

```text
  Failed HomestayBooking.Tests.Services.BankWebhookValidationTests.InvalidTransactionDateReturnsProblemBeforeProcessingMoney(transactionDate: "") [25 ms]
  Error Message:
  Failed HomestayBooking.Tests.Services.BankWebhookValidationTests.InvalidTransactionDateReturnsProblemBeforeProcessingMoney(transactionDate: "2026-02-30 10:00:00") [< 1 ms]
  Error Message:
  Failed HomestayBooking.Tests.Services.BankWebhookValidationTests.InvalidTransactionDateReturnsProblemBeforeProcessingMoney(transactionDate: "not-a-date") [< 1 ms]
  Error Message:
Failed!  - Failed:     3, Passed:     1, Skipped:     0, Total:     4, Duration: 69 ms - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/webhook-green.log

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 52 ms - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/submission-red.log

```text
  Failed HomestayBooking.Tests.Services.BookingSubmissionTests.ReplayAcrossContextsReturnsOriginalBookingWithoutHoldingAnotherRoom [3 s]
  Error Message:
Expected: 1
Actual:   2
  Failed HomestayBooking.Tests.Services.BookingSubmissionTests.ReplayAfterCancellationDoesNotCreateAnotherBooking [241 ms]
  Error Message:
Expected: 1
Actual:   2
Failed!  - Failed:     2, Passed:     1, Skipped:     0, Total:     3, Duration: 4 s - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/submission-unit-green.log

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 3 s - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/error-handler-red.log

```text
  Failed HomestayBooking.Tests.Integration.ProductionErrorHandlingTests.FailedWebhookPostRendersFriendly500WithoutRequiringBrowserAntiforgery [7 s]
  Error Message:
Expected: InternalServerError
Actual:   BadRequest
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 7 s - HomestayBooking.Tests.dll (net10.0)
```

## prelaunch/error-handler-green.log

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 7 s - HomestayBooking.Tests.dll (net10.0)
```

## selfhost/evidence/tempdata-red.log

```text
  Failed HomestayBooking.Tests.Integration.SelfHostTests.AbsoluteInvitationAndQrUseConfigurationInsteadOfRequestHost [7 s]
  Error Message:
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 7 s - HomestayBooking.Tests.dll (net10.0)
```

## selfhost-free/evidence/regression-red.log

```text
Actual:   typeof(Microsoft.AspNetCore.Mvc.UnauthorizedResult)
  Failed HomestayBooking.Tests.Integration.SelfHostTests.FunnelWithoutForwardedProtoTrustsOnlyEnabledLoopback(enabled: True, remote: "::ffff:127.0.0.1", scheme: "https") [6 s]
  Error Message:
Expected: "https"
Actual:   "http"
  Failed HomestayBooking.Tests.Integration.SelfHostTests.FunnelWithoutForwardedProtoTrustsOnlyEnabledLoopback(enabled: True, remote: "127.0.0.1", scheme: "https") [2 s]
  Error Message:
Expected: "https"
Actual:   "http"
  Failed HomestayBooking.Tests.Integration.SelfHostTests.FunnelWithoutForwardedProtoTrustsOnlyEnabledLoopback(enabled: True, remote: "::1", scheme: "https") [1 s]
  Error Message:
Expected: "https"
Actual:   "http"
Failed!  - Failed:     6, Passed:     2, Skipped:     0, Total:     8, Duration: 14 s - HomestayBooking.Tests.dll (net10.0)
```
