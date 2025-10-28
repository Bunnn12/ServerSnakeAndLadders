using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class LobbyRepository : ILobbyRepository
    {
        public bool CodeExists(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                return ctx.Partida.AsNoTracking().Any(p => p.CodigoPartida == code);
            }
        }

        public CreatedGameInfo CreateGame(
    int hostUserId,
    byte maxPlayers,
    string dificultad,
    string code,
    DateTime expiresAtUtc)
        {
            try
            {
                using (var ctx = new SnakeAndLaddersDBEntities1())
                {
                    // 1) ¿A qué base estás pegando?
                    Console.WriteLine("CS=" + ctx.Database.Connection.ConnectionString);

                    // 2) Log de SQL que ejecuta EF
                    ctx.Database.Log = s => Debug.WriteLine(s);

                    var partida = new Partida
                    {
                        IdPartida = hostUserId,
                        Dificultad = string.IsNullOrWhiteSpace(dificultad) ? null : dificultad,
                        CodigoPartida = code,           // OJO: en tu BD es CHAR(6); se rellena con espacios
                        FechaInicio = null,
                        FechaTermino = null,
                        fechaCreacion = DateTime.UtcNow,
                        expiraEn = expiresAtUtc,
                        EstadoPartida = (byte)1      // tinyint NOT NULL
                        
                        
                    };

                    ctx.Partida.Add(partida);
                    var rows1 = ctx.SaveChanges();
                    Console.WriteLine($"Save#1 rows={rows1}  NewId={partida.IdPartida}");

                    // 3) Inserta relación host
                    var host = new UsuarioHasPartida
                    {
                        UsuarioIdUsuario = hostUserId,
                        PartidaIdPartida = partida.IdPartida,
                        esHost = true,
                        Ganador = null
                    };

                    ctx.UsuarioHasPartida.Add(host);
                    var rows2 = ctx.SaveChanges();
                    Console.WriteLine($"Save#2 rows={rows2}");

                    // 4) Round-trip: confirma que la Partida existe (usa TRIM por CHAR(6))
                    bool inserted = ctx.Partida.AsNoTracking()
                                       .Any(p => p.IdPartida == partida.IdPartida
                                              && p.CodigoPartida.Trim() == code);
                    Console.WriteLine("Inserted? " + inserted);

                    return new CreatedGameInfo
                    {
                        PartidaId = partida.IdPartida,
                        Code = partida.CodigoPartida,
                        ExpiresAtUtc = partida.expiraEn ?? expiresAtUtc
                    };
                }
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                Console.WriteLine("DbUpdateException: " + ex);
                throw;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var e in ex.EntityValidationErrors)
                    foreach (var ve in e.ValidationErrors)
                        Console.WriteLine($"{e.Entry.Entity.GetType().Name}.{ve.PropertyName}: {ve.ErrorMessage}");
                throw;
            }
        }
    }
}
