using System;

namespace SnakesAndLadders.Data.Constants
{
    internal static class ReportRepositoryConstants
    {
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_USER_ID = 1;

        public const string ERROR_DTO_REQUIRED = "ReportDto is required.";
        public const string ERROR_CRITERIA_REQUIRED = "ActiveReportSearchCriteriaDto is required.";
        public const string ERROR_REPORTED_USER_ID_POSITIVE = "ReportedUserId must be positive.";
        public const string ERROR_REPORTER_USER_ID_POSITIVE = "ReporterUserId must be positive.";

        public const string LOG_ERROR_INSERT_REPORT = "Error inserting report.";
        public const string LOG_ERROR_CHECK_ACTIVE_REPORT = "Error checking active report.";
        public const string LOG_ERROR_COUNT_ACTIVE_REPORTS = "Error counting active reports.";
    }
}
