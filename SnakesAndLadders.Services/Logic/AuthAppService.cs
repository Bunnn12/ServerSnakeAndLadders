using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    /// <summary>
    /// Application service for authentication, token issuing and email verification.
    /// </summary>
    public sealed class AuthAppService : IAuthAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AuthAppService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly IPlayerReportAppService _playerReportAppService;
        private readonly IUserRepository _userRepository;

        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresUtc, DateTime LastSentUtc)> _verificationCodesCache =
            new ConcurrentDictionary<string, (string, DateTime, DateTime)>(StringComparer.OrdinalIgnoreCase);

        private const int VERIFICATION_CODE_DIGITS = 6;
        private const int RANDOM_BYTES_LENGTH = 4;

        private static readonly TimeSpan VERIFICATION_TTL = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RESEND_WINDOW = TimeSpan.FromSeconds(45);

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_BANNED = "Auth.Banned";
        private const string AUTH_CODE_INVALID_REQUEST = "Auth.InvalidRequest";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS = "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_SERVER_ERROR = "Auth.ServerError";
        private const string AUTH_CODE_INVALID_CREDENTIALS = "Auth.InvalidCredentials";
        private const string AUTH_CODE_EMAIL_REQUIRED = "Auth.EmailRequired";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_EMAIL_SEND_FAILED = "Auth.EmailSendFailed";
        private const string AUTH_CODE_CODE_NOT_REQUESTED = "Auth.CodeNotRequested";
        private const string AUTH_CODE_CODE_EXPIRED = "Auth.CodeExpired";
        private const string AUTH_CODE_CODE_INVALID = "Auth.CodeInvalid";
        private const string AUTH_CODE_ACCOUNT_DELETED = "Auth.AccountDeleted";

        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";
        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_REASON = "reason";

        private const int DEFAULT_TOKEN_MINUTES = 10080;
        private const string APP_KEY_SECRET = "Auth:Secret";
        private const string APP_KEY_TOKEN_MINUTES = "Auth:TokenMinutes";

        public AuthAppService(
            IAccountsRepository accountsRepository,
            IPasswordHasher passwordHasher,
            IEmailSender emailSender,
            IPlayerReportAppService playerReportAppService,
            IUserRepository userRepository)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _playerReportAppService = playerReportAppService ?? throw new ArgumentNullException(nameof(playerReportAppService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <summary>
        /// Registers a new user and returns an AuthResult with the created user id.
        /// </summary>
        public AuthResult RegisterUser(RegistrationDto registration)
        {
            if (registration == null
                || string.IsNullOrWhiteSpace(registration.Email)
                || string.IsNullOrWhiteSpace(registration.Password)
                || string.IsNullOrWhiteSpace(registration.UserName))
            {
                return Fail(AUTH_CODE_INVALID_REQUEST);
            }

            // Validaciones previas (usando métodos booleanos del repo)
            if (_accountsRepository.IsEmailRegistered(registration.Email))
            {
                return Fail(AUTH_CODE_EMAIL_ALREADY_EXISTS);
            }

            if (_accountsRepository.IsUserNameTaken(registration.UserName))
            {
                return Fail(AUTH_CODE_EMAIL_ALREADY_EXISTS.Replace("Email", "UserName"));
            }

            string passwordHash = _passwordHasher.Hash(registration.Password);

            var requestDto = new CreateAccountRequestDto
            {
                Username = registration.UserName,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                Email = registration.Email,
                PasswordHash = passwordHash
            };

            // CORRECCIÓN 1: Manejo de DbOperationResult
            var createResult = _accountsRepository.CreateUserWithAccountAndPassword(requestDto);

            if (!createResult.IsSuccess)
            {
                // Si el repositorio falló (error de BD, etc.), devolvemos error de servidor
                // Opcional: Podrías loguear createResult.ErrorMessage
                return Fail(AUTH_CODE_SERVER_ERROR);
            }

            // Extraemos el ID de la "caja"
            int newUserId = createResult.Data;

            return Ok(userId: newUserId, displayName: registration.UserName);
        }

        /// <summary>
        /// Authenticates a user and issues a token if credentials are valid and the user is not banned.
        /// </summary>
        public AuthResult Login(LoginDto request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.Email))
            {
                return Fail(AUTH_CODE_INVALID_REQUEST);
            }

            var authResult = _accountsRepository.GetAuthByIdentifier(request.Email);

            if (!authResult.IsSuccess || authResult.Data == null)
            {
                return Fail(AUTH_CODE_INVALID_CREDENTIALS);
            }

            AuthCredentialsDto auth = authResult.Data;

            int userId = auth.UserId;
            string passwordHash = auth.PasswordHash;
            string displayName = auth.DisplayName;
            string profilePhotoId = auth.ProfilePhotoId;

            if (!_passwordHasher.Verify(request.Password, passwordHash))
            {
                return Fail(AUTH_CODE_INVALID_CREDENTIALS);
            }

            try
            {
                var banInfo = _playerReportAppService.GetCurrentBan(userId);

                if (banInfo != null && banInfo.IsBanned)
                {
                    var meta = new Dictionary<string, string>();

                    if (!string.IsNullOrWhiteSpace(banInfo.SanctionType))
                    {
                        meta[META_KEY_SANCTION_TYPE] = banInfo.SanctionType;
                    }

                    if (banInfo.BanEndsAtUtc.HasValue)
                    {
                        meta[META_KEY_BAN_ENDS_AT_UTC] = banInfo.BanEndsAtUtc.Value.ToString("o");
                    }

                    return Fail(AUTH_CODE_BANNED, meta);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while checking ban state for login.", ex);
                return Fail(AUTH_CODE_SERVER_ERROR);
            }

            AccountDto account;

            try
            {
                account = _userRepository.GetByUserId(userId);

                if (account == null)
                {
                    Logger.WarnFormat(
                        "Login credentials valid but account/profile not found. UserId={0}. Treating as invalid credentials.",
                        userId);

                    return Fail(AUTH_CODE_INVALID_CREDENTIALS);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while loading account data for login.", ex);
                return Fail(AUTH_CODE_SERVER_ERROR);
            }

            string ttlText = ConfigurationManager.AppSettings[APP_KEY_TOKEN_MINUTES];
            int ttlMinutes;
            if (!int.TryParse(ttlText, out ttlMinutes) || ttlMinutes <= 0)
            {
                ttlMinutes = DEFAULT_TOKEN_MINUTES;
            }

            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(ttlMinutes);

            string token;
            try
            {
                token = IssueToken(userId, expiresAtUtc);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while issuing auth token.", ex);
                return Fail(AUTH_CODE_SERVER_ERROR);
            }

            var result = Ok(
                userId: userId,
                displayName: displayName,
                profilePhotoId: profilePhotoId);

            result.CurrentSkinId = account.CurrentSkinId;
            result.CurrentSkinUnlockedId = account.CurrentSkinUnlockedId;

            result.Token = token;
            result.ExpiresAtUtc = expiresAtUtc;

            return result;
        }

        private static string IssueToken(int userId, DateTime expiresAtUtc)
        {
            string secret = ConfigurationManager.AppSettings[APP_KEY_SECRET];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Auth secret is not configured (Auth:Secret).");
            }

            long expUnix = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
            string payload = $"{userId}|{expUnix}";
            string signatureHex = ComputeHmacHex(secret, payload);
            string raw = $"{payload}|{signatureHex}";

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        /// <summary>
        /// Requests an email verification code for the given email.
        /// </summary>
        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail(AUTH_CODE_EMAIL_REQUIRED);
            }

            if (_accountsRepository.IsEmailRegistered(email))
            {
                return Fail(AUTH_CODE_EMAIL_ALREADY_EXISTS);
            }

            if (_verificationCodesCache.TryGetValue(email, out var entry))
            {
                TimeSpan elapsed = DateTime.UtcNow - entry.LastSentUtc;
                if (elapsed < RESEND_WINDOW)
                {
                    int secondsToWait = (int)(RESEND_WINDOW - elapsed).TotalSeconds;
                    return Fail(
                        AUTH_CODE_THROTTLE_WAIT,
                        new Dictionary<string, string> { [META_KEY_SECONDS] = secondsToWait.ToString() });
                }
            }

            string code = GenerateCode(VERIFICATION_CODE_DIGITS);
            DateTime nowUtc = DateTime.UtcNow;

            _verificationCodesCache[email] = (code, nowUtc.Add(VERIFICATION_TTL), nowUtc);

            try
            {
                _emailSender.SendVerificationCode(email, code);
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while sending email verification code.", ex);
                _verificationCodesCache.TryRemove(email, out _);
                return Fail(
                    AUTH_CODE_EMAIL_SEND_FAILED,
                    new Dictionary<string, string> { [META_KEY_REASON] = ex.GetType().Name });
            }
        }

        /// <summary>
        /// Confirms an email verification code previously requested.
        /// </summary>
        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return Fail(AUTH_CODE_INVALID_REQUEST);
            }

            if (!_verificationCodesCache.TryGetValue(email, out var entry))
            {
                return Fail(AUTH_CODE_CODE_NOT_REQUESTED);
            }

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _verificationCodesCache.TryRemove(email, out _);
                return Fail(AUTH_CODE_CODE_EXPIRED);
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
            {
                return Fail(AUTH_CODE_CODE_INVALID);
            }

            _verificationCodesCache.TryRemove(email, out _);
            return Ok();
        }

        private static string GenerateCode(int digits)
        {
            var bytes = new byte[RANDOM_BYTES_LENGTH];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(bytes);
            }

            uint value = BitConverter.ToUInt32(bytes, 0);
            uint mod = (uint)Math.Pow(10, digits);
            uint number = value % mod;

            return number.ToString(new string('0', digits));
        }

        private static AuthResult Ok(
            string code = AUTH_CODE_OK,
            Dictionary<string, string> meta = null,
            int? userId = null,
            string displayName = null,
            string profilePhotoId = null)
        {
            return new AuthResult
            {
                Success = true,
                Code = code,
                Meta = meta,
                UserId = userId,
                DisplayName = displayName,
                ProfilePhotoId = profilePhotoId
            };
        }

        private static AuthResult Fail(string code, Dictionary<string, string> meta = null)
        {
            return new AuthResult
            {
                Success = false,
                Code = code,
                Meta = meta
            };
        }

        private static string ComputeHmacHex(string secret, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var stringBuilder = new StringBuilder(bytes.Length * 2);

                for (int index = 0; index < bytes.Length; index++)
                {
                    stringBuilder.Append(bytes[index].ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }

        public int GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return 0;
            }

            if (int.TryParse(token, out int userIdCompat))
            {
                return userIdCompat > 0 ? userIdCompat : 0;
            }

            try
            {
                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = raw.Split('|');

                if (parts.Length != 3)
                {
                    return 0;
                }

                if (!int.TryParse(parts[0], out int userId) || userId <= 0)
                {
                    return 0;
                }

                if (!long.TryParse(parts[1], out long expUnix) || expUnix <= 0)
                {
                    return 0;
                }

                long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (nowUnix > expUnix)
                {
                    return 0;
                }

                string secret = ConfigurationManager.AppSettings[APP_KEY_SECRET] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(secret))
                {
                    return 0;
                }

                string expected = ComputeHmacHex(secret, $"{userId}|{expUnix}");

                return string.Equals(expected, parts[2], StringComparison.OrdinalIgnoreCase)
                    ? userId
                    : 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error while validating auth token.", ex);
                return 0;
            }
        }
    }
}
