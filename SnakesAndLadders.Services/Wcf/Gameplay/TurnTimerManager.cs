using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Concurrent;
using System.Timers;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    public sealed class TurnTimerManager
    {
        private const int TURN_TIME_SECONDS = 30;
        private const int TIMER_INTERVAL_MILLISECONDS = 1000;

        private readonly ConcurrentDictionary<int, TurnTimerState> _turnTimersByGameId =
            new ConcurrentDictionary<int, TurnTimerState>();

        private readonly ConcurrentDictionary<int, Timer> _timersByGameId =
            new ConcurrentDictionary<int, Timer>();

        private readonly ILog _logger;

        public event Action<int> TurnTimedOut;
        public event Action<TurnTimerUpdateDto> TimerUpdated;

        public TurnTimerManager(ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void StartOrResetTurnTimer(int gameId, int currentTurnUserId)
        {
            if (gameId <= 0 || currentTurnUserId <= 0)
            {
                return;
            }

            TurnTimerState state = _turnTimersByGameId.AddOrUpdate(
                gameId,
                _ => new TurnTimerState(gameId, currentTurnUserId, TURN_TIME_SECONDS),
                (_, existing) =>
                {
                    existing.CurrentTurnUserId = currentTurnUserId;
                    existing.RemainingSeconds = TURN_TIME_SECONDS;
                    existing.LastUpdatedUtc = DateTime.UtcNow;
                    return existing;
                });

            Timer timer = _timersByGameId.GetOrAdd(
                gameId,
                _ =>
                {
                    var newTimer = new Timer(TIMER_INTERVAL_MILLISECONDS);
                    newTimer.AutoReset = true;
                    newTimer.Elapsed += (s, e) => OnServerTurnTimerTick(gameId);
                    return newTimer;
                });

            if (!timer.Enabled)
            {
                timer.Start();
            }

            TimerUpdated?.Invoke(
                new TurnTimerUpdateDto
                {
                    GameId = gameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    RemainingSeconds = state.RemainingSeconds
                });
        }

        public void StopTurnTimer(int gameId)
        {
            _turnTimersByGameId.TryRemove(gameId, out _);

            if (!_timersByGameId.TryRemove(gameId, out Timer timer))
            {
                return;
            }

            try
            {
                timer.Stop();
                timer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Warn("Error while stopping turn timer.", ex);
            }
        }

        public int GetRemainingSecondsOrDefault(int gameId, int currentTurnUserId)
        {
            if (!_turnTimersByGameId.TryGetValue(gameId, out TurnTimerState state))
            {
                return 0;
            }

            if (state.CurrentTurnUserId != currentTurnUserId)
            {
                return 0;
            }

            return state.RemainingSeconds;
        }

        private void OnServerTurnTimerTick(int gameId)
        {
            if (!_turnTimersByGameId.TryGetValue(gameId, out TurnTimerState state))
            {
                return;
            }

            int newRemaining = state.RemainingSeconds - 1;
            state.RemainingSeconds = newRemaining;
            state.LastUpdatedUtc = DateTime.UtcNow;

            if (newRemaining <= 0)
            {
                StopTurnTimer(gameId);
                TurnTimedOut?.Invoke(gameId);
                return;
            }

            TimerUpdated?.Invoke(
                new TurnTimerUpdateDto
                {
                    GameId = gameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    RemainingSeconds = newRemaining
                });
        }
    }
}
