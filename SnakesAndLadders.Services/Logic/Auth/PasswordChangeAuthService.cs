using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class PasswordChangeAuthService : IPasswordChangeAuthService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PasswordChangeAuthService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IVerificationCodeStore _verificationCodeStore;

        public PasswordChangeAuthService(
            IAccountsRepository accountsRepository,
            IPasswordHasher passwordHasher,
            IVerificationCodeStore verificationCodeStore)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _verificationCodeStore = verificationCodeStore ?? throw new ArgumentNullException(nameof(verificationCodeStore));
        }

        public AuthResult ChangePassword(ChangePasswordRequestDto request)
        {
            ChangePasswordValidationContext validationContext = ValidateChangePasswordRequest(request);

            if (validationContext.Error != null)
            {
                return validationContext.Error;
            }

            EmailCodeValidationResult emailResult = ValidateEmailAndCode(
                validationContext.Email,
                validationContext.VerificationCode);

            if (!emailResult.IsValid)
            {
                return emailResult.Error;
            }

            PasswordHistoryResult historyResult = LoadPasswordHistory(validationContext.UserId);

            if (!historyResult.IsValid)
            {
                return historyResult.Error;
            }

            IReadOnlyList<string> passwordHistory = historyResult.History;

            if (PasswordPolicyHelper.IsPasswordReused(validationContext.NewPassword, passwordHistory, _passwordHasher))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodePasswordReused);
            }

            PersistPasswordResult persistResult = PersistNewPassword(
                validationContext.UserId,
                validationContext.NewPassword);

            if (!persistResult.IsSuccess)
            {
                return persistResult.Error;
            }

            return AuthResultFactory.OkWithCustomCode(
                AuthConstants.AuthCodeOk,
                validationContext.UserId);
        }

        private ChangePasswordValidationContext ValidateChangePasswordRequest(ChangePasswordRequestDto request)
        {
            var context = new ChangePasswordValidationContext
            {
                UserId = AuthConstants.InvalidUserId,
                Email = string.Empty,
                NewPassword = string.Empty,
                VerificationCode = string.Empty,
                Error = null
            };

            if (request == null)
            {
                context.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
                return context;
            }

            string rawEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string newPassword = request.NewPassword ?? string.Empty;
            string verificationCode = (request.VerificationCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(rawEmail)
                || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(verificationCode))
            {
                context.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
                return context;
            }

            if (!PasswordPolicyHelper.IsPasswordFormatValid(newPassword))
            {
                context.Error = AuthResultFactory.Fail(AuthConstants.AuthCodePasswordWeak);
                return context;
            }

            try
            {
                OperationResult<AuthCredentialsDto> authResult =
                    _accountsRepository.GetAuthByIdentifier(rawEmail);

                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    context.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidCredentials);
                    return context;
                }

                context.UserId = authResult.Data.UserId;
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading user for password change.", ex);
                context.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return context;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while loading user for password change.", ex);
                context.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return context;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading user for password change.", ex);
                context.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return context;
            }

            context.Email = rawEmail;
            context.NewPassword = newPassword;
            context.VerificationCode = verificationCode;

            return context;
        }

        private EmailCodeValidationResult ValidateEmailAndCode(string email, string verificationCode)
        {
            var result = new EmailCodeValidationResult
            {
                IsValid = false,
                Email = string.Empty,
                Error = null
            };

            if (string.IsNullOrWhiteSpace(email))
            {
                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
                return result;
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();

            VerificationCodeEntry entry;
            if (!_verificationCodeStore.TryGet(normalizedEmail, out entry))
            {
                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeCodeNotRequested);
                return result;
            }

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _verificationCodeStore.Remove(normalizedEmail);
                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeCodeExpired);
                return result;
            }

            if (!string.Equals(verificationCode, entry.Code, StringComparison.Ordinal))
            {
                _verificationCodeStore.RegisterFailedAttempt(normalizedEmail, entry);
                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeCodeInvalid);
                return result;
            }

            _verificationCodeStore.Remove(normalizedEmail);

            result.IsValid = true;
            result.Email = normalizedEmail;

            return result;
        }

        private PasswordHistoryResult LoadPasswordHistory(int userId)
        {
            var result = new PasswordHistoryResult
            {
                IsValid = false,
                History = Array.Empty<string>(),
                Error = null
            };

            OperationResult<IReadOnlyList<string>> historyResult;

            try
            {
                historyResult = _accountsRepository.GetLastPasswordHashes(
                    userId,
                    PasswordPolicyHelper.PasswordHistoryLimit);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading password history.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return result;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while loading password history.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading password history.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return result;
            }

            if (!historyResult.IsSuccess || historyResult.Data == null || historyResult.Data.Count == 0)
            {
                Logger.WarnFormat(
                    "Password change requested but no password history found. UserId={0}",
                    userId);

                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeServerError);
                return result;
            }

            result.IsValid = true;
            result.History = historyResult.Data;
            return result;
        }

        private PersistPasswordResult PersistNewPassword(int userId, string newPassword)
        {
            var result = new PersistPasswordResult
            {
                IsSuccess = false,
                Error = null
            };

            string newHash = _passwordHasher.Hash(newPassword);

            OperationResult<bool> addResult;

            try
            {
                addResult = _accountsRepository.AddPasswordHash(userId, newHash);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while inserting new password hash.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return result;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while inserting new password hash.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while inserting new password hash.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return result;
            }

            if (!addResult.IsSuccess || !addResult.Data)
            {
                Logger.ErrorFormat(
                    "Failed to insert new password hash. UserId={0}",
                    userId);

                result.Error = AuthResultFactory.Fail(AuthConstants.AuthCodeServerError);
                return result;
            }

            result.IsSuccess = true;
            return result;
        }
    }
}
