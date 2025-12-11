using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Constants
{
    public static class AuthConstants
    {
        internal const int VerificationCodeDigits = 6;

        internal const int DefaultTokenMinutes = 10080;

        internal const int InvalidUserId = 0;
        internal const int MinValidUserId = 1;

        internal const int ResendWindowSeconds = 45;

        internal static readonly TimeSpan ResendWindow =
            TimeSpan.FromSeconds(ResendWindowSeconds);

        internal const string AuthCodeOk = "Auth.Ok";
        internal const string AuthCodeBanned = "Auth.Banned";
        internal const string AuthCodeInvalidRequest = "Auth.InvalidRequest";
        internal const string AuthCodeEmailAlreadyExists = "Auth.EmailAlreadyExists";
        internal const string AuthCodeServerError = "Auth.ServerError";
        internal const string AuthCodeInvalidCredentials = "Auth.InvalidCredentials";
        internal const string AuthCodeEmailRequired = "Auth.EmailRequired";
        internal const string AuthCodeThrottleWait = "Auth.ThrottleWait";
        internal const string AuthCodeEmailSendFailed = "Auth.EmailSendFailed";
        internal const string AuthCodeCodeNotRequested = "Auth.CodeNotRequested";
        internal const string AuthCodeCodeExpired = "Auth.CodeExpired";
        internal const string AuthCodeCodeInvalid = "Auth.CodeInvalid";
        internal const string AuthCodePasswordWeak = "Auth.PasswordWeak";
        internal const string AuthCodePasswordReused = "Auth.PasswordReused";
        internal const string AuthCodeEmailNotFound = "Auth.EmailNotFound";
        internal const string AuthCodeSessionAlreadyActive = "Auth.SessionAlreadyActive";

        internal const string MetaKeySanctionType = "sanctionType";
        internal const string MetaKeyBanEndsAtUtc = "banEndsAtUtc";
        internal const string MetaKeySeconds = "seconds";
        internal const string MetaKeyReason = "reason";
        internal const string MetaKeyErrorType = "errorType";

        internal const string ErrorTypeSql = "SqlError";
        internal const string ErrorTypeConfig = "ConfigError";
        internal const string ErrorTypeCrypto = "CryptoError";
        internal const string ErrorTypeEmailSend = "EmailSendError";
        internal const string ErrorTypeUnexpected = "UnexpectedError";

        internal const string AppKeyTokenMinutes = "Auth:TokenMinutes";
    }
}
