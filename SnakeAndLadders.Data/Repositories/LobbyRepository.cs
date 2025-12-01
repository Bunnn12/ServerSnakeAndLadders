using System;
using System.Diagnostics;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class LobbyRepository : ILobbyRepository
    {
        private const byte GAME_STATE_WAITING = 1;

        public bool CodeExists(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            using (var context = new SnakeAndLaddersDBEntities1())
            {
                return context.Partida
                    .AsNoTracking()
                    .Any(partida => partida.CodigoPartida == code);
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
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    context.Database.Log = message => Debug.WriteLine(message);

                    string safeDifficulty = string.IsNullOrWhiteSpace(dificultad)
                        ? "Normal"
                        : dificultad.Trim();

                    var partida = new Partida
                    {
                        // IdPartida lo genera la BD (IDENTITY)
                        Dificultad = safeDifficulty,
                        CodigoPartida = code,
                        FechaInicio = null,
                        FechaTermino = null,
                        fechaCreacion = DateTime.UtcNow,
                        expiraEn = expiresAtUtc,
                        EstadoPartida = GAME_STATE_WAITING
                    };

                    context.Partida.Add(partida);
                    context.SaveChanges();

                    // Si quieres persistir que el host está ligado a la partida:
                    var hostLink = new UsuarioHasPartida
                    {
                        UsuarioIdUsuario = hostUserId,
                        PartidaIdPartida = partida.IdPartida,
                        esHost = true,
                        Ganador = null
                    };

                    context.UsuarioHasPartida.Add(hostLink);
                    context.SaveChanges();

                    return new CreatedGameInfo
                    {
                        PartidaId = partida.IdPartida,
                        Code = partida.CodigoPartida,
                        ExpiresAtUtc = partida.expiraEn ?? expiresAtUtc
                    };
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var entityValidationError in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationError.ValidationErrors)
                    {
                        Debug.WriteLine(
                            "{0}.{1}: {2}",
                            entityValidationError.Entry.Entity.GetType().Name,
                            validationError.PropertyName,
                            validationError.ErrorMessage);
                    }
                }

                throw;
            }
        }

        public void UpdateGameStatus(int gameId, LobbyStatus newStatus)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            using (var context = new SnakeAndLaddersDBEntities1())
            {
                var partida = context.Partida
                    .SingleOrDefault(p => p.IdPartida == gameId);

                if (partida == null)
                {
                    return;
                }

                partida.EstadoPartida = (byte)newStatus;
                context.SaveChanges();
            }
        }

        public void AddUserToGame(int gameId, int userId, bool isHost)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = new SnakeAndLaddersDBEntities1())
            {
                var link = new UsuarioHasPartida
                {
                    UsuarioIdUsuario = userId,
                    PartidaIdPartida = gameId,
                    esHost = isHost,
                    Ganador = null
                };

                context.UsuarioHasPartida.Add(link);
                context.SaveChanges();
            }
        }
    }
}
