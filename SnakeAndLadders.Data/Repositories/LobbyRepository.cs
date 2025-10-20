using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;

namespace SnakesAndLadders.Data.Repositories
{
    /// <summary>EF6 repository that persists Partida and host membership.</summary>
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
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var partida = new Partida
                {

                    Dificultad = string.IsNullOrWhiteSpace(dificultad) ? null : dificultad,
                    CodigoPartida = code,
                    EstadoPartida = 1,               // Waiting
                    FechaInicio = null,
                    FechaTermino = null,
                    fechaCreacion = DateTime.UtcNow, // BD también lo setea por DEFAULT; duplicado no estorba
                    expiraEn = expiresAtUtc
                };

                ctx.Partida.Add(partida);
                ctx.SaveChanges(); // auto identity

                var host = new UsuarioHasPartida
                {
                    UsuarioIdUsuario = hostUserId,
                    PartidaIdPartida = partida.IdPartida,
                    esHost = true,
                    Ganador = null
                };

                ctx.UsuarioHasPartida.Add(host);
                ctx.SaveChanges();

                return new CreatedGameInfo
                {
                    PartidaId = partida.IdPartida,
                    Code = partida.CodigoPartida,
                    ExpiresAtUtc = partida.expiraEn ?? expiresAtUtc
                };
            }
        }
    }
}
