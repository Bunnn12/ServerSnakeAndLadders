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

        public AuthResult Login(LoginDto request)
        {
            // Email' significa "identificador": puede ser usuario o correo
            var auth = _repo.GetAuthByIdentifier(request.Email);
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


        // Placeholder: cámbialo luego por BCrypt/Argon2
        private static string Hash(string s) =>
            BitConverter.ToString(System.Security.Cryptography.SHA256.Create()
             .ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-", "");
        private static bool Verify(string p, string h) => Hash(p) == h;
    }
}
