using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
   
    public interface ILobbyAppService
    {
        CreateGameResponse CreateGame(CreateGameRequest request);

        void RegisterHostInGame(int gameId, int userId);

        void RegisterPlayerInGame(int gameId, int userId);
        void KickPlayerFromLobby(int lobbyId, int hostUserId, int targetUserId);
    }
}
