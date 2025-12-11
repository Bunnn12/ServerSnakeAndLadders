using SnakeAndLadders.Contracts.Dtos;
using System;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface ILobbyRepository
    {
        bool CodeExists(string code);

        CreatedGameInfo CreateGame(
            CreateLobbyRequestDto createLobbyRequest);

        void AddUserToGame(int gameId, int userId, bool isHost);

        void UpdateGameStatus(int gameId, LobbyStatus newStatus);
        bool IsUserHost(int lobbyId, int userId);
        bool IsUserInLobby(int lobbyId, int userId);
        void RemoveUserFromLobby(int lobbyId, int userId);
    }

    public sealed class CreatedGameInfo
    {
        public int PartidaId { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }


}
