using System;
using System.Collections.Generic;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class PlayerReportAppServiceTests : IDisposable
    {
        private const int MIN_VALID_USER_ID = 1;

        private const int THRESHOLD_S1 = 5;
        private const int THRESHOLD_S2 = 5;
        private const int THRESHOLD_S3 = 10;
        private const int THRESHOLD_S4 = 15;

        private const string SANCTION_TYPE_S1 = "S1";
        private const string SANCTION_TYPE_S2 = "S2";
        private const string SANCTION_TYPE_S3 = "S3";
        private const string SANCTION_TYPE_S4 = "S4";

        private const string ERROR_REPORT_INVALID_REQUEST = "REPORT_INVALID_REQUEST";
        private const string ERROR_REPORT_INVALID_USER = "REPORT_INVALID_USER";
        private const string ERROR_REPORT_DUPLICATE = "REPORT_DUPLICATE";
        private const string ERROR_REPORT_INTERNAL = "REPORT_INTERNAL_ERROR";

        private const string QUICK_KICK_DEFAULT_REASON = "Quick kick by host.";
        private const int MAX_REPORT_REASON_LENGTH = 100;

        private readonly Mock<IReportRepository> _reportRepositoryMock;
        private readonly Mock<ISanctionRepository> _sanctionRepositoryMock;
        private readonly Mock<IAccountStatusRepository> _accountStatusRepositoryMock;
        private readonly Mock<IPlayerSessionManager> _playerSessionManagerMock;

        private readonly PlayerReportAppService _service;

        public PlayerReportAppServiceTests()
        {
            _reportRepositoryMock =
                new Mock<IReportRepository>(MockBehavior.Strict);

            _sanctionRepositoryMock =
                new Mock<ISanctionRepository>(MockBehavior.Strict);

            _accountStatusRepositoryMock =
                new Mock<IAccountStatusRepository>(MockBehavior.Strict);

            _playerSessionManagerMock =
                new Mock<IPlayerSessionManager>(MockBehavior.Strict);

            _service = new PlayerReportAppService(
                _reportRepositoryMock.Object,
                _sanctionRepositoryMock.Object,
                _accountStatusRepositoryMock.Object,
                _playerSessionManagerMock.Object);
        }

        public void Dispose()
        {
            _reportRepositoryMock.VerifyAll();
            _sanctionRepositoryMock.VerifyAll();
            _accountStatusRepositoryMock.VerifyAll();
            _playerSessionManagerMock.VerifyAll();
        }

        #region Helpers

        private static ReportDto BuildValidReport(
            int reporterId = 10,
            int reportedId = 20,
            string reason = "Toxic behavior")
        {
            return new ReportDto
            {
                ReporterUserId = reporterId,
                ReportedUserId = reportedId,
                ReportReason = reason
            };
        }

        private static QuickKickDto BuildValidQuickKick(
            int hostId = 10,
            int targetId = 20,
            string reason = "Too toxic")
        {
            return new QuickKickDto
            {
                HostUserId = hostId,
                TargetUserId = targetId,
                KickReason = reason
            };
        }

        private static string GetFaultCode(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            try
            {
                dynamic faultEx = exception;
                ServiceFault detail = faultEx.Detail as ServiceFault;
                return detail?.Code;
            }
            catch
            {
                return null;
            }
        }

        private static void AssertFaultCode(Exception exception, string expectedCode)
        {
            string code = GetFaultCode(exception);

            Assert.NotNull(code);
            Assert.Equal(expectedCode, code);
        }

        #endregion

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenReportRepositoryIsNull()
        {
            var sanctionRepo = new Mock<ISanctionRepository>().Object;
            var accountRepo = new Mock<IAccountStatusRepository>().Object;
            var sessionManager = new Mock<IPlayerSessionManager>().Object;

            var ex = Assert.Throws<ArgumentNullException>(
                () => new PlayerReportAppService(
                    null,
                    sanctionRepo,
                    accountRepo,
                    sessionManager));

            Assert.Equal("reportRepositoryValue", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenSanctionRepositoryIsNull()
        {
            var reportRepo = new Mock<IReportRepository>().Object;
            var accountRepo = new Mock<IAccountStatusRepository>().Object;
            var sessionManager = new Mock<IPlayerSessionManager>().Object;

            var ex = Assert.Throws<ArgumentNullException>(
                () => new PlayerReportAppService(
                    reportRepo,
                    null,
                    accountRepo,
                    sessionManager));

            Assert.Equal("sanctionRepositoryValue", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenAccountStatusRepositoryIsNull()
        {
            var reportRepo = new Mock<IReportRepository>().Object;
            var sanctionRepo = new Mock<ISanctionRepository>().Object;
            var sessionManager = new Mock<IPlayerSessionManager>().Object;

            var ex = Assert.Throws<ArgumentNullException>(
                () => new PlayerReportAppService(
                    reportRepo,
                    sanctionRepo,
                    null,
                    sessionManager));

            Assert.Equal("accountStatusRepositoryValue", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenPlayerSessionManagerIsNull()
        {
            var reportRepo = new Mock<IReportRepository>().Object;
            var sanctionRepo = new Mock<ISanctionRepository>().Object;
            var accountRepo = new Mock<IAccountStatusRepository>().Object;

            var ex = Assert.Throws<ArgumentNullException>(
                () => new PlayerReportAppService(
                    reportRepo,
                    sanctionRepo,
                    accountRepo,
                    null));

            Assert.Equal("playerSessionManagerValue", ex.ParamName);
        }

        #endregion

        #region CreateReport – validation

        [Fact]
        public void TestCreateReportThrowsFaultWhenReportIsNull()
        {
            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(null));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_REQUEST);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(10, 0)]
        [InlineData(10, -1)]
        public void TestCreateReportThrowsFaultWhenUserIdsAreInvalid(
            int reporterId,
            int reportedId)
        {
            var report = BuildValidReport(
                reporterId,
                reportedId);

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(report));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_USER);
        }

        [Fact]
        public void TestCreateReportThrowsFaultWhenReporterEqualsReported()
        {
            const int userId = 10;

            var report = BuildValidReport(
                userId,
                userId);

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(report));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_REQUEST);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestCreateReportThrowsFaultWhenReasonIsNullOrWhitespace(
            string reason)
        {
            var report = BuildValidReport(
                10,
                20,
                reason);

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(report));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_REQUEST);
        }

        [Fact]
        public void TestCreateReportTrimsAndTruncatesReasonBeforeInsert()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            string longReason = new string('x', MAX_REPORT_REASON_LENGTH + 10);
            string paddedReason = "  " + longReason + "  ";

            var report = BuildValidReport(
                reporterId,
                reportedId,
                paddedReason);

            SanctionDto lastSanction = null;
            ReportDto capturedReport = null;

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns(lastSanction);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(false);

            _reportRepositoryMock
                .Setup(repo => repo.InsertReport(It.IsAny<ReportDto>()))
                .Callback<ReportDto>(dto => capturedReport = dto);

            _reportRepositoryMock
                .Setup(repo => repo.CountActiveReportsAgainstUser(
                    reportedId,
                    null))
                .Returns(0);

            _service.CreateReport(report);

            Assert.NotNull(capturedReport);
            Assert.False(string.IsNullOrWhiteSpace(capturedReport.ReportReason));
            Assert.True(capturedReport.ReportReason.Length <= MAX_REPORT_REASON_LENGTH);
            Assert.Equal(MAX_REPORT_REASON_LENGTH, capturedReport.ReportReason.Length);
        }

        [Fact]
        public void TestCreateReportThrowsDuplicateFaultWhenReporterHasActiveReport()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(
                reporterId,
                reportedId);

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns((SanctionDto)null);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(true);

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(report));

            AssertFaultCode(ex, ERROR_REPORT_DUPLICATE);
        }

        [Fact]
        public void TestCreateReportWrapsUnexpectedExceptionsIntoInternalFault()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(reporterId, reportedId);

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Throws(new InvalidOperationException("boom"));

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.CreateReport(report));

            AssertFaultCode(ex, ERROR_REPORT_INTERNAL);
        }

        #endregion

        #region CreateReport – EvaluateSanctions (S1..S4)

        [Fact]
        public void TestCreateReportAppliesSanctionS1WhenThresholdReachedAndNoPriorSanction()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(reporterId, reportedId);

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns((SanctionDto)null);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(false);

            _reportRepositoryMock
                .Setup(repo => repo.InsertReport(It.IsAny<ReportDto>()));

            _reportRepositoryMock
                .Setup(repo => repo.CountActiveReportsAgainstUser(
                    reportedId,
                    null))
                .Returns(THRESHOLD_S1);

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(
                    It.Is<SanctionDto>(s =>
                        s.UserId == reportedId &&
                        s.SanctionType == SANCTION_TYPE_S1 &&
                        !string.IsNullOrWhiteSpace(s.ReportReason))));

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    reportedId,
                    It.IsAny<string>()));

            _service.CreateReport(report);
        }

        [Fact]
        public void TestCreateReportAppliesSanctionS2WhenThresholdReachedAfterS1()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(reporterId, reportedId);

            var lastSanction = new SanctionDto
            {
                SanctionId = 1,
                UserId = reportedId,
                SanctionType = SANCTION_TYPE_S1,
                SanctionDateUtc = DateTime.UtcNow.AddDays(-3)
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns(lastSanction);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(false);

            _reportRepositoryMock
                .Setup(repo => repo.InsertReport(It.IsAny<ReportDto>()));

            _reportRepositoryMock
                .Setup(repo => repo.CountActiveReportsAgainstUser(
                    reportedId,
                    It.IsAny<DateTime?>()))
                .Returns(THRESHOLD_S2);

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(
                    It.Is<SanctionDto>(s =>
                        s.UserId == reportedId &&
                        s.SanctionType == SANCTION_TYPE_S2)));

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    reportedId,
                    It.IsAny<string>()));

            _service.CreateReport(report);
        }

        [Fact]
        public void TestCreateReportAppliesSanctionS3WhenThresholdReachedAfterS2()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(reporterId, reportedId);

            var lastSanction = new SanctionDto
            {
                SanctionId = 1,
                UserId = reportedId,
                SanctionType = SANCTION_TYPE_S2,
                SanctionDateUtc = DateTime.UtcNow.AddDays(-10)
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns(lastSanction);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(false);

            _reportRepositoryMock
                .Setup(repo => repo.InsertReport(It.IsAny<ReportDto>()));

            _reportRepositoryMock
                .Setup(repo => repo.CountActiveReportsAgainstUser(
                    reportedId,
                    It.IsAny<DateTime?>()))
                .Returns(THRESHOLD_S3);

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(
                    It.Is<SanctionDto>(s =>
                        s.UserId == reportedId &&
                        s.SanctionType == SANCTION_TYPE_S3)));

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    reportedId,
                    It.IsAny<string>()));

            _service.CreateReport(report);
        }

        [Fact]
        public void TestCreateReportAppliesSanctionS4AndDeactivatesAccountWhenThresholdReachedAfterS3()
        {
            const int reporterId = 10;
            const int reportedId = 20;

            var report = BuildValidReport(reporterId, reportedId);

            var lastSanction = new SanctionDto
            {
                SanctionId = 1,
                UserId = reportedId,
                SanctionType = SANCTION_TYPE_S3,
                SanctionDateUtc = DateTime.UtcNow.AddDays(-40)
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(reportedId))
                .Returns(lastSanction);

            _reportRepositoryMock
                .Setup(repo => repo.ReporterHasActiveReport(
                    It.IsAny<ActiveReportSearchCriteriaDto>()))
                .Returns(false);

            _reportRepositoryMock
                .Setup(repo => repo.InsertReport(It.IsAny<ReportDto>()));

            _reportRepositoryMock
                .Setup(repo => repo.CountActiveReportsAgainstUser(
                    reportedId,
                    It.IsAny<DateTime?>()))
                .Returns(THRESHOLD_S4);

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(
                    It.Is<SanctionDto>(s =>
                        s.UserId == reportedId &&
                        s.SanctionType == SANCTION_TYPE_S4)));

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    reportedId,
                    It.IsAny<string>()));

            _accountStatusRepositoryMock
                .Setup(repo => repo.DeactivateUserAndAccount(reportedId));

            _service.CreateReport(report);
        }

        #endregion

        #region GetCurrentBan

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestGetCurrentBanThrowsFaultWhenUserIdIsInvalid(int userId)
        {
            var ex = Assert.ThrowsAny<Exception>(
                () => _service.GetCurrentBan(userId));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_USER);
        }

        [Fact]
        public void TestGetCurrentBanReturnsNotBannedWhenNoSanctions()
        {
            const int userId = 10;

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(userId))
                .Returns((SanctionDto)null);

            BanInfoDto result = _service.GetCurrentBan(userId);

            Assert.False(result.IsBanned);
            Assert.Null(result.BanEndsAtUtc);
            Assert.Null(result.SanctionType);
        }

        [Fact]
        public void TestGetCurrentBanReturnsBannedForActiveS1()
        {
            const int userId = 10;

            var sanction = new SanctionDto
            {
                UserId = userId,
                SanctionType = SANCTION_TYPE_S1,
                SanctionDateUtc = DateTime.UtcNow
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(userId))
                .Returns(sanction);

            BanInfoDto result = _service.GetCurrentBan(userId);

            Assert.True(result.IsBanned);
            Assert.Equal(SANCTION_TYPE_S1, result.SanctionType);
            Assert.True(result.BanEndsAtUtc.HasValue);
        }

        [Fact]
        public void TestGetCurrentBanReturnsPermanentlyBannedForS4()
        {
            const int userId = 10;

            var sanction = new SanctionDto
            {
                UserId = userId,
                SanctionType = SANCTION_TYPE_S4,
                SanctionDateUtc = DateTime.UtcNow.AddYears(-1)
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(userId))
                .Returns(sanction);

            BanInfoDto result = _service.GetCurrentBan(userId);

            Assert.True(result.IsBanned);
            Assert.Equal(SANCTION_TYPE_S4, result.SanctionType);
            Assert.Null(result.BanEndsAtUtc);
        }

        [Fact]
        public void TestGetCurrentBanWrapsUnexpectedExceptionIntoInternalFault()
        {
            const int userId = 10;

            _sanctionRepositoryMock
                .Setup(repo => repo.GetLastSanctionForUser(userId))
                .Throws(new InvalidOperationException("boom"));

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.GetCurrentBan(userId));

            AssertFaultCode(ex, ERROR_REPORT_INTERNAL);
        }

        #endregion

        #region GetSanctionsHistory

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestGetSanctionsHistoryThrowsFaultWhenUserIdIsInvalid(int userId)
        {
            var ex = Assert.ThrowsAny<Exception>(
                () => _service.GetSanctionsHistory(userId));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_USER);
        }

        [Fact]
        public void TestGetSanctionsHistoryReturnsEmptyListWhenRepositoryReturnsNull()
        {
            const int userId = 10;

            _sanctionRepositoryMock
                .Setup(repo => repo.GetSanctionsHistory(userId))
                .Returns((IList<SanctionDto>)null);

            IList<SanctionDto> result = _service.GetSanctionsHistory(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void TestGetSanctionsHistoryReturnsRepositoryList()
        {
            const int userId = 10;

            var list = new List<SanctionDto>
            {
                new SanctionDto { UserId = userId },
                new SanctionDto { UserId = userId }
            };

            _sanctionRepositoryMock
                .Setup(repo => repo.GetSanctionsHistory(userId))
                .Returns(list);

            IList<SanctionDto> result = _service.GetSanctionsHistory(userId);

            Assert.Same(list, result);
        }

        [Fact]
        public void TestGetSanctionsHistoryWrapsUnexpectedExceptionIntoInternalFault()
        {
            const int userId = 10;

            _sanctionRepositoryMock
                .Setup(repo => repo.GetSanctionsHistory(userId))
                .Throws(new InvalidOperationException("boom"));

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.GetSanctionsHistory(userId));

            AssertFaultCode(ex, ERROR_REPORT_INTERNAL);
        }

        #endregion

        #region QuickKickPlayer

        [Fact]
        public void TestQuickKickPlayerThrowsFaultWhenRequestIsNull()
        {
            var ex = Assert.ThrowsAny<Exception>(
                () => _service.QuickKickPlayer(null));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_REQUEST);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(10, 0)]
        [InlineData(10, -1)]
        public void TestQuickKickPlayerThrowsFaultWhenIdsAreInvalid(
            int targetId,
            int hostId)
        {
            var quickKick = BuildValidQuickKick(
                hostId,
                targetId);

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.QuickKickPlayer(quickKick));

            AssertFaultCode(ex, ERROR_REPORT_INVALID_USER);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestQuickKickPlayerUsesDefaultReasonWhenReasonIsNullOrWhitespace(
            string reason)
        {
            const int targetId = 20;
            const int hostId = 10;

            var quickKick = BuildValidQuickKick(
                hostId,
                targetId,
                reason);

            SanctionDto capturedSanction = null;

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(It.IsAny<SanctionDto>()))
                .Callback<SanctionDto>(dto => capturedSanction = dto);

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    targetId,
                    QUICK_KICK_DEFAULT_REASON));

            _service.QuickKickPlayer(quickKick);

            Assert.NotNull(capturedSanction);
            Assert.Equal(targetId, capturedSanction.UserId);
            Assert.Equal(SANCTION_TYPE_S1, capturedSanction.SanctionType);
            Assert.Equal(QUICK_KICK_DEFAULT_REASON, capturedSanction.ReportReason);
            Assert.False(capturedSanction.AppliedBySystem);
        }

        [Fact]
        public void TestQuickKickPlayerTruncatesLongReason()
        {
            const int targetId = 20;
            const int hostId = 10;

            string longReason = new string('y', MAX_REPORT_REASON_LENGTH + 15);

            var quickKick = BuildValidQuickKick(
                hostId,
                targetId,
                longReason);

            SanctionDto capturedSanction = null;

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(It.IsAny<SanctionDto>()))
                .Callback<SanctionDto>(dto => capturedSanction = dto);

            _playerSessionManagerMock
                .Setup(mgr => mgr.KickUserFromAllSessions(
                    targetId,
                    It.IsAny<string>()));

            _service.QuickKickPlayer(quickKick);

            Assert.NotNull(capturedSanction);
            Assert.True(capturedSanction.ReportReason.Length <= MAX_REPORT_REASON_LENGTH);
            Assert.Equal(MAX_REPORT_REASON_LENGTH, capturedSanction.ReportReason.Length);
        }

        [Fact]
        public void TestQuickKickPlayerWrapsUnexpectedExceptionIntoInternalFault()
        {
            const int targetId = 20;
            const int hostId = 10;

            var quickKick = BuildValidQuickKick(
                hostId,
                targetId);

            _sanctionRepositoryMock
                .Setup(repo => repo.InsertSanction(It.IsAny<SanctionDto>()))
                .Throws(new InvalidOperationException("boom"));

            var ex = Assert.ThrowsAny<Exception>(
                () => _service.QuickKickPlayer(quickKick));

            AssertFaultCode(ex, ERROR_REPORT_INTERNAL);
        }

        #endregion
    }
}
