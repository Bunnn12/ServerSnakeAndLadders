using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract]
        SendMessageResponse SendMessage(SendMessageRequest request);

        [OperationContract]
        IList<ChatMessageDto> GetRecent(int take);
    }
}
