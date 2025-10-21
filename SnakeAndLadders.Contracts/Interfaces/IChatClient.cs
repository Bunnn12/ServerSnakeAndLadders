using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    public interface IChatClient
    {
        [OperationContract(IsOneWay = true)]
        void OnMessage(int lobbyId, ChatMessageDto message);
    }
}
