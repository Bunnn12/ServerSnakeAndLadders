using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Chat
{
    internal sealed class ChatMessageSerializer : IChatMessageSerializer
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatMessageSerializer()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.Latin1Supplement),
                WriteIndented = false
            };
        }

        public string Serialize(ChatMessageDto message)
        {
            return JsonSerializer.Serialize(message, _jsonOptions);
        }

        public bool TryDeserialize(string jsonLine, out ChatMessageDto message)
        {
            message = new ChatMessageDto();

            try
            {
                ChatMessageDto deserialized = JsonSerializer.Deserialize<ChatMessageDto>(
                    jsonLine,
                    _jsonOptions);

                if (deserialized == null)
                {
                    return false;
                }

                message = deserialized;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
