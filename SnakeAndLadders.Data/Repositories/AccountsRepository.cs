using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace ServerSnakesAndLadders
{
    public class AccountsRepository : IAccountsRepository
    {
        public bool EmailExists(string email)
        {
            email = (email ?? "").Trim();
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var cn = db.Database.Connection;
                Trace.WriteLine($"[EmailExists] DS={cn.DataSource} DB={cn.Database} email={email}");
                ((IObjectContextAdapter)db).ObjectContext.CommandTimeout = 30;

                return db.Cuenta.AsNoTracking().Any(c => c.Correo == email);
            }
        }

        public bool UserNameExists(string userName)
        {
            userName = (userName ?? "").Trim();
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var cn = db.Database.Connection;
                Trace.WriteLine($"[UserNameExists] DS={cn.DataSource} DB={cn.Database} user={userName}");
                ((IObjectContextAdapter)db).ObjectContext.CommandTimeout = 10;

                return db.Usuario.AsNoTracking().Any(u => u.NombreUsuario == userName);
            }
        }

        public int CreateUserWithAccountAndPassword(string userName, string firstName, string lastName, string email, string passwordHash)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    var user = new Usuario
                    {
                        NombreUsuario = userName,
                        Nombre = firstName,
                        Apellidos = lastName,
                        Monedas = 0,
                        Estado = new byte[] { 1 }
                    };
                    db.Usuario.Add(user);
                    db.SaveChanges();

                    var account = new Cuenta
                    {
                        UsuarioIdUsuario = user.IdUsuario,
                        Correo = email,
                        Estado = new byte[] { 1 }
                    };
                    db.Cuenta.Add(account);
                    db.SaveChanges();

                    var pwd = new Contrasenia
                    {
                        UsuarioIdUsuario = user.IdUsuario,
                        Contrasenia1 = passwordHash,
                        Estado = new byte[] { 1 },
                        FechaCreacion = DateTime.UtcNow,
                        Cuenta = account
                    };
                    db.Contrasenia.Add(pwd);

                    db.SaveChanges();
                    tx.Commit();
                    return user.IdUsuario;
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    tx.Rollback();
                    // Deja que AuthService mapee por Number (2601/2627/1205/…)
                    throw;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public (int userId, string passwordHash, string displayName)? GetAuthByIdentifier(string identifier)
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
                    if (usuario == null) return null;
                    userId = usuario.IdUsuario;
                }

                var lastPwd = db.Contrasenia.AsNoTracking()
                    .Where(p => p.UsuarioIdUsuario == userId)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => p.Contrasenia1)
                    .FirstOrDefault();

                if (lastPwd == null) return null;

                var display = db.Usuario.AsNoTracking()
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => u.NombreUsuario)
                    .FirstOrDefault();

                return (userId, lastPwd, display);
            }
        }
    }
}
