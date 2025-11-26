using System;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface ILobbyRepository
    {
        bool CodeExists(string code);

        CreatedGameInfo CreateGame(
            int hostUserId,
            byte maxPlayers,
            string dificultad,
            string code,
            DateTime expiresAtUtc);

        void AddUserToGame(int gameId, int userId, bool isHost);
    }

    public sealed class CreatedGameInfo
    {
        public int PartidaId { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
