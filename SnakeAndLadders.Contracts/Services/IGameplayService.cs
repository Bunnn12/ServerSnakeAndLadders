using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos.Gameplay;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract(CallbackContract = typeof(IGameplayCallback))]
    public interface IGameplayService
    {
        [OperationContract]
        RollDiceResponseDto RollDice(RollDiceRequestDto request);

        [OperationContract]
        GetGameStateResponseDto GetGameState(GetGameStateRequestDto request);

        [OperationContract]
        UseItemResponseDto UseItem(UseItemRequestDto request);

        [OperationContract]
        void JoinGame(int gameId, int userId, string userName);

        [OperationContract]
        void LeaveGame(int gameId, int userId, string reason);

        [OperationContract]
        void RegisterTurnTimeout(int gameId);


    }
}
