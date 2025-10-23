using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                     ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class ChatService : IChatService
    {
        private readonly ChatAppService chatAppService;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>> subscribedClients =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>>();

        public ChatService(ChatAppService chatAppService) => this.chatAppService = chatAppService;

        public SendMessageResponse2 SendMessage(SendMessageRequest2 request)
        {
            if (request == null || request.Message == null)
                return new SendMessageResponse2 { Ok = false };

            chatAppService.Send(request.LobbyId, request.Message);
            BroadcastMessageToSubscribers(request.LobbyId, request.Message);
            return new SendMessageResponse2 { Ok = true };
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take) =>
            chatAppService.GetRecent(lobbyId, take);

        public void Subscribe(int lobbyId, int userId)
        {
            var clientCallbackChannel = OperationContext.Current?.GetCallbackChannel<IChatClient>();
            if (clientCallbackChannel == null) return;

            var lobbySubscribers = subscribedClients.GetOrAdd(
                lobbyId,
                _ => new ConcurrentDictionary<int, IChatClient>()
            );

            lobbySubscribers[userId] = clientCallbackChannel;

            var communicationChannel = OperationContext.Current.Channel;
            communicationChannel.Closed += (_, __) => RemoveSubscriber(lobbyId, userId);
            communicationChannel.Faulted += (_, __) => RemoveSubscriber(lobbyId, userId);
        }

        public void Unsubscribe(int lobbyId, int userId) => RemoveSubscriber(lobbyId, userId);

        private void RemoveSubscriber(int lobbyId, int userId)
        {
            if (subscribedClients.TryGetValue(lobbyId, out var lobbySubscribers))
            {
                lobbySubscribers.TryRemove(userId, out _);
                if (lobbySubscribers.IsEmpty) subscribedClients.TryRemove(lobbyId, out _);
            }
        }

        private void BroadcastMessageToSubscribers(int lobbyId, ChatMessageDto message)
        {
            if (!subscribedClients.TryGetValue(lobbyId, out var lobbySubscribers)) return;

            foreach (var kvp in lobbySubscribers.ToArray())
            {
                try
                {
                    kvp.Value.OnMessage(lobbyId, message);
                }
                catch (CommunicationException ex)  
                {
                    Console.WriteLine($"Error de comunicación con usuario {kvp.Key}: {ex.Message}");
                }
                catch (TimeoutException ex)  
                {
                    Console.WriteLine($"Timeout al enviar mensaje al usuario {kvp.Key}: {ex.Message}");
                }

            }
        }
    }
}
