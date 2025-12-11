using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class VerificationAuthService : IVerificationAuthService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VerificationAuthService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationCodeStore _verificationCodeStore;

        public VerificationAuthService(
            IAccountsRepository accountsRepository,
            IEmailSender emailSender,
            IVerificationCodeStore verificationCodeStore)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _verificationCodeStore = verificationCodeStore ?? throw new ArgumentNullException(nameof(verificationCodeStore));
        }

        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailRequired);
            }

            bool isRegistered;

            try
            {
                isRegistered = _accountsRepository.IsEmailRegistered(email);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while checking email for verification.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while checking email for verification.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while checking email for verification.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            if (isRegistered)
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailAlreadyExists);
            }

            VerificationCodeEntry existingEntry;
            if (_verificationCodeStore.TryGet(email, out existingEntry))
            {
                TimeSpan elapsed = DateTime.UtcNow - existingEntry.LastSentUtc;
                if (elapsed < AuthConstants.ResendWindow)
                {
                    int secondsToWait = (int)(AuthConstants.ResendWindow - elapsed).TotalSeconds;
                    var meta = new Dictionary<string, string>
                    {
                        [AuthConstants.MetaKeySeconds] = secondsToWait.ToString()
                    };

                    return AuthResultFactory.Fail(AuthConstants.AuthCodeThrottleWait, meta);
                }
            }

            string code;
            DateTime nowUtc = DateTime.UtcNow;

            try
            {
                code = VerificationCodeGenerator.GenerateCode(AuthConstants.VerificationCodeDigits);
            }
            catch (CryptographicException ex)
            {
                Logger.Error("Cryptographic error while generating email verification code.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while generating email verification code.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            _verificationCodeStore.SaveNewCode(email, code, nowUtc);

            try
            {
                _emailSender.SendVerificationCode(email, code);
                return AuthResultFactory.Ok();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while sending email verification code.", ex);
                _verificationCodeStore.Remove(email);

                var meta = new Dictionary<string, string>
                {
                    [AuthConstants.MetaKeyReason] = ex.GetType().Name,
                    [AuthConstants.MetaKeyErrorType] = AuthConstants.ErrorTypeEmailSend
                };

                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailSendFailed, meta);
            }
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            VerificationCodeEntry entry;
            if (!_verificationCodeStore.TryGet(email, out entry))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeCodeNotRequested);
            }

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _verificationCodeStore.Remove(email);
                return AuthResultFactory.Fail(AuthConstants.AuthCodeCodeExpired);
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
            {
                _verificationCodeStore.RegisterFailedAttempt(email, entry);
                return AuthResultFactory.Fail(AuthConstants.AuthCodeCodeInvalid);
            }

            _verificationCodeStore.Remove(email);
            return AuthResultFactory.Ok();
        }

        public AuthResult RequestPasswordChangeCode(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailRequired);
            }

            bool isRegistered;

            try
            {
                isRegistered = _accountsRepository.IsEmailRegistered(email);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while checking email for password change.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while checking email for password change.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while checking email for password change.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            if (!isRegistered)
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailNotFound);
            }

            VerificationCodeEntry existingEntry;
            if (_verificationCodeStore.TryGet(email, out existingEntry))
            {
                TimeSpan elapsed = DateTime.UtcNow - existingEntry.LastSentUtc;

                if (elapsed < AuthConstants.ResendWindow)
                {
                    int secondsToWait = (int)(AuthConstants.ResendWindow - elapsed).TotalSeconds;

                    var meta = new Dictionary<string, string>
                    {
                        [AuthConstants.MetaKeySeconds] = secondsToWait.ToString()
                    };

                    return AuthResultFactory.Fail(AuthConstants.AuthCodeThrottleWait, meta);
                }
            }

            string code;
            DateTime nowUtc = DateTime.UtcNow;

            try
            {
                code = VerificationCodeGenerator.GenerateCode(AuthConstants.VerificationCodeDigits);
            }
            catch (CryptographicException ex)
            {
                Logger.Error("Cryptographic error while generating password change verification code.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while generating password change verification code.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            _verificationCodeStore.SaveNewCode(email, code, nowUtc);

            try
            {
                _emailSender.SendVerificationCode(email, code);
                return AuthResultFactory.Ok();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while sending password change verification code.", ex);
                _verificationCodeStore.Remove(email);

                var meta = new Dictionary<string, string>
                {
                    [AuthConstants.MetaKeyReason] = ex.GetType().Name,
                    [AuthConstants.MetaKeyErrorType] = AuthConstants.ErrorTypeEmailSend
                };

                return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailSendFailed, meta);
            }
        }
    }
}
