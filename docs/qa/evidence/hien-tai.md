# Kết quả chạy lại 20/10/2026

Nhánh docs/qa-evidence; cùng mã ứng dụng 1c5859f (chỉ bổ sung tài liệu). Không chạy test SQL Server thật; ReportServiceSqlServerTests bị lọc riêng. Benchmark mặc định bỏ qua. Lượt sandbox đầu bị timeout host và dừng, không dùng làm kết quả đạt; bảng dưới là lượt xác nhận ngoài sandbox.

Tổng 1780; pass 1779; fail 0; skip 1; thêm 1 test SQL không chọn. Thời gian runner: 4 m 30 s.

SHA-256 TRX nguồn: `c77eb14c9555f3752f74ac553e786b71c9852f17c01f1fc216a5e08f56111e6e`.

Mốc runner giữ múi giờ TRX: {'creation': '2026-10-20T17:23:44.4574070+07:00', 'queuing': '2026-10-20T17:23:44.4574080+07:00', 'start': '2026-10-20T17:23:42.0022780+07:00', 'finish': '2026-10-20T17:28:15.1948960+07:00'}.

| Lớp test | Tổng | Pass | Fail | Skip | Tổng thời gian từng ca (giây) |
|---|---:|---:|---:|---:|---:|
| HomestayBooking.Tests.Data.DatabaseConstraintTests | 313 | 313 | 0 | 0 | 49.837 |
| HomestayBooking.Tests.Data.SeedAssetPackagingTests | 2 | 2 | 0 | 0 | 0.032 |
| HomestayBooking.Tests.Data.SeedDataMigrationTests | 1 | 1 | 0 | 0 | 1.721 |
| HomestayBooking.Tests.Data.SeedDataSqliteTests | 1 | 1 | 0 | 0 | 11.074 |
| HomestayBooking.Tests.Data.SeedDataTests | 14 | 14 | 0 | 0 | 58.604 |
| HomestayBooking.Tests.Extensions.PaginatedListTests | 7 | 7 | 0 | 0 | 0.074 |
| HomestayBooking.Tests.Identity.AccountLookupServiceTests | 13 | 13 | 0 | 0 | 3.963 |
| HomestayBooking.Tests.Identity.AppSignInManagerTests | 17 | 17 | 0 | 0 | 2.562 |
| HomestayBooking.Tests.Identity.CustomerAccountServiceTests | 6 | 6 | 0 | 0 | 1.006 |
| HomestayBooking.Tests.Identity.PhoneNumberNormalizerTests | 15 | 15 | 0 | 0 | 0.001 |
| HomestayBooking.Tests.Integration.AccountingWorkspaceUiTests | 14 | 14 | 0 | 0 | 106.134 |
| HomestayBooking.Tests.Integration.AdminWorkspaceUiTests | 21 | 21 | 0 | 0 | 158.865 |
| HomestayBooking.Tests.Integration.AnonymousBookingPageTests | 3 | 3 | 0 | 0 | 6.145 |
| HomestayBooking.Tests.Integration.B9WorkspaceUiTests | 3 | 3 | 0 | 0 | 21.655 |
| HomestayBooking.Tests.Integration.ChangeRequestPoliciesTests | 5 | 5 | 0 | 0 | 5.842 |
| HomestayBooking.Tests.Integration.ChangeRequestUiTests | 40 | 40 | 0 | 0 | 104.908 |
| HomestayBooking.Tests.Integration.GuestReviewPageTests | 1 | 1 | 0 | 0 | 15.767 |
| HomestayBooking.Tests.Integration.MyBookingsTabsTests | 1 | 1 | 0 | 0 | 7.476 |
| HomestayBooking.Tests.Integration.OwnerApiTests | 4 | 4 | 0 | 0 | 30.537 |
| HomestayBooking.Tests.Integration.OwnerBookingsAuthorizationTests | 11 | 11 | 0 | 0 | 14.749 |
| HomestayBooking.Tests.Integration.OwnerCalendarUiTests | 5 | 5 | 0 | 0 | 25.936 |
| HomestayBooking.Tests.Integration.OwnerDashboardUiTests | 3 | 3 | 0 | 0 | 16.485 |
| HomestayBooking.Tests.Integration.OwnerHomestaysUiTests | 12 | 12 | 0 | 0 | 35.352 |
| HomestayBooking.Tests.Integration.OwnerReviewsUiTests | 4 | 4 | 0 | 0 | 16.692 |
| HomestayBooking.Tests.Integration.OwnerScheduleUiTests | 6 | 6 | 0 | 0 | 9.856 |
| HomestayBooking.Tests.Integration.OwnerSettlementsUiTests | 4 | 4 | 0 | 0 | 15.536 |
| HomestayBooking.Tests.Integration.PartnerRegistrationPageTests | 3 | 3 | 0 | 0 | 26.389 |
| HomestayBooking.Tests.Integration.ProductionErrorHandlingTests | 1 | 1 | 0 | 0 | 7.575 |
| HomestayBooking.Tests.Integration.PublicApiTests | 5 | 5 | 0 | 0 | 14.456 |
| HomestayBooking.Tests.Integration.SecurityHeadersTests | 3 | 3 | 0 | 0 | 8.235 |
| HomestayBooking.Tests.Integration.SelfHostTests | 18 | 18 | 0 | 0 | 125.853 |
| HomestayBooking.Tests.Integration.StaffWorkspaceUiTests | 16 | 16 | 0 | 0 | 127.322 |
| HomestayBooking.Tests.Services.AccountAdminServiceTests | 44 | 44 | 0 | 0 | 28.201 |
| HomestayBooking.Tests.Services.AccountingServiceTests | 11 | 11 | 0 | 0 | 3.770 |
| HomestayBooking.Tests.Services.AdminBootstrapServiceTests | 4 | 4 | 0 | 0 | 2.149 |
| HomestayBooking.Tests.Services.AnonymousBookingTests | 4 | 4 | 0 | 0 | 2.768 |
| HomestayBooking.Tests.Services.AvailabilityPerformanceTests | 1 | 0 | 0 | 1 | 0.001 |
| HomestayBooking.Tests.Services.AvailabilitySeedRegressionTests | 1 | 1 | 0 | 0 | 6.813 |
| HomestayBooking.Tests.Services.AvailabilityServiceTests | 38 | 38 | 0 | 0 | 17.387 |
| HomestayBooking.Tests.Services.B9AdminTests | 2 | 2 | 0 | 0 | 1.080 |
| HomestayBooking.Tests.Services.B9FinanceTests | 15 | 15 | 0 | 0 | 7.755 |
| HomestayBooking.Tests.Services.B9OwnerTests | 7 | 7 | 0 | 0 | 3.060 |
| HomestayBooking.Tests.Services.B9StaffTests | 2 | 2 | 0 | 0 | 2.954 |
| HomestayBooking.Tests.Services.BankMatchingServiceTests | 19 | 19 | 0 | 0 | 7.247 |
| HomestayBooking.Tests.Services.BankWebhookValidationTests | 7 | 7 | 0 | 0 | 0.007 |
| HomestayBooking.Tests.Services.BookingChangeOptionsTests | 3 | 3 | 0 | 0 | 0.002 |
| HomestayBooking.Tests.Services.BookingChangeRequestServiceTests | 115 | 115 | 0 | 0 | 59.479 |
| HomestayBooking.Tests.Services.BookingContactVisibilityTests | 12 | 12 | 0 | 0 | 1.827 |
| HomestayBooking.Tests.Services.BookingOptionsTests | 1 | 1 | 0 | 0 | 0.002 |
| HomestayBooking.Tests.Services.BookingServiceTests | 22 | 22 | 0 | 0 | 10.816 |
| HomestayBooking.Tests.Services.BookingSubmissionTests | 3 | 3 | 0 | 0 | 1.411 |
| HomestayBooking.Tests.Services.BookingTimeoutJobTests | 5 | 5 | 0 | 0 | 3.302 |
| HomestayBooking.Tests.Services.CatalogDeletionServiceTests | 11 | 11 | 0 | 0 | 5.023 |
| HomestayBooking.Tests.Services.CompensationServiceTests | 33 | 33 | 0 | 0 | 12.236 |
| HomestayBooking.Tests.Services.ContractQueryTests | 2 | 2 | 0 | 0 | 0.938 |
| HomestayBooking.Tests.Services.ContractServiceTests | 39 | 39 | 0 | 0 | 21.467 |
| HomestayBooking.Tests.Services.CustomerServiceTests | 17 | 17 | 0 | 0 | 6.532 |
| HomestayBooking.Tests.Services.GuestCustomerServiceTests | 9 | 9 | 0 | 0 | 3.655 |
| HomestayBooking.Tests.Services.HomestayContentTests | 18 | 18 | 0 | 0 | 8.385 |
| HomestayBooking.Tests.Services.HomestayEditorTests | 3 | 3 | 0 | 0 | 1.099 |
| HomestayBooking.Tests.Services.HomestayMediaPolicyTests | 16 | 16 | 0 | 0 | 6.257 |
| HomestayBooking.Tests.Services.HomestayReviewFixTests | 28 | 28 | 0 | 0 | 19.133 |
| HomestayBooking.Tests.Services.HomestayReviewTests | 49 | 49 | 0 | 0 | 7.796 |
| HomestayBooking.Tests.Services.HomestayTransactionTests | 7 | 7 | 0 | 0 | 3.353 |
| HomestayBooking.Tests.Services.MessageServiceTests | 10 | 10 | 0 | 0 | 3.548 |
| HomestayBooking.Tests.Services.NotificationServiceTests | 2 | 2 | 0 | 0 | 0.738 |
| HomestayBooking.Tests.Services.OverbookingServiceTests | 35 | 35 | 0 | 0 | 15.163 |
| HomestayBooking.Tests.Services.OwnerBookingTests | 20 | 20 | 0 | 0 | 8.314 |
| HomestayBooking.Tests.Services.OwnerNotificationTests | 11 | 11 | 0 | 0 | 6.463 |
| HomestayBooking.Tests.Services.OwnerRegistrationQueryServiceTests | 5 | 5 | 0 | 0 | 3.410 |
| HomestayBooking.Tests.Services.OwnerRegistrationServiceTests | 87 | 87 | 0 | 0 | 22.564 |
| HomestayBooking.Tests.Services.OwnerRegistrationSlaTests | 9 | 9 | 0 | 0 | 6.352 |
| HomestayBooking.Tests.Services.OwnerScheduleServiceTests | 11 | 11 | 0 | 0 | 4.906 |
| HomestayBooking.Tests.Services.PartnerReviewTests | 24 | 24 | 0 | 0 | 10.559 |
| HomestayBooking.Tests.Services.PartnerWorkflowRegressionTests | 14 | 14 | 0 | 0 | 6.487 |
| HomestayBooking.Tests.Services.PendingHomestayDetailsTests | 1 | 1 | 0 | 0 | 1.996 |
| HomestayBooking.Tests.Services.PricingServiceTests | 13 | 13 | 0 | 0 | 6.224 |
| HomestayBooking.Tests.Services.ReceiptFileStoreTests | 6 | 6 | 0 | 0 | 0.001 |
| HomestayBooking.Tests.Services.ReceiptReviewServiceTests | 12 | 12 | 0 | 0 | 5.194 |
| HomestayBooking.Tests.Services.RefundAndCodeTests | 9 | 9 | 0 | 0 | 2.572 |
| HomestayBooking.Tests.Services.ReminderDeliveryTests | 27 | 27 | 0 | 0 | 7.416 |
| HomestayBooking.Tests.Services.ReminderServiceTests | 38 | 38 | 0 | 0 | 12.458 |
| HomestayBooking.Tests.Services.ReportServiceTests | 13 | 13 | 0 | 0 | 6.264 |
| HomestayBooking.Tests.Services.ReviewServiceTests | 31 | 31 | 0 | 0 | 10.010 |
| HomestayBooking.Tests.Services.RoomBlockServiceTests | 17 | 17 | 0 | 0 | 7.236 |
| HomestayBooking.Tests.Services.RoomManagementTests | 21 | 21 | 0 | 0 | 8.188 |
| HomestayBooking.Tests.Services.RoomSearchSelectionTests | 8 | 8 | 0 | 0 | 0.155 |
| HomestayBooking.Tests.Services.SettlementCalculatorTests | 19 | 19 | 0 | 0 | 0.032 |
| HomestayBooking.Tests.Services.SettlementServiceTests | 79 | 79 | 0 | 0 | 26.436 |
| HomestayBooking.Tests.Services.StaffAssigneeAndOwnerScheduleTests | 2 | 2 | 0 | 0 | 0.879 |
| HomestayBooking.Tests.Services.StaffBookingControllerTests | 1 | 1 | 0 | 0 | 0.450 |
| HomestayBooking.Tests.Services.StaffBookingServiceTests | 24 | 24 | 0 | 0 | 10.406 |
| HomestayBooking.Tests.Services.UploadValidationTests | 16 | 16 | 0 | 0 | 0.833 |
| HomestayBooking.Tests.Services.VoucherServiceTests | 17 | 17 | 0 | 0 | 6.130 |
| HomestayBooking.Tests.TagHelpers.OwnerStatusBadgeTests | 7 | 7 | 0 | 0 | 0.003 |
| HomestayBooking.Tests.TagHelpers.TagHelperTests | 48 | 48 | 0 | 0 | 0.117 |
| HomestayBooking.Tests.Ui.DataComponentTests | 7 | 7 | 0 | 0 | 0.093 |
| HomestayBooking.Tests.Ui.NotificationsControllerTests | 13 | 13 | 0 | 0 | 12.339 |
| HomestayBooking.Tests.Ui.RoomCalendarModelTests | 3 | 3 | 0 | 0 | 0.054 |

