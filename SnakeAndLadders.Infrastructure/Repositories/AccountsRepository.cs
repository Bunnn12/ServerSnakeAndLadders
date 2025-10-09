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

        public (int userId, string passwordHash, string displayName)? GetAuthByIdentifier(string identifier)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                // Busca por correo o por nombre de usuario (relación Cuenta -> Usuario)
                var account = db.Cuenta
                    .Include(c => c.Usuario)
                    .Include(c => c.Contrasenia)
                    .SingleOrDefault(c => c.Correo == identifier
                                       || c.Usuario.NombreUsuario == identifier);

                if (account == null) return null;

                var lastPwd = account.Contrasenia
                    .OrderByDescending(p => p.FechaCreacion)
                    .FirstOrDefault();

                if (lastPwd == null) return null;

                var userId = account.Usuario?.IdUsuario ?? account.UsuarioIdUsuario;
                var display = account.Usuario?.NombreUsuario;

                return (userId, lastPwd.Contrasenia1, display);
            }
        }
    }
}
