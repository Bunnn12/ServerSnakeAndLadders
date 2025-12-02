using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IChatRepository
    {
        void SaveMessage(int lobbyId, ChatMessageDto message);
        IList<ChatMessageDto> ReadLast(int lobbyId, int take);
    }
}
