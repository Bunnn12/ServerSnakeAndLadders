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
    // Single: mantiene las suscripciones en memoria
    // Multiple: permite concurrencia para múltiples clientes
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                     ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class ChatService : IChatService
    {
        private readonly ChatAppService app;

        // lobbyId -> (userId -> callback)
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>> subs =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IChatClient>>();

        public ChatService(ChatAppService app) => this.app = app;

        public SendMessageResponse2 SendMessage(SendMessageRequest2 request)
        {
            if (request == null || request.Message == null)
                return new SendMessageResponse2 { Ok = false };

            // TODO: validar AuthToken => resolver Sender/Id del servidor si deseas mayor seguridad

            app.Send(request.LobbyId, request.Message);
            Broadcast(request.LobbyId, request.Message);
            return new SendMessageResponse2 { Ok = true };
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take) =>
            app.GetRecent(lobbyId, take);

        public void Subscribe(int lobbyId, int userId)
        {
            var cb = OperationContext.Current?.GetCallbackChannel<IChatClient>();
            if (cb == null) return;

            var map = subs.GetOrAdd(lobbyId, _ => new ConcurrentDictionary<int, IChatClient>());
            map[userId] = cb;

            var ch = OperationContext.Current.Channel;
            ch.Closed += (_, __) => Remove(lobbyId, userId);
            ch.Faulted += (_, __) => Remove(lobbyId, userId);
        }

        public void Unsubscribe(int lobbyId, int userId) => Remove(lobbyId, userId);

        private void Remove(int lobbyId, int userId)
        {
            if (subs.TryGetValue(lobbyId, out var map))
            {
                map.TryRemove(userId, out _);
                if (map.IsEmpty) subs.TryRemove(lobbyId, out _);
            }
        }

        private void Broadcast(int lobbyId, ChatMessageDto message)
        {
            if (!subs.TryGetValue(lobbyId, out var map)) return;

            foreach (var kv in map.ToArray())
            {
                try { kv.Value.OnMessage(lobbyId, message); }
                catch { /* ignore bad client */ }
            }
        }
    }
}
