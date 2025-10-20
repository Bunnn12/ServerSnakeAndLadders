using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IChatRepository
    {
        void Append(ChatMessageDto message);
        IList<ChatMessageDto> ReadLast(int take);
    }
}
