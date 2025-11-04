using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Server.Helpers;

namespace ServerSnakesAndLadders
{
    public class AccountsRepository : IAccountsRepository
    {
        public bool EmailExists(string emailAddress)
        {
            var normalized = Normalize(emailAddress, 200);
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)db).ObjectContext.CommandTimeout = 30;
                return db.Cuenta.AsNoTracking().Any(c => c.Correo == normalized);
            }
        }

        public bool UserNameExists(string username)
        {
            var normalized = Normalize(username, 90);
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)db).ObjectContext.CommandTimeout = 30;
                return db.Usuario.AsNoTracking().Any(u => u.NombreUsuario == normalized);
            }
        }


        public int CreateUserWithAccountAndPassword(CreateAccountRequestDto createAccountRequest)
        {
            if (createAccountRequest == null) throw new ArgumentNullException(nameof(createAccountRequest));

            var username = RequireParam(Normalize(createAccountRequest.Username, 90), nameof(createAccountRequest.Username));
            var firstName = Normalize(createAccountRequest.FirstName, 90);
            var lastName = Normalize(createAccountRequest.LastName, 90);
            var profileDescription = Normalize(createAccountRequest.ProfileDescription, 510);
            var profilePhotoId = Normalize(createAccountRequest.ProfilePhotoId, 5);
            var email = RequireParam(Normalize(createAccountRequest.Email, 200), nameof(createAccountRequest.Email));
            var passwordHash = RequireParam(Normalize(createAccountRequest.PasswordHash, 510), nameof(createAccountRequest.PasswordHash));

            using (var db = new SnakeAndLaddersDBEntities1())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    var userRow = new Usuario
                    {
                        NombreUsuario = username,
                        Nombre = firstName,
                        Apellidos = lastName,
                        DescripcionPerfil = string.IsNullOrWhiteSpace(profileDescription) ? null : profileDescription,
                        FotoPerfil = string.IsNullOrWhiteSpace(profilePhotoId) ? null : profilePhotoId,
                        Monedas = 0,
                        Estado = new byte[] { 1 }
                    };
                    db.Usuario.Add(userRow);
                    db.SaveChanges(); 

                    var accountRow = new Cuenta
                    {
                        UsuarioIdUsuario = userRow.IdUsuario,
                        Correo = email,
                        Estado = new byte[] { 1 }
                    };
                    db.Cuenta.Add(accountRow);
                    db.SaveChanges(); 

                    var passwordRow = new Contrasenia
                    {
                        UsuarioIdUsuario = userRow.IdUsuario,
                        Contrasenia1 = passwordHash,
                        Estado = new byte[] { 1 },
                        FechaCreacion = DateTime.UtcNow,
                        Cuenta = accountRow      
                    };
                    db.Contrasenia.Add(passwordRow);

                    db.SaveChanges();
                    tx.Commit();
                    return userRow.IdUsuario;
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    tx.Rollback();
                    throw;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static string Normalize(string value, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var trimmed = value.Trim();
            return trimmed.Length <= maxLen ? trimmed : trimmed.Substring(0, maxLen);
        }

        private static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} is required.", paramName);
            return value;
        }

        public (int userId, string passwordHash, string displayName, string profilePhotoId)? GetAuthByIdentifier(string identifier)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var cuenta = db.Cuenta.AsNoTracking()
                    .Where(c => c.Correo == identifier)
                    .Select(c => new { c.UsuarioIdUsuario })
                    .FirstOrDefault();

                int userId = 0;

                if (cuenta != null)
                {
                    userId = cuenta.UsuarioIdUsuario;
                }
                else
                {
                    var usuario = db.Usuario.AsNoTracking()
                        .Where(u => u.NombreUsuario == identifier)
                        .Select(u => new { u.IdUsuario })
                        .FirstOrDefault();

                    if (usuario == null)
                    {
                        return null;
                    }

                    userId = usuario.IdUsuario;
                }

                var lastPwd = db.Contrasenia.AsNoTracking()
                    .Where(p => p.UsuarioIdUsuario == userId)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => p.Contrasenia1)
                    .FirstOrDefault();

                if (lastPwd == null)
                {
                    return null;
                }

                var userData = db.Usuario.AsNoTracking()
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => new
                    {
                        u.NombreUsuario,
                        u.FotoPerfil
                    })
                    .FirstOrDefault();

                if (userData == null)
                {
                    return null;
                }

                var normalizedAvatarId = AvatarIdHelper.MapFromDb(userData.FotoPerfil);

                return (userId, lastPwd, userData.NombreUsuario, normalizedAvatarId);
            }
        }
    }
}
