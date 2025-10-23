using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class AuthAppService : IAuthAppService
    {
        private readonly IAccountsRepository _repo;
        private readonly IPasswordHasher _hasher;
        private readonly IEmailSender _email;

        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresUtc, DateTime LastSentUtc)> _codes
            = new ConcurrentDictionary<string, (string, DateTime, DateTime)>(StringComparer.OrdinalIgnoreCase);

        private const int VerificationCodeDigits = 6;
        private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(45);

        public AuthAppService(IAccountsRepository repo, IPasswordHasher hasher, IEmailSender email)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _email = email ?? throw new ArgumentNullException(nameof(email));
        }

        public AuthResult Register(RegistrationDto registration)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.Email) ||
                string.IsNullOrWhiteSpace(registration.Password) || string.IsNullOrWhiteSpace(registration.UserName))
                return Fail("Auth.InvalidRequest");

            if (_repo.EmailExists(registration.Email)) return Fail("Auth.EmailAlreadyExists");
            if (_repo.UserNameExists(registration.UserName)) return Fail("Auth.UserNameAlreadyExists");

            try
            {
                var hash = _hasher.Hash(registration.Password);
                var id = _repo.CreateUserWithAccountAndPassword(registration.UserName, registration.FirstName, registration.LastName, registration.Email, hash);
                return Ok(userId: id, displayName: registration.UserName);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return Fail("Auth.EmailAlreadyExists");
            }
            catch
            {
                return Fail("Auth.ServerError");
            }
        }

        public AuthResult Login(LoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
                return Fail("Auth.InvalidRequest");

            var auth = _repo.GetAuthByIdentifier(request.Email); 
            if (auth == null) return Fail("Auth.InvalidCredentials");

            var (userId, hash, display) = auth.Value;
            if (!_hasher.Verify(request.Password, hash)) return Fail("Auth.InvalidCredentials");

            return Ok(userId: userId, displayName: display);
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

        private static AuthResult Ok(string code = "Auth.Ok", Dictionary<string, string> meta = null,
                                     int? userId = null, string displayName = null)
            => new AuthResult { Success = true, Code = code, Meta = meta, UserId = userId, DisplayName = displayName };

        private static AuthResult Fail(string code, Dictionary<string, string> meta = null)
            => new AuthResult { Success = false, Code = code, Meta = meta };
    }
}
