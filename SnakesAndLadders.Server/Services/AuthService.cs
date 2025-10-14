using System;
using System.Collections.Concurrent;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Infrastructure.Repositories;
using System.Security.Cryptography;
using SnakeAndLadders.Host.Helpers;


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

        public AuthResult Register(RegistrationDto r)
        {
            if (string.IsNullOrWhiteSpace(r.Email) ||
                string.IsNullOrWhiteSpace(r.Password) ||
                string.IsNullOrWhiteSpace(r.UserName))
                return new AuthResult { Success = false, Message = "Datos incompletos." };

            if (_repo.EmailExists(r.Email)) return new AuthResult { Success = false, Message = "Correo ya existe." };
            if (_repo.UserNameExists(r.UserName)) return new AuthResult { Success = false, Message = "Usuario ya existe." };

            var hash = Hash(r.Password);
            var id = _repo.CreateUserWithAccountAndPassword(r.UserName, r.FirstName, r.LastName, r.Email, hash);

            return new AuthResult
            {
                Success = true,
                UserId = id,
                DisplayName = r.UserName,
                Message = "Registro OK."
            };
        }

        public AuthResult Login(LoginDto request)
        {
            var auth = _repo.GetAuthByIdentifier(request.Email); // email o username
            if (auth == null)
                return new AuthResult { Success = false, Message = "Usuario/correo o contraseña inválidos." };

            var (userId, hash, display) = auth.Value;
            if (!Verify(request.Password, hash))
                return new AuthResult { Success = false, Message = "Usuario/correo o contraseña inválidos." };

            return new AuthResult
            {
                Success = true,
                UserId = userId,
                DisplayName = display,
                Message = "Inicio de sesión correcto."
            };
        }

        public AuthResult RequestEmailVerification(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return new AuthResult { Success = false, Message = "Correo requerido." };

            // Para alta nueva, si ya existe en DB, no enviamos código.
            if (_repo.EmailExists(email))
                return new AuthResult { Success = false, Message = "Ese correo ya está registrado." };

            // Anti-spam / reenvío
            if (_codes.TryGetValue(email, out var entry))
            {
                var elapsed = DateTime.UtcNow - entry.LastSentUtc;
                if (elapsed < ResendWindow)
                {
                    var wait = (int)(ResendWindow - elapsed).TotalSeconds;
                    return new AuthResult { Success = false, Message = $"Espera {wait}s para reenviar el código." };
                }
            }

            // Generar y guardar
            string code = GenerateCode(VerificationCodeDigits);
            var expires = DateTime.UtcNow.Add(VerificationTtl);
            _codes[email] = (code, expires, DateTime.UtcNow);

            // Enviar correo real
            try
            {
                EmailSender.SendVerificationCode(email, code);
            }
            catch (Exception ex)
            {
                // Si falla el envío, limpia el código para no dejar basura
                _codes.TryRemove(email, out _);
                return new AuthResult { Success = false, Message = "No se pudo enviar el correo: " + ex.Message };
            }

            // Mensaje de éxito
            return new AuthResult { Success = true, Message = "Verification code sent" };
        }


        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            code = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return new AuthResult { Success = false, Message = "Correo y código son requeridos." };

            if (!_codes.TryGetValue(email, out var entry))
                return new AuthResult { Success = false, Message = "Solicita un código primero." };

            if (DateTime.UtcNow > entry.ExpiresUtc)
            {
                _codes.TryRemove(email, out _);
                return new AuthResult { Success = false, Message = "Código expirado. Solicítalo de nuevo." };
            }

            if (!string.Equals(code, entry.Code, StringComparison.Ordinal))
                return new AuthResult { Success = false, Message = "Código inválido." };

            // OK: limpiar el registro temporal
            _codes.TryRemove(email, out _);
            return new AuthResult { Success = true, Message = "Código verificado." };
        }

        private static string Hash(string s) =>
            BitConverter.ToString(System.Security.Cryptography.SHA256.Create()
             .ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-", "");

        private static bool Verify(string p, string h) => Hash(p) == h;

        private static string GenerateCode(int digits)
        {
            // Código numérico con ceros a la izquierda (p.ej., 6 -> "042317")
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            uint value = BitConverter.ToUInt32(bytes, 0);
            uint mod = (uint)Math.Pow(10, digits); // 10^digits
            uint num = value % mod;

            return num.ToString(new string('0', digits)); // formatea con ceros a la izquierda
        }

    }
}