Mỗi dòng Theory có dữ liệu được tính là một test. Tổng thời gian từng ca có thể chồng nhau do chạy song song, không bằng thời gian toàn lượt.

## Test skip

AvailabilityPerformanceTests.StaffAvailabilityAndDatedSearchStayWithinTargets: cần RUN_PERF_TESTS=1 để chạy SQL Server, lượt này không bật.

## Ca đại diện trong tài liệu

| Lớp.phương thức | Số đầu vào | Pass | Fail | Skip |
|---|---:|---:|---:|---:|
| AdminWorkspaceUiTests.AccountsManagerReadsButCannotWrite_AdministratorCreatesLocksResetsAndChangesEmployment | 1 | 1 | 0 | 0 |
| AdminWorkspaceUiTests.CatalogInUseCannotBeDeletedAndShowsClearError | 1 | 1 | 0 | 0 |
| AdminWorkspaceUiTests.ContractsCreateExtendAndTerminationUseServiceAndNotificationAlias | 1 | 1 | 0 | 0 |
| AdminWorkspaceUiTests.EveryMutationRequiresAntiforgery | 10 | 10 | 0 | 0 |
| AdminWorkspaceUiTests.FiveReportTabsPreserveRangeAndDisplayServiceConversionAndFinancialTotals | 2 | 2 | 0 | 0 |
| AdminWorkspaceUiTests.ManagementScreensAllowManagersAndAdministratorsOnly | 5 | 5 | 0 | 0 |
| AdminWorkspaceUiTests.PendingContentCanBeRejectedApprovedAndResumed_ConsultantCannotAct | 1 | 1 | 0 | 0 |
| B9FinanceTests.AccountantConfirmsOnBehalfWithReasonAuditAndOwnerLinkThenPaysActualAmountAndTime | 1 | 1 | 0 | 0 |
| B9FinanceTests.OnBehalfRejectsInvalidConfirmation | 5 | 5 | 0 | 0 |
| B9FinanceTests.PaymentRejectsBadAmountDateStateAndActors | 7 | 7 | 0 | 0 |
| B9FinanceTests.ReadRowsUseRealSettlementItemsAndLoadNamesAndBlockOwner | 1 | 1 | 0 | 0 |
| B9FinanceTests.RejectableCompensationsUseExactlyRejectConditionsAndHideCompleted | 1 | 1 | 0 | 0 |
| B9OwnerTests.BookingNotificationsLinkToRecipientsWorkspace | 5 | 5 | 0 | 0 |
| B9OwnerTests.EditorCatalogIsCompleteForInactiveHomeAndMarksSelection | 1 | 1 | 0 | 0 |
| B9OwnerTests.SummaryCountsOnlyOwnDepositedAndTodaysArrivalsAndBlocksNonOwner | 1 | 1 | 0 | 0 |
| BankWebhookValidationTests.InvalidDateStillRequiresWebhookAuthentication | 1 | 1 | 0 | 0 |
| BankWebhookValidationTests.InvalidTransactionDateReturnsProblemBeforeProcessingMoney | 3 | 3 | 0 | 0 |
| BankWebhookValidationTests.UnconfiguredWebhookIs503BeforeProcessingMoney | 3 | 3 | 0 | 0 |
| BookingSubmissionTests.ReplayAcrossContextsReturnsOriginalBookingWithoutHoldingAnotherRoom | 1 | 1 | 0 | 0 |
| BookingSubmissionTests.ReplayAfterCancellationDoesNotCreateAnotherBooking | 1 | 1 | 0 | 0 |
| BookingSubmissionTests.SeparateFormsCanStillBookTwoRoomsForSameStay | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.AlternativesPrioritizeSameHomeThenAreaThenOtherWithoutLeakingContact | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.FailureAfterBusinessWritesRollsBackCompleteIncident | 3 | 3 | 0 | 0 |
| OverbookingServiceTests.FujiRefundRestoresFullVoucherIssuesApologyAndQueuesCashRefund | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.IncidentNotificationsLinkCustomerByCodeAndOwnerToOwnerArea | 3 | 3 | 0 | 0 |
| OverbookingServiceTests.InvalidBookingActorOrOpenRequestBlocksIncident | 8 | 8 | 0 | 0 |
| OverbookingServiceTests.NewOwnerRejectionQueuesOnlyReplacementCashRefundAndRestoresExactlyTransferredDiscount | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.OnlyManagementMayChooseFullRefund | 3 | 3 | 0 | 0 |
| OverbookingServiceTests.OnlyStaffMayMoveReplaceOrListAlternatives | 3 | 3 | 0 | 0 |
| OverbookingServiceTests.ReplacedBookingNeverEntersSettlementEvenWithRetainedDepositOrChangeFees | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.ReplacementBelowDepositRollsBackEveryTableIncludingCodeCounter | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.ReplacementRoomNightCollisionRollsBackOldAndNewBookingPaymentAndVoucher | 1 | 1 | 0 | 0 |
| OverbookingServiceTests.ReplacementTransfersSnapshotsVoucherAndCreatesOnlyPositiveCompensation | 4 | 4 | 0 | 0 |
| OverbookingServiceTests.ReportNotifiesAssignedAndManagersAndAuditsActor | 4 | 4 | 0 | 0 |
| OverbookingServiceTests.SameTypeRoomMovePreservesMoneyCodeAndWritesIncidentHistory | 1 | 1 | 0 | 0 |
| OwnerApiTests.NonOwnersGet401Or403WithoutRedirect | 1 | 1 | 0 | 0 |
| OwnerApiTests.OwnerBlocksAndUnblocksOwnRoomWithConflictAndOwnershipChecks | 1 | 1 | 0 | 0 |
| OwnerApiTests.OwnerConfirmsOwnDepositedBookingButNotOthersAndNotWithoutToken | 1 | 1 | 0 | 0 |
| OwnerApiTests.RejectNeedsReasonAndRefundsDeposit | 1 | 1 | 0 | 0 |
| PricingServiceTests.DayTypeOf_FollowsBR08 | 5 | 5 | 0 | 0 |
| PricingServiceTests.Quote_CheckOutNotAfterCheckIn_Throws | 1 | 1 | 0 | 0 |
| PricingServiceTests.Quote_HolidayNight_UsesHolidayPrice | 1 | 1 | 0 | 0 |
| PricingServiceTests.Quote_NoPriceForNight_Throws | 1 | 1 | 0 | 0 |
| PricingServiceTests.Quote_ThursdayToSunday_PricesEachNight | 1 | 1 | 0 | 0 |
| PricingServiceTests.Quote_UsesPriceEffectiveOnEachNight | 1 | 1 | 0 | 0 |
| PricingServiceTests.RoundToThousand_RoundsHalfUp | 3 | 3 | 0 | 0 |
| ProductionErrorHandlingTests.FailedWebhookPostRendersFriendly500WithoutRequiringBrowserAntiforgery | 1 | 1 | 0 | 0 |
| PublicApiTests.AvailabilityListsBlockedNightsAndRejectsBadRanges | 1 | 1 | 0 | 0 |
| PublicApiTests.BookingLookupNeedsMatchingPhoneAndHidesExistence | 1 | 1 | 0 | 0 |
| PublicApiTests.QuoteReturnsNightlyPricesAndDepositOrProblem | 1 | 1 | 0 | 0 |
| PublicApiTests.SearchReturnsPagedHomestaysAndRejectsReversedPriceRange | 1 | 1 | 0 | 0 |
| PublicApiTests.UnreadCountRequiresSignInAndMatchesAccount | 1 | 1 | 0 | 0 |
| RefundAndCodeTests.BookingCode_IncrementsPerDayAndRestartsNextDay | 1 | 1 | 0 | 0 |
| RefundAndCodeTests.QuoteCancellation_DepositedBooking_FollowsBR04 | 5 | 5 | 0 | 0 |
| RefundAndCodeTests.QuoteCancellation_NoDepositYet_RefundsNothing | 1 | 1 | 0 | 0 |
| RefundAndCodeTests.SelectPolicy_HolidayNight_PrefersHomestayHolidayPolicy | 1 | 1 | 0 | 0 |
| RefundAndCodeTests.SelectPolicy_NoHolidayNight_UsesGeneralPolicy | 1 | 1 | 0 | 0 |
| SecurityHeadersTests.ResponsesCarryBaselineSecurityHeaders | 3 | 3 | 0 | 0 |
| SeedAssetPackagingTests.DeploymentOutputContainsReadableSampleImage | 2 | 2 | 0 | 0 |
| SelfHostTests.AbsoluteInvitationAndQrUseConfigurationInsteadOfRequestHost | 1 | 1 | 0 | 0 |
| SelfHostTests.BookingQrNeverFallsBackToDemoAccountInProduction | 2 | 2 | 0 | 0 |
| SelfHostTests.DisabledSePayReturns503WithoutRecordingTransactions | 1 | 1 | 0 | 0 |
| SelfHostTests.FunnelWithoutForwardedProtoTrustsOnlyEnabledLoopback | 5 | 5 | 0 | 0 |
| SelfHostTests.HealthFailureIs503WithoutDatabaseDetails | 1 | 1 | 0 | 0 |
| SelfHostTests.KeysCookieAndOldAntiforgerySurviveNewHost | 1 | 1 | 0 | 0 |
| SelfHostTests.OnlyEnabledLoopbackProxyCanForward | 4 | 4 | 0 | 0 |
| SelfHostTests.ProductionHasNoDevelopmentEndpointsEvenForAnonymousRequests | 3 | 3 | 0 | 0 |
| UploadValidationTests.MismatchedNameTypeOrPathIsRejected | 8 | 8 | 0 | 0 |
| UploadValidationTests.OctetStreamOrMissingContentTypeFallsBackToContentCheck | 1 | 1 | 0 | 0 |
| UploadValidationTests.PdfOnlyAllowedWhereEnabled | 1 | 1 | 0 | 0 |
| UploadValidationTests.RealImagesWithMatchingNameAndTypeAreAccepted | 5 | 5 | 0 | 0 |
| UploadValidationTests.TruncatedOrCorruptedImagesAreRejected | 1 | 1 | 0 | 0 |
