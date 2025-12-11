using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Interfaces;

namespace SnakesAndLadders.Data.Repositories.Chat
{
    internal sealed class ChatMessageReader : IChatMessageReader
    {
        private readonly IChatMessageSerializer _serializer;

        public ChatMessageReader(IChatMessageSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IList<ChatMessageDto> ReadLastMessages(string[] lines, int take)
        {
            var result = new List<ChatMessageDto>();

            if (lines == null || lines.Length == 0)
            {
                return result;
            }

            foreach (string lineText in lines
                         .Reverse()
                         .Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                if (_serializer.TryDeserialize(lineText, out ChatMessageDto message))
                {
                    result.Add(message);
                }

                if (result.Count >= take)
                {
                    break;
                }
            }

            result.Reverse();
            return result;
        }
    }
}
