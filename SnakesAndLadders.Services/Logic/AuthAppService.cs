using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Auth;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class AuthAppService : IAuthAppService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AuthAppService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly IPlayerReportAppService _playerReportAppService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IVerificationCodeStore _verificationCodeStore;

        private const int VerificationCodeDigits = 6;
        private const int RandomBytesLength = 4;

        private const int DefaultTokenMinutes = 10080;

        private const int PasswordMinLength = 8;
        private const int PasswordMaxLength = 64;
        private const int PasswordHistoryLimit = 3;

        private const int InvalidUserId = 0;
        private const int MinValidUserId = 1;

        private const long MinValidUnixTimestamp = 1;

        private const int FirstByteIndex = 0;
        private const int DecimalBase = 10;
        private const char VerificationCodePadChar = '0';

        private const int ResendWindowSeconds = 45;

        private static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(ResendWindowSeconds);

        private const string AuthCodeOk = "Auth.Ok";
        private const string AuthCodeBanned = "Auth.Banned";
        private const string AuthCodeInvalidRequest = "Auth.InvalidRequest";
        private const string AuthCodeEmailAlreadyExists = "Auth.EmailAlreadyExists";
        private const string AuthCodeServerError = "Auth.ServerError";
        private const string AuthCodeInvalidCredentials = "Auth.InvalidCredentials";
        private const string AuthCodeEmailRequired = "Auth.EmailRequired";
        private const string AuthCodeThrottleWait = "Auth.ThrottleWait";
        private const string AuthCodeEmailSendFailed = "Auth.EmailSendFailed";
        private const string AuthCodeCodeNotRequested = "Auth.CodeNotRequested";
        private const string AuthCodeCodeExpired = "Auth.CodeExpired";
        private const string AuthCodeCodeInvalid = "Auth.CodeInvalid";
        private const string AuthCodePasswordWeak = "Auth.PasswordWeak";
        private const string AuthCodePasswordReused = "Auth.PasswordReused";
        private const string AuthCodeEmailNotFound = "Auth.EmailNotFound";
        private const string AuthCodeSessionAlreadyActive = "Auth.SessionAlreadyActive";

        private const string MetaKeySanctionType = "sanctionType";
        private const string MetaKeyBanEndsAtUtc = "banEndsAtUtc";
        private const string MetaKeySeconds = "seconds";
        private const string MetaKeyReason = "reason";
        private const string MetaKeyErrorType = "errorType";

        private const string ErrorTypeSql = "SqlError";
        private const string ErrorTypeConfig = "ConfigError";
        private const string ErrorTypeCrypto = "CryptoError";
        private const string ErrorTypeEmailSend = "EmailSendError";
        private const string ErrorTypeUnexpected = "UnexpectedError";

        private const string AppKeyTokenMinutes = "Auth:TokenMinutes";

        public AuthAppService(
            IAccountsRepository accountsRepository,
            IPasswordHasher passwordHasher,
            IEmailSender emailSender,
            IPlayerReportAppService playerReportAppService,
            IUserRepository userRepository,
            ITokenService tokenService,
            IVerificationCodeStore verificationCodeStore)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _playerReportAppService = playerReportAppService ?? throw new ArgumentNullException(nameof(playerReportAppService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _verificationCodeStore = verificationCodeStore ?? throw new ArgumentNullException(nameof(verificationCodeStore));
        }

        public AuthResult RegisterUser(RegistrationDto registration)
        {
            AuthResult validationError = ValidateRegistrationRequest(registration);
            if (validationError != null)
            {
                return validationError;
            }

            OperationResult<int> createResult = CreateUserAccount(registration);

            if (!createResult.IsSuccess)
            {
                return FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
            }

            int newUserId = createResult.Data;
            return OkWithUser(newUserId, registration.UserName);
        }

        private AuthResult ValidateRegistrationRequest(RegistrationDto registration)
        {
            if (registration == null
                || string.IsNullOrWhiteSpace(registration.Email)
                || string.IsNullOrWhiteSpace(registration.Password)
                || string.IsNullOrWhiteSpace(registration.UserName))
            {
                return Fail(AuthCodeInvalidRequest);
            }

            try
            {
                if (_accountsRepository.IsEmailRegistered(registration.Email))
                {
                    return Fail(AuthCodeEmailAlreadyExists);
                }

                if (_accountsRepository.IsUserNameTaken(registration.UserName))
                {
                    return Fail(AuthCodeEmailAlreadyExists.Replace("Email", "UserName"));
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while validating registration request.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while validating registration request.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while validating registration request.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            return null;
        }

        private OperationResult<int> CreateUserAccount(RegistrationDto registration)
        {
            string passwordHash = _passwordHasher.Hash(registration.Password);

            var requestDto = new CreateAccountRequestDto
            {
                Username = registration.UserName,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                Email = registration.Email,
                PasswordHash = passwordHash
            };

            try
            {
                return _accountsRepository.CreateUserWithAccountAndPassword(requestDto);
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while creating user account.", ex);
                return OperationResult<int>.Failure("SQL error while creating user account.");
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while creating user account.", ex);
                return OperationResult<int>.Failure("Configuration error while creating user account.");
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while creating user account.", ex);
                return OperationResult<int>.Failure("Unexpected error while creating user account.");
            }
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
                return Fail(AuthCodeInvalidCredentials);
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

            if (!TryIssueToken(userId, expiresAtUtc, out string token, out errorResult))
            {
                return errorResult;
            }

            string sessionErrorCode;
            bool sessionRegistered = InMemorySessionManager.TryRegisterNewSession(
                userId,
                token,
                out sessionErrorCode);

            if (!sessionRegistered)
            {
                _logger.WarnFormat(
                    "Login rejected for user {0} due to active session. SessionErrorCode={1}",
                    userId,
                    sessionErrorCode);

                return Fail(AuthCodeSessionAlreadyActive);
            }

            AuthResult result = OkWithUserProfile(
                userId,
                displayName,
                profilePhotoId);

            result.CurrentSkinId = account.CurrentSkinId;
            result.CurrentSkinUnlockedId = account.CurrentSkinUnlockedId;
            result.Token = token;
            result.ExpiresAtUtc = expiresAtUtc;

            return result;
        }

        public AuthResult Logout(LogoutRequestDto request)
        {
            if (request == null)
            {
                return Fail(AuthCodeInvalidRequest);
            }

            string normalizedToken = (request.Token ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return Fail(AuthCodeInvalidRequest);
            }

            int userId;

            try
            {
                userId = _tokenService.GetUserIdFromToken(normalizedToken);
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while resolving user id from token in logout.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while resolving user id from token in logout.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while resolving user id from token in logout.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            if (userId < MinValidUserId)
            {
                return Fail(AuthCodeInvalidRequest);
            }

            InMemorySessionManager.Logout(userId, normalizedToken);

            _logger.InfoFormat(
                "Logout completed for user {0}.",
                userId);

            return OkWithCustomCode(AuthCodeOk, userId);
        }

        private AuthResult ValidateLoginRequest(LoginDto request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.Email))
            {
                return Fail(AuthCodeInvalidRequest);
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
                _logger.Error("SQL error while loading credentials for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while loading credentials for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while loading credentials for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
                return false;
            }

            if (!authResult.IsSuccess || authResult.Data == null)
            {
                errorResult = Fail(AuthCodeInvalidCredentials);
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
                        meta[MetaKeySanctionType] = banInfo.SanctionType;
                    }

                    if (banInfo.BanEndsAtUtc.HasValue)
                    {
                        meta[MetaKeyBanEndsAtUtc] = banInfo.BanEndsAtUtc.Value.ToString("o");
                    }

                    errorResult = Fail(AuthCodeBanned, meta);
                    return false;
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while checking ban state for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while checking ban state for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while checking ban state for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
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
                    _logger.WarnFormat(
                        "Login credentials valid but account/profile not found. UserId={0}. Treating as invalid credentials.",
                        userId);

                    errorResult = Fail(AuthCodeInvalidCredentials);
                    return false;
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while loading account data for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return false;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while loading account data for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while loading account data for login.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
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
                ttlText = ConfigurationManager.AppSettings[AppKeyTokenMinutes];

                if (!int.TryParse(ttlText, out ttlMinutes) || ttlMinutes <= InvalidUserId)
                {
                    ttlMinutes = DefaultTokenMinutes;
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while reading token TTL.", ex);
                ttlMinutes = DefaultTokenMinutes;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while reading token TTL.", ex);
                ttlMinutes = DefaultTokenMinutes;
            }

            return ttlMinutes;
        }

        private bool TryIssueToken(
    int userId,
    DateTime expiresAtUtc,
    out string token,
    out AuthResult errorResult)
        {
            token = string.Empty;
            errorResult = null;

            try
            {
                token = _tokenService.IssueToken(userId, expiresAtUtc);
                return true;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while issuing auth token.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return false;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while issuing auth token.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeCrypto);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while issuing auth token.", ex);
                errorResult = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
                return false;
            }
        }








        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail(AuthCodeEmailRequired);
            }

            bool isRegistered;

            try
            {
                isRegistered = _accountsRepository.IsEmailRegistered(email);
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while checking email for verification.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while checking email for verification.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while checking email for verification.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            if (isRegistered)
            {
                return Fail(AuthCodeEmailAlreadyExists);
            }

            VerificationCodeEntry existingEntry;
            if (_verificationCodeStore.TryGet(email, out existingEntry))
            {
                TimeSpan elapsed = DateTime.UtcNow - existingEntry.LastSentUtc;
                if (elapsed < ResendWindow)
                {
                    int secondsToWait = (int)(ResendWindow - elapsed).TotalSeconds;
                    var meta = new Dictionary<string, string>
                    {
                        [MetaKeySeconds] = secondsToWait.ToString()
                    };

                    return Fail(AuthCodeThrottleWait, meta);
                }
            }

            string code;
            DateTime nowUtc = DateTime.UtcNow;

            try
            {
                code = GenerateCode(VerificationCodeDigits);
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while generating email verification code.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while generating email verification code.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            _verificationCodeStore.SaveNewCode(email, code, nowUtc);

            try
            {
                _emailSender.SendVerificationCode(email, code);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Error("Error while sending email verification code.", ex);
                _verificationCodeStore.Remove(email);

                var meta = new Dictionary<string, string>
                {
                    [MetaKeyReason] = ex.GetType().Name,
                    [MetaKeyErrorType] = ErrorTypeEmailSend
                };

                return Fail(AuthCodeEmailSendFailed, meta);
            }
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return Fail(AuthCodeInvalidRequest);
            }

            VerificationCodeEntry entry;
            if (!_verificationCodeStore.TryGet(email, out entry))
            {
                return Fail(AuthCodeCodeNotRequested);
            }

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _verificationCodeStore.Remove(email);
                return Fail(AuthCodeCodeExpired);
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
            {
                _verificationCodeStore.RegisterFailedAttempt(email, entry);
                return Fail(AuthCodeCodeInvalid);
            }

            _verificationCodeStore.Remove(email);
            return Ok();
        }

        public AuthResult RequestPasswordChangeCode(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail(AuthCodeEmailRequired);
            }

            bool isRegistered;

            try
            {
                isRegistered = _accountsRepository.IsEmailRegistered(email);
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while checking email for password change.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while checking email for password change.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while checking email for password change.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            if (!isRegistered)
            {
                return Fail(AuthCodeEmailNotFound);
            }

            VerificationCodeEntry existingEntry;
            if (_verificationCodeStore.TryGet(email, out existingEntry))
            {
                TimeSpan elapsed = DateTime.UtcNow - existingEntry.LastSentUtc;

                if (elapsed < ResendWindow)
                {
                    int secondsToWait = (int)(ResendWindow - elapsed).TotalSeconds;

                    var meta = new Dictionary<string, string>
                    {
                        [MetaKeySeconds] = secondsToWait.ToString()
                    };

                    return Fail(AuthCodeThrottleWait, meta);
                }
            }

            string code;
            DateTime nowUtc = DateTime.UtcNow;

            try
            {
                code = GenerateCode(VerificationCodeDigits);
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while generating password change verification code.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while generating password change verification code.", ex);
                return FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
            }

            _verificationCodeStore.SaveNewCode(email, code, nowUtc);

            try
            {
                _emailSender.SendVerificationCode(email, code);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Error("Error while sending password change verification code.", ex);
                _verificationCodeStore.Remove(email);

                var meta = new Dictionary<string, string>
                {
                    [MetaKeyReason] = ex.GetType().Name,
                    [MetaKeyErrorType] = ErrorTypeEmailSend
                };

                return Fail(AuthCodeEmailSendFailed, meta);
            }
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

            if (IsPasswordReused(validationContext.NewPassword, passwordHistory))
            {
                return Fail(AuthCodePasswordReused);
            }

            PersistPasswordResult persistResult = PersistNewPassword(
                validationContext.UserId,
                validationContext.NewPassword);

            if (!persistResult.IsSuccess)
            {
                return persistResult.Error;
            }

            return OkWithCustomCode(AuthCodeOk, validationContext.UserId);
        }

        public int GetUserIdFromToken(string token)
        {
            return _tokenService.GetUserIdFromToken(token);
        }

        private static string GenerateCode(int digits)
        {
            var bytes = new byte[RandomBytesLength];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(bytes);
            }

            uint value = BitConverter.ToUInt32(bytes, FirstByteIndex);
            uint mod = (uint)Math.Pow(DecimalBase, digits);
            uint number = value % mod;

            return number.ToString(new string(VerificationCodePadChar, digits));
        }

        private static AuthResult Ok()
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthCodeOk,
                Meta = new Dictionary<string, string>()
            };
        }

        private static AuthResult OkWithUser(int userId, string displayName)
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthCodeOk,
                Meta = new Dictionary<string, string>(),
                UserId = userId,
                DisplayName = displayName
            };
        }

        private static AuthResult OkWithUserProfile(int userId, string displayName, string profilePhotoId)
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthCodeOk,
                Meta = new Dictionary<string, string>(),
                UserId = userId,
                DisplayName = displayName,
                ProfilePhotoId = profilePhotoId
            };
        }

        private static AuthResult OkWithCustomCode(string code, int userId)
        {
            return new AuthResult
            {
                Success = true,
                Code = code,
                Meta = new Dictionary<string, string>(),
                UserId = userId
            };
        }

        private static AuthResult Fail(string code, Dictionary<string, string> meta = null)
        {
            return new AuthResult
            {
                Success = false,
                Code = code,
                Meta = meta ?? new Dictionary<string, string>()
            };
        }

        private static AuthResult FailWithErrorType(string code, string errorType)
        {
            var meta = new Dictionary<string, string>
            {
                [MetaKeyErrorType] = errorType
            };

            return Fail(code, meta);
        }

        private ChangePasswordValidationContext ValidateChangePasswordRequest(ChangePasswordRequestDto request)
        {
            var context = new ChangePasswordValidationContext
            {
                UserId = InvalidUserId,
                Email = string.Empty,
                NewPassword = string.Empty,
                VerificationCode = string.Empty,
                Error = null
            };

            if (request == null)
            {
                context.Error = Fail(AuthCodeInvalidRequest);
                return context;
            }

            string rawEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string newPassword = request.NewPassword ?? string.Empty;
            string verificationCode = (request.VerificationCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(rawEmail)
                || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(verificationCode))
            {
                context.Error = Fail(AuthCodeInvalidRequest);
                return context;
            }

            if (!IsPasswordFormatValid(newPassword))
            {
                context.Error = Fail(AuthCodePasswordWeak);
                return context;
            }

            try
            {
                OperationResult<AuthCredentialsDto> authResult =
                    _accountsRepository.GetAuthByIdentifier(rawEmail);

                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    context.Error = Fail(AuthCodeInvalidCredentials);
                    return context;
                }

                context.UserId = authResult.Data.UserId;
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while loading user for password change.", ex);
                context.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return context;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while loading user for password change.", ex);
                context.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return context;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while loading user for password change.", ex);
                context.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
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
                result.Error = Fail(AuthCodeInvalidRequest);
                return result;
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();

            VerificationCodeEntry entry;
            if (!_verificationCodeStore.TryGet(normalizedEmail, out entry))
            {
                result.Error = Fail(AuthCodeCodeNotRequested);
                return result;
            }

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _verificationCodeStore.Remove(normalizedEmail);
                result.Error = Fail(AuthCodeCodeExpired);
                return result;
            }

            if (!string.Equals(verificationCode, entry.Code, StringComparison.Ordinal))
            {
                _verificationCodeStore.RegisterFailedAttempt(normalizedEmail, entry);
                result.Error = Fail(AuthCodeCodeInvalid);
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
                historyResult = _accountsRepository.GetLastPasswordHashes(userId, PasswordHistoryLimit);
            }
            catch (SqlException ex)
            {
                _logger.Error("SQL error while loading password history.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return result;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while loading password history.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while loading password history.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
                return result;
            }

            if (!historyResult.IsSuccess || historyResult.Data == null || historyResult.Data.Count == 0)
            {
                _logger.WarnFormat(
                    "Password change requested but no password history found. UserId={0}",
                    userId);

                result.Error = Fail(AuthCodeServerError);
                return result;
            }

            result.IsValid = true;
            result.History = historyResult.Data;
            return result;
        }

        private bool IsPasswordReused(string newPassword, IReadOnlyList<string> passwordHistory)
        {
            for (int index = 0; index < passwordHistory.Count; index++)
            {
                string oldHash = passwordHistory[index];

                if (string.IsNullOrWhiteSpace(oldHash))
                {
                    continue;
                }

                if (_passwordHasher.Verify(newPassword, oldHash))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPasswordFormatValid(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            {
                return false;
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;

            for (int index = 0; index < password.Length; index++)
            {
                char character = password[index];

                if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
            }

            return hasUpper && hasLower && hasDigit;
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
                _logger.Error("SQL error while inserting new password hash.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeSql);
                return result;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while inserting new password hash.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeConfig);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while inserting new password hash.", ex);
                result.Error = FailWithErrorType(AuthCodeServerError, ErrorTypeUnexpected);
                return result;
            }

            if (!addResult.IsSuccess || !addResult.Data)
            {
                _logger.ErrorFormat(
                    "Failed to insert new password hash. UserId={0}",
                    userId);

                result.Error = Fail(AuthCodeServerError);
                return result;
            }

            result.IsSuccess = true;
            return result;
        }
    }
}
