using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Wcf.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

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

        public ChatService(ChatAppService chatAppServiceValue)
        {
            _chatAppService = chatAppServiceValue
                              ?? throw new ArgumentNullException(nameof(chatAppServiceValue));
        }

        public SendMessageResponse2 SendMessage(SendMessageRequest2 request)
        {
            if (!IsValidRequest(request))
            {
                return CreateSendMessageResponse(false);
            }

            try
            {
                _chatAppService.Send(request.LobbyId, request.Message);
                BroadcastMessageToSubscribers(request.LobbyId, request.Message);

                return CreateSendMessageResponse(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ChatServiceConstants.LOG_ERROR_SEND_FAILED, ex);
                return CreateSendMessageResponse(false);
            }
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            try
            {
                IList<ChatMessageDto> messages = _chatAppService.GetRecent(lobbyId, take);

                return messages ?? CreateEmptyMessages();
            }
            catch (Exception ex)
            {
                Logger.Error(ChatServiceConstants.LOG_ERROR_GET_RECENT_FAILED, ex);
                return CreateEmptyMessages();
            }
        }

        public void Subscribe(int lobbyId, int userId)
        {
            IChatClient callbackChannel = TryGetCallbackChannel();
            if (callbackChannel == null)
            {
                Logger.Warn(ChatServiceConstants.LOG_WARN_NO_CALLBACK_CHANNEL);
                return;
            }

            ConcurrentDictionary<int, IChatClient> lobbySubscribers = _subscribedClients.GetOrAdd(
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

        private static SendMessageResponse2 CreateSendMessageResponse(bool ok)
        {
            return new SendMessageResponse2
            {
                Ok = ok
            };
        }

        private static IList<ChatMessageDto> CreateEmptyMessages()
        {
            return new List<ChatMessageDto>(ChatServiceConstants.ZERO_MESSAGES_CAPACITY);
        }

        private void RemoveSubscriber(int lobbyId, int userId)
        {
            if (!_subscribedClients.TryGetValue(lobbyId, out ConcurrentDictionary<int, IChatClient> lobbySubscribers))
            {
                return;
            }

            lobbySubscribers.TryRemove(userId, out _);

            if (lobbySubscribers.IsEmpty)
            {
                _subscribedClients.TryRemove(lobbyId, out _);
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
                        ChatServiceConstants.LOG_WARN_COMMUNICATION_ERROR_FORMAT,
                        subscriber.Key,
                        lobbyId,
                        ex);

                    RemoveSubscriber(lobbyId, subscriber.Key);
                }
                catch (TimeoutException ex)
                {
                    Logger.WarnFormat(
                        ChatServiceConstants.LOG_WARN_TIMEOUT_ERROR_FORMAT,
                        subscriber.Key,
                        lobbyId,
                        ex);

                    RemoveSubscriber(lobbyId, subscriber.Key);
                }
            }
        }
    }
}
