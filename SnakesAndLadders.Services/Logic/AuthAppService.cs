using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class AuthAppService : IAuthAppService
    {
        private readonly IAccountsRepository _repo;
        private readonly IPasswordHasher _hasher;
        private readonly IEmailSender _email;
        private readonly IPlayerReportAppService _playerReportApp;


        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresUtc, DateTime LastSentUtc)> _codes
            = new ConcurrentDictionary<string, (string, DateTime, DateTime)>(StringComparer.OrdinalIgnoreCase);

        private const int VerificationCodeDigits = 6;
        private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(45);

        private const string AUTH_CODE_BANNED = "Auth.Banned";
        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";


        public AuthAppService(
            IAccountsRepository repo,
            IPasswordHasher hasher,
            IEmailSender email,
            IPlayerReportAppService playerReportApp)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _playerReportApp = playerReportApp ?? throw new ArgumentNullException(nameof(playerReportApp));
        }


        public AuthResult RegisterUser(RegistrationDto registration)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.Email) ||
                string.IsNullOrWhiteSpace(registration.Password) || string.IsNullOrWhiteSpace(registration.UserName))
                return Fail("Auth.InvalidRequest");

            if (_repo.EmailExists(registration.Email)) return Fail("Auth.EmailAlreadyExists");
            if (_repo.UserNameExists(registration.UserName)) return Fail("Auth.UserNameAlreadyExists");

            try
            {
                var passwordHash = _hasher.Hash(registration.Password);
                var requestDto = new CreateAccountRequestDto
                {
                    Username = registration.UserName,
                    FirstName = registration.FirstName,          
                    LastName = registration.LastName,           
                    Email = registration.Email,
                    PasswordHash = passwordHash
                };

                var newUserId = _repo.CreateUserWithAccountAndPassword(requestDto);

                return Ok(userId: newUserId, displayName: registration.UserName);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return Fail("Auth.EmailAlreadyExists");
            }
            catch (Exception ex)
            {
                return Fail("Auth.ServerError");
            }
        }

        public AuthResult Login(LoginDto request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return Fail("Auth.InvalidRequest");
            }

            var auth = _repo.GetAuthByIdentifier(request.Email);
            if (auth == null)
            {
                return Fail("Auth.InvalidCredentials");
            }

            var (userId, hash, display, photoId) = auth.Value;

            if (!_hasher.Verify(request.Password, hash))
            {
                return Fail("Auth.InvalidCredentials");
            }

            try
            {
                var banInfo = _playerReportApp.GetCurrentBan(userId);

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
            catch (Exception)
            {
                return Fail("Auth.ServerError");
            }

            return Ok(
                userId: userId,
                displayName: display,
                profilePhotoId: photoId
            );

        }

        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) return Fail("Auth.EmailRequired");
            if (_repo.EmailExists(email)) return Fail("Auth.EmailAlreadyExists");

            if (_codes.TryGetValue(email, out var entry))
            {
                var elapsed = DateTime.UtcNow - entry.LastSentUtc;
                if (elapsed < ResendWindow)
                {
                    int wait = (int)(ResendWindow - elapsed).TotalSeconds;
                    return Fail("Auth.ThrottleWait", new Dictionary<string, string> { ["seconds"] = wait.ToString() });
                }
            }

            string code = GenerateCode(VerificationCodeDigits);
            var now = DateTime.UtcNow;
            _codes[email] = (code, now.Add(VerificationTtl), now);

            try { _email.SendVerificationCode(email, code); return Ok(); }
            catch (Exception ex)
            {
                _codes.TryRemove(email, out _);
                return Fail("Auth.EmailSendFailed", new Dictionary<string, string> { ["reason"] = ex.GetType().Name });
            }
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return Fail("Auth.InvalidRequest");

            if (!_codes.TryGetValue(email, out var entry)) return Fail("Auth.CodeNotRequested");

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _codes.TryRemove(email, out _);
                return Fail("Auth.CodeExpired");
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
                return Fail("Auth.CodeInvalid");

            _codes.TryRemove(email, out _);
            return Ok();
        }

        private static string GenerateCode(int digits)
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(bytes); }
            uint value = BitConverter.ToUInt32(bytes, 0);
            uint mod = (uint)Math.Pow(10, digits);
            uint num = value % mod;
            return num.ToString(new string('0', digits));
        }

        private static AuthResult Ok(
            string code = "Auth.Ok",
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
            => new AuthResult { Success = false, Code = code, Meta = meta };
    }
}
