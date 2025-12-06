using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos.Gameplay;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IGameplayCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPlayerMoved(PlayerMoveResultDto move);

        [OperationContract(IsOneWay = true)]
        void OnTurnChanged(TurnChangedDto turnInfo);

        [OperationContract(IsOneWay = true)]
        void OnItemUsed(ItemUsedNotificationDto notification);

        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(PlayerLeftDto playerLeftInfo);

        [OperationContract(IsOneWay = true)]
        void OnTurnTimerUpdated(TurnTimerUpdateDto timerInfo);
    }
}
