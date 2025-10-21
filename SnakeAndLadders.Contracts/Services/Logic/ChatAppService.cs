using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakeAndLadders.Services.Logic
{
    public sealed class ChatAppService
    {
        private readonly IChatRepository repository;

        public ChatAppService(IChatRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void Send(ChatMessageDto message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(message.Text)) return;

            message.TimestampUtc = DateTime.UtcNow;
            repository.Append(message);
        }

        public IList<ChatMessageDto> GetRecent(int take)
        {
            if (take <= 0) take = 100;
            return repository.ReadLast(take);
        }
    }
}
