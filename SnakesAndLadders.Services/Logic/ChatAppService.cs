using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;


namespace SnakesAndLadders.Services.Logic
{
    public sealed class ChatAppService
    {
        private readonly IChatRepository repository;
        public ChatAppService(IChatRepository repository) => this.repository = repository;

        public void Send(int lobbyId, ChatMessageDto message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Text)) return;
            message.TimestampUtc = DateTime.UtcNow;
            message.Text = message.Text.Trim();
            if (string.IsNullOrEmpty(message.Text)) return;
            message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);
            repository.Append(lobbyId, message);
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            var effectiveTake = take <= 0 ? 100 : take;
            var messages = repository.ReadLast(lobbyId, effectiveTake);
            foreach (var m in messages)
            {
                m.SenderAvatarId = AvatarIdHelper.MapFromDb(m.SenderAvatarId);
            }

            return messages;
        }
    }
}
