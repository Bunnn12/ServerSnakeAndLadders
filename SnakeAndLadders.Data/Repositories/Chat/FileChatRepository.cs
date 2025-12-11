using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Data.Interfaces;
using SnakesAndLadders.Data.Repositories.Chat;
using System;
using System.Collections.Generic;
using System.IO;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class FileChatRepository : IChatRepository
    {
        private readonly ChatFilePathProvider _pathProvider;
        private readonly IChatFileStore _fileStore;
        private readonly IChatMessageSerializer _serializer;
        private readonly IChatMessageReader _messageReader;

        public FileChatRepository(string templatePathOrDir)
        {
            _pathProvider = new ChatFilePathProvider(templatePathOrDir);
            _serializer = new ChatMessageSerializer();
            _fileStore = new ChatFileStore();
            _messageReader = new ChatMessageReader(_serializer);
        }

        public void SaveMessage(int lobbyId, ChatMessageDto message)
        {
            ChatMessageValidator.ValidateLobbyId(lobbyId);
            ChatMessageValidator.ValidateMessage(message);

            string filePath = _pathProvider.GetLobbyFilePath(lobbyId);
            string serializedLine = _serializer.Serialize(message);

            _fileStore.AppendLine(filePath, serializedLine);
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            ChatMessageValidator.ValidateLobbyId(lobbyId);

            int effectiveTake = GetEffectiveTake(take);

            string filePath = _pathProvider.GetLobbyFilePath(lobbyId);

            if (!File.Exists(filePath))
            {
                return new List<ChatMessageDto>(0);
            }

            string[] lines = _fileStore.ReadAllLines(filePath);
            IList<ChatMessageDto> lastMessages = _messageReader.ReadLastMessages(lines, effectiveTake);

            return lastMessages;
        }

        private static int GetEffectiveTake(int take)
        {
            if (take <= 0)
            {
                return ChatRepositoryConstants.DEFAULT_TAKE_MESSAGES;
            }

            return take;
        }
    }
}
