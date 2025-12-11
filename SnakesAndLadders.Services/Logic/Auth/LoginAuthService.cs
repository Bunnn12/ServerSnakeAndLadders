using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;


namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class LoginAuthService : ILoginAuthService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginAuthService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPlayerReportAppService _playerReportAppService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public LoginAuthService(
            IAccountsRepository accountsRepository,
            IPasswordHasher passwordHasher,
            IPlayerReportAppService playerReportAppService,
            IUserRepository userRepository,
            ITokenService tokenService)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _playerReportAppService = playerReportAppService ?? throw new ArgumentNullException(nameof(playerReportAppService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public AuthResult Login(LoginDto request)
        {
            AuthResult validationError = ValidateLoginRequest(request);
            if (validationError != null)
            {
                return validationError;
            }

            AuthResult errorResult;

            if (!TryGetAuthCredentials(
                    request.Email,
                    out AuthCredentialsDto authCredentials,
                    out errorResult))
            {
                return errorResult;
            }

            int userId = authCredentials.UserId;
            string passwordHash = authCredentials.PasswordHash;
            string displayName = authCredentials.DisplayName;
            string profilePhotoId = authCredentials.ProfilePhotoId;

            if (!_passwordHasher.Verify(request.Password, passwordHash))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidCredentials);
            }

            if (!TryCheckBan(userId, out errorResult))
            {
                return errorResult;
            }

            if (!TryLoadAccount(userId, out AccountDto account, out errorResult))
            {
                return errorResult;
            }

            int ttlMinutes = GetTokenTtlMinutes();
            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(ttlMinutes);

            IssueTokenRequest tokenRequest = new IssueTokenRequest
            {
                UserId = userId,
                ExpiresAtUtc = expiresAtUtc
            };

            IssueTokenResult tokenResult = TryIssueToken(tokenRequest);

            if (!tokenResult.IsSuccess)
            {
                return tokenResult.Error;
            }

            string token = tokenResult.Token;

            string sessionErrorCode;
            bool sessionRegistered = InMemorySessionManager.TryRegisterNewSession(
                userId,
                token,
                out sessionErrorCode);

            if (!sessionRegistered)
            {
                Logger.WarnFormat(
                    "Login rejected for user {0} due to active session. SessionErrorCode={1}",
                    userId,
                    sessionErrorCode);

                return AuthResultFactory.Fail(AuthConstants.AuthCodeSessionAlreadyActive);
            }

            AuthResult result = AuthResultFactory.OkWithUserProfile(
                userId,
                displayName,
                profilePhotoId);

            result.CurrentSkinId = account.CurrentSkinId;
            result.CurrentSkinUnlockedId = account.CurrentSkinUnlockedId;
            result.Token = token;
            result.ExpiresAtUtc = expiresAtUtc;

            return result;
        }

        private AuthResult ValidateLoginRequest(LoginDto request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.Email))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            return null;
        }

        private bool TryGetAuthCredentials(
            string email,
            out AuthCredentialsDto authCredentials,
            out AuthResult errorResult)
        {
            authCredentials = null;
            errorResult = null;

            OperationResult<AuthCredentialsDto> authResult;

            try
            {
                authResult = _accountsRepository.GetAuthByIdentifier(email);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading credentials for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while loading credentials for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading credentials for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return false;
            }

            if (!authResult.IsSuccess || authResult.Data == null)
            {
                errorResult = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidCredentials);
                return false;
            }

            authCredentials = authResult.Data;
            return true;
        }

        private bool TryCheckBan(int userId, out AuthResult errorResult)
        {
            errorResult = null;

            try
            {
                var banInfo = _playerReportAppService.GetCurrentBan(userId);

                if (banInfo != null && banInfo.IsBanned)
                {
                    var meta = new Dictionary<string, string>();

                    if (!string.IsNullOrWhiteSpace(banInfo.SanctionType))
                    {
                        meta[AuthConstants.MetaKeySanctionType] = banInfo.SanctionType;
                    }

                    if (banInfo.BanEndsAtUtc.HasValue)
                    {
                        meta[AuthConstants.MetaKeyBanEndsAtUtc] = banInfo.BanEndsAtUtc.Value.ToString("o");
                    }

                    errorResult = AuthResultFactory.Fail(AuthConstants.AuthCodeBanned, meta);
                    return false;
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while checking ban state for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while checking ban state for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while checking ban state for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return false;
            }

            return true;
        }

        private bool TryLoadAccount(
            int userId,
            out AccountDto account,
            out AuthResult errorResult)
        {
            account = null;
            errorResult = null;

            try
            {
                account = _userRepository.GetByUserId(userId);

                if (account == null)
                {
                    Logger.WarnFormat(
                        "Login credentials valid but account/profile not found. UserId={0}. Treating as invalid credentials.",
                        userId);

                    errorResult = AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidCredentials);
                    return false;
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading account data for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while loading account data for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading account data for login.", ex);
                errorResult = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return false;
            }

            return true;
        }

        private int GetTokenTtlMinutes()
        {
            string ttlText;
            int ttlMinutes;

            try
            {
                ttlText = ConfigurationManager.AppSettings[AuthConstants.AppKeyTokenMinutes];

                if (!int.TryParse(ttlText, out ttlMinutes) || ttlMinutes <= AuthConstants.InvalidUserId)
                {
                    ttlMinutes = AuthConstants.DefaultTokenMinutes;
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while reading token TTL.", ex);
                ttlMinutes = AuthConstants.DefaultTokenMinutes;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while reading token TTL.", ex);
                ttlMinutes = AuthConstants.DefaultTokenMinutes;
            }

            return ttlMinutes;
        }

        private IssueTokenResult TryIssueToken(IssueTokenRequest request)
        {
            var result = new IssueTokenResult
            {
                IsSuccess = false,
                Token = string.Empty,
                Error = null
            };

            try
            {
                string token = _tokenService.IssueToken(request.UserId, request.ExpiresAtUtc);

                result.IsSuccess = true;
                result.Token = token;
                return result;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while issuing auth token.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
                return result;
            }
            catch (CryptographicException ex)
            {
                Logger.Error("Cryptographic error while issuing auth token.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeCrypto);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while issuing auth token.", ex);
                result.Error = AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
                return result;
            }
        }
    }
}
