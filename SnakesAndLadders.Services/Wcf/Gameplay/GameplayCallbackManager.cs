using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    public sealed class GameplayCallbackManager
    {
        private const int INVALID_USER_ID = 0;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>> _callbacksByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> _userNamesByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, string>>();

        public void RegisterCallback(
            int gameId,
            int userId,
            string userName,
            IGameplayCallback callbackChannel)
        {
            var callbacksForGame = _callbacksByGameId.GetOrAdd(
                gameId,
                _ => new ConcurrentDictionary<int, IGameplayCallback>());

            callbacksForGame[userId] = callbackChannel;

            var userNamesForGame = _userNamesByGameId.GetOrAdd(
                gameId,
                _ => new ConcurrentDictionary<int, string>());

            string effectiveUserName = string.IsNullOrWhiteSpace(userName)
                ? $"User {userId}"
                : userName;

            userNamesForGame[userId] = effectiveUserName;
        }

        public bool TryRemoveCallback(int gameId, int userId)
        {
            if (!_callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return false;
            }

            return callbacksForGame.TryRemove(userId, out _);
        }

        public string GetUserNameOrDefault(int gameId, int userId)
        {
            if (!_userNamesByGameId.TryGetValue(gameId, out var usersForGame))
            {
                return $"User {userId}";
            }

            if (!usersForGame.TryGetValue(userId, out string userName))
            {
                return $"User {userId}";
            }

            return userName;
        }

        public IReadOnlyCollection<int> GetActiveUserIds(int gameId)
        {
            if (!_callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return Array.Empty<int>();
            }

            return callbacksForGame.Keys
                .Where(id => id != INVALID_USER_ID)
                .ToList()
                .AsReadOnly();
        }

        public void CleanupIfEmpty(int gameId)
        {
            if (!_callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return;
            }

            if (!callbacksForGame.IsEmpty)
            {
                return;
            }

            _callbacksByGameId.TryRemove(gameId, out _);
            _userNamesByGameId.TryRemove(gameId, out _);
        }

        public void NotifyPlayerMoved(int gameId, PlayerMoveResultDto move, Action<int, Exception> onFailure)
        {
            if (!_callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    gameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnPlayerMoved(move),
                    onFailure);
            }
        }

        public void NotifyTurnChanged(TurnChangedDto turnInfo, Action<int, Exception> onFailure)
        {
            if (!_callbacksByGameId.TryGetValue(turnInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    turnInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnTurnChanged(turnInfo),
                    onFailure);
            }
        }

        public void NotifyPlayerLeft(PlayerLeftDto playerLeftInfo, Action<int, Exception> onFailure)
        {
            if (!_callbacksByGameId.TryGetValue(playerLeftInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    playerLeftInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnPlayerLeft(playerLeftInfo),
                    onFailure);
            }
        }

        public void NotifyItemUsed(int gameId, ItemUsedNotificationDto notification, Action<int, Exception> onFailure)
        {
            if (!_callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    gameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnItemUsed(notification),
                    onFailure);
            }
        }

        public void NotifyTurnTimerUpdated(TurnTimerUpdateDto timerInfo, Action<int, Exception> onFailure)
        {
            if (timerInfo == null)
            {
                return;
            }

            if (!_callbacksByGameId.TryGetValue(timerInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    timerInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnTurnTimerUpdated(timerInfo),
                    onFailure);
            }
        }

        private void InvokeCallbackSafely(
            int gameId,
            int userId,
            IGameplayCallback callback,
            Action<IGameplayCallback> callbackInvoker,
            Action<int, Exception> onFailure)
        {
            try
            {
                callbackInvoker(callback);
            }
            catch (Exception ex)
            {
                onFailure?.Invoke(userId, ex);
            }
        }
    }
}
