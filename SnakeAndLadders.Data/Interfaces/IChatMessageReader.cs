using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    internal interface IChatMessageReader
    {
        IList<ChatMessageDto> ReadLastMessages(string[] lines, int take);
    }
}
