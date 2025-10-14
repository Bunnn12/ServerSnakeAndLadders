using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Host.Helpers;
using SnakeAndLadders.Infrastructure.Repositories;

namespace SnakeAndLadders.Host.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountsRepository _repo = new AccountsRepository();

        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresUtc, DateTime LastSentUtc)> _codes
            = new ConcurrentDictionary<string, (string, DateTime, DateTime)>(StringComparer.OrdinalIgnoreCase);

        private const int VerificationCodeDigits = 6;
        private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(45);

        // Códigos estables para UI (puedes moverlos a un assembly Contracts si quieres compartirlos)
        private static class Codes
        {
            public const string Ok = "Auth.Ok";
            public const string InvalidRequest = "Auth.InvalidRequest";
            public const string EmailRequired = "Auth.EmailRequired";
            public const string EmailAlreadyExists = "Auth.EmailAlreadyExists";
            public const string UserNameAlreadyExists = "Auth.UserNameAlreadyExists";
            public const string InvalidCredentials = "Auth.InvalidCredentials";
            public const string ThrottleWait = "Auth.ThrottleWait";     // Meta["seconds"]
            public const string CodeNotRequested = "Auth.CodeNotRequested";
            public const string CodeExpired = "Auth.CodeExpired";
            public const string CodeInvalid = "Auth.CodeInvalid";
            public const string EmailSendFailed = "Auth.EmailSendFailed";  // Meta["reason"] opcional
            public const string ServerError = "Auth.ServerError";
        }

        private static AuthResult Ok(string code = Codes.Ok, Dictionary<string, string> meta = null,
                                     int? userId = null, string displayName = null)
            => new AuthResult { Success = true, Code = code, Meta = meta, UserId = userId, DisplayName = displayName };

        private static AuthResult Fail(string code, Dictionary<string, string> meta = null)
            => new AuthResult { Success = false, Code = code, Meta = meta };

        public AuthResult Register(RegistrationDto r)
        {
            if (r == null ||
                string.IsNullOrWhiteSpace(r.Email) ||
                string.IsNullOrWhiteSpace(r.Password) ||
                string.IsNullOrWhiteSpace(r.UserName))
            {
                return Fail(Codes.InvalidRequest);
            }

            if (_repo.EmailExists(r.Email)) return Fail(Codes.EmailAlreadyExists);
            if (_repo.UserNameExists(r.UserName)) return Fail(Codes.UserNameAlreadyExists);

            try
            {
                var hash = Hash(r.Password);
                var id = _repo.CreateUserWithAccountAndPassword(r.UserName, r.FirstName, r.LastName, r.Email, hash);
                return Ok(userId: id, displayName: r.UserName);
            }
            catch (SqlException ex)
            {
                // 2601/2627: violación de índice único; 1205: deadlock
                if (ex.Number == 2601 || ex.Number == 2627) return Fail(Codes.EmailAlreadyExists);
                return Fail(Codes.ServerError);
            }
            catch
            {
                return Fail(Codes.ServerError);
            }
        }

        public AuthResult Login(LoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
                return Fail(Codes.InvalidRequest);

            var auth = _repo.GetAuthByIdentifier(request.Email); // email o username
            if (auth == null) return Fail(Codes.InvalidCredentials);

            var (userId, hash, display) = auth.Value;
            if (!Verify(request.Password, hash)) return Fail(Codes.InvalidCredentials);

            return Ok(userId: userId, displayName: display);
        }

        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) return Fail(Codes.EmailRequired);

            // Si ya existe, no se envía código de alta
            if (_repo.EmailExists(email)) return Fail(Codes.EmailAlreadyExists);

            // Anti-spam / reenvío
            if (_codes.TryGetValue(email, out var entry))
            {
                var elapsed = DateTime.UtcNow - entry.LastSentUtc;
                if (elapsed < ResendWindow)
                {
                    int wait = (int)(ResendWindow - elapsed).TotalSeconds;
                    return Fail(Codes.ThrottleWait, new Dictionary<string, string> { ["seconds"] = wait.ToString() });
                }
            }

            // Generar y guardar
            string code = GenerateCode(VerificationCodeDigits);
            var expires = DateTime.UtcNow.Add(VerificationTtl);
            _codes[email] = (code, expires, DateTime.UtcNow);

            try
            {
                EmailSender.SendVerificationCode(email, code);
                return Ok();
            }
            catch (Exception ex)
            {
                _codes.TryRemove(email, out _);
                return Fail(Codes.EmailSendFailed, new Dictionary<string, string> { ["reason"] = ex.GetType().Name });
            }
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return Fail(Codes.InvalidRequest);

            if (!_codes.TryGetValue(email, out var entry))
                return Fail(Codes.CodeNotRequested);

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _codes.TryRemove(email, out _);
                return Fail(Codes.CodeExpired);
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
                return Fail(Codes.CodeInvalid);

            _codes.TryRemove(email, out _);
            return Ok();
        }

        private static string Hash(string s) =>
            BitConverter.ToString(SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-", "");

        private static bool Verify(string p, string h) => Hash(p) == h;

        private static string GenerateCode(int digits)
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(bytes); }
            uint value = BitConverter.ToUInt32(bytes, 0);
            uint mod = (uint)Math.Pow(10, digits);
            uint num = value % mod;
            return num.ToString(new string('0', digits)); // ceros a la izquierda
        }
    }
}
