using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract(CallbackContract = typeof(IChatClient))]
    public interface IChatService
    {
        [OperationContract] SendMessageResponse2 SendMessage(SendMessageRequest2 request);
        [OperationContract] IList<ChatMessageDto> GetRecent(int lobbyId, int take);
        [OperationContract] void Subscribe(int lobbyId, int userId);
        [OperationContract(IsOneWay = true)] void Unsubscribe(int lobbyId, int userId);
    }
}
