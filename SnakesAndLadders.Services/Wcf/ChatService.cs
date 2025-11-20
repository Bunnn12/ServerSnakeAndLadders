using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class ChatService : IChatService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatService));

        private readonly ChatAppService _chatAppService;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>> _subscribedClients =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>>();

        public ChatService(ChatAppService chatAppService)
        {
            _chatAppService = chatAppService ?? throw new ArgumentNullException(nameof(chatAppService));
        }

        public SendMessageResponse2 SendMessage(SendMessageRequest2 request)
        {
            if (request == null || request.Message == null)
            {
                return new SendMessageResponse2 { Ok = false };
            }

            _chatAppService.Send(request.LobbyId, request.Message);
            BroadcastMessageToSubscribers(request.LobbyId, request.Message);

            return new SendMessageResponse2 { Ok = true };
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            return _chatAppService.GetRecent(lobbyId, take);
        }

        public void Subscribe(int lobbyId, int userId)
        {
            IChatClient clientCallbackChannel = OperationContext.Current?.GetCallbackChannel<IChatClient>();
            if (clientCallbackChannel == null)
            {
                return;
            }

            ConcurrentDictionary<int, IChatClient> lobbySubscribers = _subscribedClients.GetOrAdd(
                lobbyId,
                _ => new ConcurrentDictionary<int, IChatClient>());

            lobbySubscribers[userId] = clientCallbackChannel;

            IContextChannel communicationChannel = OperationContext.Current.Channel;
            communicationChannel.Closed += (_, __) => RemoveSubscriber(lobbyId, userId);
            communicationChannel.Faulted += (_, __) => RemoveSubscriber(lobbyId, userId);
        }

        public void Unsubscribe(int lobbyId, int userId)
        {
            RemoveSubscriber(lobbyId, userId);
        }

        private void RemoveSubscriber(int lobbyId, int userId)
        {
            if (_subscribedClients.TryGetValue(lobbyId, out ConcurrentDictionary<int, IChatClient> lobbySubscribers))
            {
                lobbySubscribers.TryRemove(userId, out _);

                if (lobbySubscribers.IsEmpty)
                {
                    _subscribedClients.TryRemove(lobbyId, out _);
                }
            }
        }

        private void BroadcastMessageToSubscribers(int lobbyId, ChatMessageDto message)
        {
            if (!_subscribedClients.TryGetValue(lobbyId, out ConcurrentDictionary<int, IChatClient> lobbySubscribers))
            {
                return;
            }

            foreach (KeyValuePair<int, IChatClient> subscriber in lobbySubscribers.ToArray())
            {
                try
                {
                    subscriber.Value.OnMessage(lobbyId, message);
                }
                catch (CommunicationException ex)
                {
                    Logger.WarnFormat(
                        "Communication error sending chat message to user {0} in lobby {1}.",
                        subscriber.Key,
                        lobbyId);
                    Logger.Warn("Communication exception details while sending chat message.", ex);
                }
                catch (TimeoutException ex)
                {
                    Logger.WarnFormat(
                        "Timeout sending chat message to user {0} in lobby {1}.",
                        subscriber.Key,
                        lobbyId);
                    Logger.Warn("Timeout exception details while sending chat message.", ex);
                }
            }
        }
    }
}
