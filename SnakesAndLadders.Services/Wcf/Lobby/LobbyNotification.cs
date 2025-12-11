using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public sealed class LobbyNotification : ILobbyNotification
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(LobbyNotification));

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ILobbyCallback>>
            _lobbyCallbacks =
                new ConcurrentDictionary<int, ConcurrentDictionary<int, ILobbyCallback>>();

        private readonly ConcurrentDictionary<int, ILobbyCallback> _publicLobbySubscribers =
            new ConcurrentDictionary<int, ILobbyCallback>();

        private readonly ILobbyStore _lobbyStore;

        public LobbyNotification(ILobbyStore lobbyStore)
        {
            _lobbyStore = lobbyStore ?? throw new ArgumentNullException(nameof(lobbyStore));
        }

        public void RegisterLobbyCallback(int lobbyId, int userId, ILobbyCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks =
                _lobbyCallbacks.GetOrAdd(
                    lobbyId,
                    id => new ConcurrentDictionary<int, ILobbyCallback>());

            perLobbyCallbacks[userId] = callback;
        }

        public void RemoveLobbyCallback(int lobbyId, int userId)
        {
            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks;

            if (!_lobbyCallbacks.TryGetValue(lobbyId, out perLobbyCallbacks))
            {
                return;
            }

            perLobbyCallbacks.TryRemove(userId, out _);

            if (perLobbyCallbacks.IsEmpty)
            {
                _lobbyCallbacks.TryRemove(lobbyId, out _);
            }
        }

        public void NotifyLobbyUpdated(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                return;
            }

            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks;

            if (!_lobbyCallbacks.TryGetValue(lobby.PartidaId, out perLobbyCallbacks))
            {
                return;
            }

            LobbyInfo snapshot = _lobbyStore.CloneLobby(lobby);

            foreach (KeyValuePair<int, ILobbyCallback> entry in perLobbyCallbacks.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    callback => callback.OnLobbyUpdated(snapshot));
            }
        }

        public void NotifyLobbyClosed(int lobbyId, string reason)
        {
            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks;

            if (!_lobbyCallbacks.TryGetValue(lobbyId, out perLobbyCallbacks))
            {
                return;
            }

            foreach (KeyValuePair<int, ILobbyCallback> entry in perLobbyCallbacks.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    callback => callback.OnLobbyClosed(lobbyId, reason));
            }

            _lobbyCallbacks.TryRemove(lobbyId, out _);
        }

        public void NotifyKickedFromLobby(int lobbyId, int userId, string reason)
        {
            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks;

            if (!_lobbyCallbacks.TryGetValue(lobbyId, out perLobbyCallbacks))
            {
                return;
            }

            ILobbyCallback callback;

            if (!perLobbyCallbacks.TryGetValue(userId, out callback))
            {
                return;
            }

            SafeInvoke(
                callback,
                cb => cb.OnKickedFromLobby(lobbyId, reason));
        }

        public void SubscribePublicLobbies(int userId, ILobbyCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            _publicLobbySubscribers[userId] = callback;
        }

        public void UnsubscribePublicLobbies(int userId)
        {
            _publicLobbySubscribers.TryRemove(userId, out _);
        }

        public void BroadcastPublicLobbies(IList<LobbySummary> summaries)
        {
            foreach (KeyValuePair<int, ILobbyCallback> entry in _publicLobbySubscribers.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    callback => callback.OnPublicLobbiesChanged(summaries));
            }
        }

        private static void SafeInvoke(
            ILobbyCallback callback,
            Action<ILobbyCallback> invoker)
        {
            try
            {
                invoker(callback);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error invoking lobby callback.", ex);
            }
        }
    }
}
