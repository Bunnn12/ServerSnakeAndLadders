using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
   
    public interface ILobbyAppService
    {
        CreateGameResponse CreateGame(CreateGameRequest request);

        void RegisterPlayerInGame(int gameId, int userId, bool isHost);
    }
}
