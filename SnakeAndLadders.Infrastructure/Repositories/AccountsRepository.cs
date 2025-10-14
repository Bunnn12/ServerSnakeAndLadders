using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using ServerSnakesAndLadders;

namespace SnakeAndLadders.Infrastructure.Repositories
{
    public class AccountsRepository : IAccountsRepository
    {
        public bool EmailExists(string email)
        {
            email = (email ?? "").Trim();
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                // Log de conexión (para ver instancia y DB)
                var cn = db.Database.Connection;
                Trace.WriteLine($"[EmailExists] DS={cn.DataSource} DB={cn.Database} email={email}");

                // Timeout corto para no colgarte
                ((IObjectContextAdapter)db).ObjectContext.CommandTimeout = 10;

                // Consulta directa (sin ToLower/Trim SQL para evitar traducción rara)
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

        public int CreateUserWithAccountAndPassword(
            string userName, string firstName, string lastName, string email, string passwordHash)
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
                    db.SaveChanges(); // genera IdUsuario

                    var account = new Cuenta
                    {
                        // usa tu FK real si la tienes (p.ej. UsuarioIdUsuario = user.IdUsuario)
                        UsuarioIdUsuario = user.IdUsuario,
                        Correo = email,
                        Estado = new byte[] { 1 }
                    };
                    db.Cuenta.Add(account);
                    db.SaveChanges(); // genera IdCuenta

                    var pwd = new Contrasenia
                    {
                        UsuarioIdUsuario = user.IdUsuario,   // FK a USUARIO
                        Contrasenia1 = passwordHash,
                        Estado = new byte[] { 1 },
                        FechaCreacion = DateTime.UtcNow,

                        // *** CLAVE: vincular a la cuenta requerida ***
                        Cuenta = account           // <-- esto satisface FK_Contrasenia_Cuenta
                    };
                    db.Contrasenia.Add(pwd);

                    db.SaveChanges();
                    tx.Commit();
                    return user.IdUsuario;
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    tx.Rollback();
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                        throw new InvalidOperationException("Correo o usuario ya existe (índice único).", sqlEx);
                    if (sqlEx.Number == 1205)
                        throw new InvalidOperationException("Deadlock creando usuario. Reintenta.", sqlEx);

                    throw new InvalidOperationException("DB error creando usuario: " + sqlEx.Message, sqlEx);
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
                // Busca por correo
                var cuenta = db.Cuenta.AsNoTracking()
                    .Where(c => c.Correo == identifier)
                    .Select(c => new { c.UsuarioIdUsuario })
                    .FirstOrDefault();

                // Si no, busca por username
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
