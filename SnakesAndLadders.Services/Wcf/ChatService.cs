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

        private const string LOG_CONTEXT_SEND = "ChatService.SendMessage";
        private const string LOG_CONTEXT_GET_RECENT = "ChatService.GetRecent";

        private readonly ChatAppService chatAppService;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>> subscribedClients =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>>();

        public ChatService(ChatAppService chatAppServiceValue)
        {
            chatAppService = chatAppServiceValue
                             ?? throw new ArgumentNullException(nameof(chatAppServiceValue));
        }

        public SendMessageResponse2 SendMessage(SendMessageRequest2 request)
        {
            if (!IsValidRequest(request))
            {
                return new SendMessageResponse2
                {
                    Ok = false
                };
            }

            try
            {
                chatAppService.Send(request.LobbyId, request.Message);
                BroadcastMessageToSubscribers(request.LobbyId, request.Message);

                return new SendMessageResponse2
                {
                    Ok = true
                };
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_CONTEXT_SEND + " failed.", ex);

                return new SendMessageResponse2
                {
                    Ok = false
                };
            }
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            try
            {
                IList<ChatMessageDto> messages = chatAppService.GetRecent(lobbyId, take);

                return messages ?? new List<ChatMessageDto>(0);
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_CONTEXT_GET_RECENT + " failed.", ex);
                return new List<ChatMessageDto>(0);
            }
        }

        public void Subscribe(int lobbyId, int userId)
        {
            IChatClient callbackChannel = TryGetCallbackChannel();
            if (callbackChannel == null)
            {
                return;
            }

            ConcurrentDictionary<int, IChatClient> lobbySubscribers = subscribedClients.GetOrAdd(
                lobbyId,
                _ => new ConcurrentDictionary<int, IChatClient>());

            lobbySubscribers[userId] = callbackChannel;

            IContextChannel communicationChannel = OperationContext.Current.Channel;
            communicationChannel.Closed += (_, __) => RemoveSubscriber(lobbyId, userId);
            communicationChannel.Faulted += (_, __) => RemoveSubscriber(lobbyId, userId);
        }

        public void Unsubscribe(int lobbyId, int userId)
        {
            RemoveSubscriber(lobbyId, userId);
        }

        private static bool IsValidRequest(SendMessageRequest2 request)
        {
            if (request == null)
            {
                return false;
            }

            if (request.Message == null)
            {
                return false;
            }

            if (request.LobbyId <= 0)
            {
                return false;
            }

            return true;
        }

        private static IChatClient TryGetCallbackChannel()
        {
            if (OperationContext.Current == null)
            {
                return null;
            }

            return OperationContext.Current.GetCallbackChannel<IChatClient>();
        }

        private void RemoveSubscriber(int lobbyId, int userId)
        {
            if (!subscribedClients.TryGetValue(lobbyId, out ConcurrentDictionary<int, IChatClient> lobbySubscribers))
            {
                return;
            }

            lobbySubscribers.TryRemove(userId, out _);

            if (lobbySubscribers.IsEmpty)
            {
                subscribedClients.TryRemove(lobbyId, out _);
            }
        }

        private void BroadcastMessageToSubscribers(int lobbyId, ChatMessageDto message)
        {
            if (!subscribedClients.TryGetValue(lobbyId, out ConcurrentDictionary<int, IChatClient> lobbySubscribers))
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
                        "Communication error sending chat message to user {0} in lobby {1}. Exception: {2}",
                        subscriber.Key,
                        lobbyId,
                        ex);
                }
                catch (TimeoutException ex)
                {
                    Logger.WarnFormat(
                        "Timeout sending chat message to user {0} in lobby {1}. Exception: {2}",
                        subscriber.Key,
                        lobbyId,
                        ex);
                }
            }
        }
    }
}
