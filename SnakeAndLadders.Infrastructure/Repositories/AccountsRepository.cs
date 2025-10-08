using System;
using System.Data.Entity;                  
using System.Linq;
using ServerSnakesAndLadders;

namespace SnakeAndLadders.Infrastructure.Repositories
{
    public class AccountsRepository : IAccountsRepository
    {
        public bool EmailExists(string email)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                return db.Cuenta.Any(c => c.Correo == email);
            }
        }

        public bool UserNameExists(string userName)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                return db.Usuario.Any(u => u.NombreUsuario == userName);
            }
        }

        public int CreateUserWithAccountAndPassword(
            string userName,
            string firstName,
            string lastName,
            string email,
            string passwordHash)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1) User
                    var user = new Usuario
                    {
                        NombreUsuario = userName,
                        Nombre = firstName,
                        Apellidos = lastName,
                        Monedas = 0,
                        Estado = new byte[] { 1 }
                    };
                    db.Usuario.Add(user);

                    // 2) Account (FK al user)
                    var account = new Cuenta
                    {
                        Correo = email,
                        Estado = new byte[] { 1 },
                        UsuarioIdUsuario = user.IdUsuario // EF rellena tras SaveChanges
                    };
                    // puedes enlazar por navegación si existe: user.Cuenta.Add(account);
                    db.Cuenta.Add(account);

                    // 3) Password (FK a account y user)
                    var pwd = new Contrasenia
                    {
                        CuentaIdCuenta = account.IdCuenta,
                        UsuarioIdUsuario = user.IdUsuario,
                        Contrasenia1 = passwordHash,    // guarda el HASH aquí
                        Estado = new byte[] { 1 },
                        FechaCreacion = DateTime.UtcNow
                    };
                    // o vía navegación si la colección existe: account.Contrasenia.Add(pwd);
                    db.Contrasenia.Add(pwd);

                    db.SaveChanges();
                    tx.Commit();
                    return user.IdUsuario;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    throw new InvalidOperationException("Could not create user.", ex);
                }
            }
        }

        public (int userId, string passwordHash, string displayName)? GetAuthByEmail(string email)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                // Trae la cuenta + navegación a usuario y contraseñas (sin joins explícitos).
                var account = db.Cuenta
                    .Include(c => c.Usuario)
                    .Include(c => c.Contrasenia)     // si tu nav es plural/otro nombre, ajústalo
                    .SingleOrDefault(c => c.Correo == email);

                if (account == null) return null;

                // Último hash por fecha
                var lastPwd = account.Contrasenia
                                .OrderByDescending(p => p.FechaCreacion)
                                .FirstOrDefault();

                if (lastPwd == null) return null;

                var userId = account.Usuario != null
                    ? account.Usuario.IdUsuario
                    : account.UsuarioIdUsuario; // fallback por FK directa

                var display = account.Usuario != null
                    ? account.Usuario.NombreUsuario
                    : null;

                return (userId, lastPwd.Contrasenia1, display);
            }
        }
    }
}
