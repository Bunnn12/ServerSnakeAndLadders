using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System.ServiceModel;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IGameplayService
    {
        [OperationContract]
        RollDiceResponseDto RollDice(RollDiceRequestDto request);

        
        [OperationContract]
        GetGameStateResponseDto GetGameState(GetGameStateRequestDto request);
    }
}
