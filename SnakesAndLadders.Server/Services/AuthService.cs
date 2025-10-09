using System;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Infrastructure.Repositories;

namespace SnakeAndLadders.Host.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountsRepository _repo = new AccountsRepository();

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
            return new AuthResult { Success = true, UserId = id, DisplayName = r.UserName, Message = "Registro OK." };
        }

        public AuthResult Login(LoginDto r)
        {
            var auth = _repo.GetAuthByEmail(r.Email);
            if (auth == null) return new AuthResult { Success = false, Message = "Credenciales inválidas." };
            var (id, hash, name) = auth.Value;
            return Verify(r.Password, hash)
                ? new AuthResult { Success = true, UserId = id, DisplayName = name, Message = "Login OK." }
                : new AuthResult { Success = false, Message = "Credenciales inválidas." };
        }

        // Placeholder: cámbialo luego por BCrypt/Argon2
        private static string Hash(string s) =>
            BitConverter.ToString(System.Security.Cryptography.SHA256.Create()
             .ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-", "");
        private static bool Verify(string p, string h) => Hash(p) == h;
    }
}
