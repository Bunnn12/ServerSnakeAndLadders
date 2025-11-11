using System;
using System.Collections.Generic;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class PlayerReportAppService : IPlayerReportAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlayerReportAppService));

        private const int MIN_VALID_USER_ID = 1;

        private const int THRESHOLD_S1 = 5;
        private const int THRESHOLD_S2 = 5;
        private const int THRESHOLD_S3 = 10;
        private const int THRESHOLD_S4 = 15;

        private static readonly TimeSpan DURATION_S1 = TimeSpan.FromDays(2);
        private static readonly TimeSpan DURATION_S2 = TimeSpan.FromDays(7);
        private static readonly TimeSpan DURATION_S3 = TimeSpan.FromDays(30);

        private const string SANCTION_TYPE_S1 = "S1";
        private const string SANCTION_TYPE_S2 = "S2";
        private const string SANCTION_TYPE_S3 = "S3";
        private const string SANCTION_TYPE_S4 = "S4";

        private const int SYSTEM_KICK_HOST_ID = 0;

        private const string ERROR_REPORT_INVALID_REQUEST = "REPORT_INVALID_REQUEST";
        private const string ERROR_REPORT_INVALID_USER = "REPORT_INVALID_USER";
        private const string ERROR_REPORT_DUPLICATE = "REPORT_DUPLICATE";
        private const string ERROR_REPORT_INTERNAL = "REPORT_INTERNAL_ERROR";

        private const string AUTO_SANCTION_REASON_FORMAT =
            "Automatic sanction {0} applied due to accumulated player reports.";

        private const string QUICK_KICK_DEFAULT_REASON = "Quick kick by host.";

        private readonly IReportRepository reportRepository;
        private readonly ISanctionRepository sanctionRepository;
        private readonly IAccountStatusRepository accountStatusRepository;
        private readonly IPlayerSessionManager playerSessionManager;

        public PlayerReportAppService(
            IReportRepository reportRepositoryValue,
            ISanctionRepository sanctionRepositoryValue,
            IAccountStatusRepository accountStatusRepositoryValue,
            IPlayerSessionManager playerSessionManagerValue)
        {
            reportRepository = reportRepositoryValue
                ?? throw new ArgumentNullException(nameof(reportRepositoryValue));

            sanctionRepository = sanctionRepositoryValue
                ?? throw new ArgumentNullException(nameof(sanctionRepositoryValue));

            accountStatusRepository = accountStatusRepositoryValue
                ?? throw new ArgumentNullException(nameof(accountStatusRepositoryValue));

            playerSessionManager = playerSessionManagerValue
                ?? throw new ArgumentNullException(nameof(playerSessionManagerValue));
        }

        public void CreateReport(ReportDto report)
        {
            if (report == null)
            {
                Logger.Warn("CreateReport called with null payload.");
                throw CreateFault(ERROR_REPORT_INVALID_REQUEST);
            }

            try
            {
                ValidateReport(report);

                var lastSanction = sanctionRepository.GetLastSanctionForUser(report.ReportedUserId);
                DateTime? lastSanctionDateUtc = lastSanction?.SanctionDateUtc;

                var reportCriteria = new ActiveReportSearchCriteriaDto
                {
                    ReporterUserId = report.ReporterUserId,
                    ReportedUserId = report.ReportedUserId,
                    LastSanctionDateUtc = lastSanctionDateUtc
                };

                bool hasActiveReport = reportRepository.ReporterHasActiveReport(reportCriteria);
                if (hasActiveReport)
                {
                    Logger.WarnFormat(
                        "Duplicate report detected. ReporterUserId={0}, ReportedUserId={1}",
                        report.ReporterUserId,
                        report.ReportedUserId);

                    throw CreateFault(ERROR_REPORT_DUPLICATE);
                }

                reportRepository.InsertReport(report);

                EvaluateSanctions(report.ReportedUserId, lastSanction);
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in CreateReport.", ex);
                throw CreateFault(ERROR_REPORT_INTERNAL);
            }
        }

        public BanInfoDto GetCurrentBan(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                Logger.WarnFormat("GetCurrentBan called with invalid userId={0}.", userId);
                throw CreateFault(ERROR_REPORT_INVALID_USER);
            }

            try
            {
                var lastSanction = sanctionRepository.GetLastSanctionForUser(userId);
                if (lastSanction == null)
                {
                    return new BanInfoDto { IsBanned = false };
                }

                return BuildBanInfo(lastSanction);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetCurrentBan.", ex);
                throw CreateFault(ERROR_REPORT_INTERNAL);
            }
        }

        public IList<SanctionDto> GetSanctionsHistory(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                Logger.WarnFormat("GetSanctionsHistory called with invalid userId={0}.", userId);
                throw CreateFault(ERROR_REPORT_INVALID_USER);
            }

            try
            {
                return sanctionRepository.GetSanctionsHistory(userId);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetSanctionsHistory.", ex);
                throw CreateFault(ERROR_REPORT_INTERNAL);
            }
        }

        public void QuickKickPlayer(QuickKickDto quickKick)
        {
            if (quickKick == null)
            {
                Logger.Warn("QuickKickPlayer called with null payload.");
                throw CreateFault(ERROR_REPORT_INVALID_REQUEST);
            }

            if (quickKick.TargetUserId < MIN_VALID_USER_ID ||
                quickKick.HostUserId < MIN_VALID_USER_ID)
            {
                Logger.WarnFormat(
                    "QuickKickPlayer called with invalid ids. Target={0}, Host={1}",
                    quickKick.TargetUserId,
                    quickKick.HostUserId);

                throw CreateFault(ERROR_REPORT_INVALID_USER);
            }

            string safeReason = string.IsNullOrWhiteSpace(quickKick.KickReason)
                ? QUICK_KICK_DEFAULT_REASON
                : quickKick.KickReason.Trim();

            try
            {
                KickUserAndRegisterSanction(
                    quickKick.TargetUserId,
                    quickKick.HostUserId,
                    safeReason,
                    SANCTION_TYPE_S1,
                    false);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in QuickKickPlayer.", ex);
                throw CreateFault(ERROR_REPORT_INTERNAL);
            }
        }

        private void ValidateReport(ReportDto report)
        {
            if (report.ReporterUserId < MIN_VALID_USER_ID ||
                report.ReportedUserId < MIN_VALID_USER_ID)
            {
                Logger.WarnFormat(
                    "ValidateReport invalid users. Reporter={0}, Reported={1}",
                    report.ReporterUserId,
                    report.ReportedUserId);

                throw CreateFault(ERROR_REPORT_INVALID_USER);
            }

            if (report.ReporterUserId == report.ReportedUserId)
            {
                Logger.WarnFormat(
                    "ValidateReport self-report attempt. UserId={0}",
                    report.ReporterUserId);

                throw CreateFault(ERROR_REPORT_INVALID_REQUEST);
            }

            if (string.IsNullOrWhiteSpace(report.ReportReason))
            {
                Logger.Warn("ValidateReport called with empty reportReason.");
                throw CreateFault(ERROR_REPORT_INVALID_REQUEST);
            }

            string trimmedReason = report.ReportReason.Trim();
            const int MAX_REASON_LENGTH = 100;

            if (trimmedReason.Length > MAX_REASON_LENGTH)
            {
                trimmedReason = trimmedReason.Substring(0, MAX_REASON_LENGTH);
            }

            report.ReportReason = trimmedReason;
        }

        private void EvaluateSanctions(int reportedUserId, SanctionDto lastSanction)
        {
            DateTime? lastSanctionDateUtc = lastSanction?.SanctionDateUtc;

            int activeReports = reportRepository.CountActiveReportsAgainstUser(
                reportedUserId,
                lastSanctionDateUtc);

            if (lastSanction == null)
            {
                if (activeReports >= THRESHOLD_S1)
                {
                    ApplySanction(reportedUserId, SANCTION_TYPE_S1);
                }

                return;
            }

            if (string.Equals(lastSanction.SanctionType, SANCTION_TYPE_S1, StringComparison.OrdinalIgnoreCase))
            {
                if (activeReports >= THRESHOLD_S2)
                {
                    ApplySanction(reportedUserId, SANCTION_TYPE_S2);
                }

                return;
            }

            if (string.Equals(lastSanction.SanctionType, SANCTION_TYPE_S2, StringComparison.OrdinalIgnoreCase))
            {
                if (activeReports >= THRESHOLD_S3)
                {
                    ApplySanction(reportedUserId, SANCTION_TYPE_S3);
                }

                return;
            }

            if (string.Equals(lastSanction.SanctionType, SANCTION_TYPE_S3, StringComparison.OrdinalIgnoreCase))
            {
                if (activeReports >= THRESHOLD_S4)
                {
                    ApplySanction(reportedUserId, SANCTION_TYPE_S4);
                }
            }
        }

        private void ApplySanction(int userId, string sanctionType)
        {
            bool deactivateAccount = string.Equals(
                sanctionType,
                SANCTION_TYPE_S4,
                StringComparison.OrdinalIgnoreCase);

            string autoReason = string.Format(
                AUTO_SANCTION_REASON_FORMAT,
                sanctionType);

            KickUserAndRegisterSanction(
                userId,
                SYSTEM_KICK_HOST_ID,
                autoReason,
                sanctionType,
                deactivateAccount);
        }

        private void KickUserAndRegisterSanction(
            int targetUserId,
            int hostUserId,
            string reason,
            string sanctionType,
            bool deactivateAccount)
        {
            var nowUtc = DateTime.UtcNow;

            var sanction = new SanctionDto
            {
                UserId = targetUserId,
                SanctionType = sanctionType,
                SanctionDateUtc = nowUtc,
                AppliedBySystem = hostUserId == SYSTEM_KICK_HOST_ID,
                ReportReason = reason
            };

            sanctionRepository.InsertSanction(sanction);

            var banInfo = BuildBanInfo(sanction);

            Logger.InfoFormat(
                "Applied sanction {0} to user {1}. Host={2}. IsBanned={3}, BanEndsAtUtc={4}",
                sanctionType,
                targetUserId,
                hostUserId,
                banInfo.IsBanned,
                banInfo.BanEndsAtUtc?.ToString("o") ?? "null");

            if (banInfo.IsBanned)
            {
                TryKickUserFromSessions(targetUserId, reason, sanctionType);
            }

            if (deactivateAccount)
            {
                TryDeactivateAccount(targetUserId);
            }
        }

        private void TryKickUserFromSessions(int userId, string reason, string sanctionType)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                return;
            }

            try
            {
                string formattedReason = string.IsNullOrWhiteSpace(reason)
                    ? string.Format(AUTO_SANCTION_REASON_FORMAT, sanctionType)
                    : reason.Trim();

                playerSessionManager.KickUserFromAllSessions(userId, formattedReason);

                Logger.InfoFormat(
                    "KickUserFromAllSessions invoked for user {0} after sanction {1}.",
                    userId,
                    sanctionType);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    string.Format(
                        "Error while kicking user {0} from sessions after sanction {1}.",
                        userId,
                        sanctionType),
                    ex);
            }
        }

        private void TryDeactivateAccount(int userId)
        {
            try
            {
                accountStatusRepository.SetUserAndAccountActiveState(userId, false);

                Logger.InfoFormat(
                    "User {0} permanently banned. User, account and passwords set to inactive.",
                    userId);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    string.Format(
                        "Error applying permanent deactivation for user {0}.",
                        userId),
                    ex);
            }
        }

        private BanInfoDto BuildBanInfo(SanctionDto sanction)
        {
            var info = new BanInfoDto
            {
                SanctionType = sanction.SanctionType
            };

            DateTime startUtc = sanction.SanctionDateUtc;
            DateTime nowUtc = DateTime.UtcNow;

            if (string.Equals(sanction.SanctionType, SANCTION_TYPE_S1, StringComparison.OrdinalIgnoreCase))
            {
                DateTime endUtc = startUtc.Add(DURATION_S1);
                info.BanEndsAtUtc = endUtc;
                info.IsBanned = nowUtc <= endUtc;
            }
            else if (string.Equals(sanction.SanctionType, SANCTION_TYPE_S2, StringComparison.OrdinalIgnoreCase))
            {
                DateTime endUtc = startUtc.Add(DURATION_S2);
                info.BanEndsAtUtc = endUtc;
                info.IsBanned = nowUtc <= endUtc;
            }
            else if (string.Equals(sanction.SanctionType, SANCTION_TYPE_S3, StringComparison.OrdinalIgnoreCase))
            {
                DateTime endUtc = startUtc.Add(DURATION_S3);
                info.BanEndsAtUtc = endUtc;
                info.IsBanned = nowUtc <= endUtc;
            }
            else if (string.Equals(sanction.SanctionType, SANCTION_TYPE_S4, StringComparison.OrdinalIgnoreCase))
            {
                info.IsBanned = true;
                info.BanEndsAtUtc = null;
            }
            else
            {
                info.IsBanned = false;
                info.BanEndsAtUtc = null;
            }

            return info;
        }

        private static FaultException<ServiceFault> CreateFault(string errorCode)
        {
            var fault = new ServiceFault
            {
                Code = errorCode,
                Message = errorCode
            };

            return new FaultException<ServiceFault>(fault, new FaultReason(errorCode));
        }
    }
}
