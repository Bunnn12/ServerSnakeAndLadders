using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    using global::SnakesAndLadders.Services.Logic;
    using global::SnakesAndLadders.Services.Logic.Gameplay;
    using SnakeAndLadders.Contracts.Dtos.Gameplay;
    using SnakeAndLadders.Contracts.Interfaces;
    using System;
    using System.Collections.Concurrent;

    namespace SnakesAndLadders.Services.Logic.Gameplay
    {
        public sealed class GameplayAppService : IGameplayAppService
        {
            private const string ERROR_SESSION_NOT_FOUND =
                "Game session not found for the specified gameId.";

            private const string ERROR_DICE_CODE_REQUIRED =
                "diceCode cannot be null or whitespace.";

            private const string ERROR_ITEM_CODE_REQUIRED =
                "itemCode cannot be null or whitespace.";

            private const string ERROR_INVALID_GAME_ID =
                "gameId must be greater than zero.";

            private const string ERROR_INVALID_USER_ID =
                "userId must be greater than zero.";

            private readonly IGameSessionStore _gameSessionStore;

            private readonly ConcurrentDictionary<int, GameplayLogic> _gameplayByGameId =
                new ConcurrentDictionary<int, GameplayLogic>();

            public GameplayAppService(IGameSessionStore gameSessionStore)
            {
                _gameSessionStore = gameSessionStore
                    ?? throw new ArgumentNullException(nameof(gameSessionStore));
            }

            public RollDiceResult RollDice(
                int gameId,
                int userId,
                string diceCode)
            {
                ValidateGameId(gameId);
                ValidateUserId(userId);

                GameplayLogic logic = GetOrCreateGameplayLogic(gameId);

                return logic.RollDice(userId, diceCode);
            }

            public GameStateSnapshot GetCurrentState(int gameId)
            {
                ValidateGameId(gameId);

                GameplayLogic logic = GetOrCreateGameplayLogic(gameId);

                return logic.GetCurrentState();
            }

            public ItemEffectResult UseItem(
                int gameId,
                int userId,
                string itemCode,
                int? targetUserId)
            {
                ValidateGameId(gameId);
                ValidateUserId(userId);

                if (string.IsNullOrWhiteSpace(itemCode))
                {
                    throw new ArgumentNullException(nameof(itemCode), ERROR_ITEM_CODE_REQUIRED);
                }

                GameplayLogic logic = GetOrCreateGameplayLogic(gameId);

                return logic.UseItem(userId, itemCode, targetUserId);
            }

            public TurnTimeoutResult HandleTurnTimeout(int gameId)
            {
                ValidateGameId(gameId);

                GameplayLogic logic = GetOrCreateGameplayLogic(gameId);

                return logic.HandleTurnTimeout();
            }

            private GameplayLogic GetOrCreateGameplayLogic(int gameId)
            {
                if (!_gameSessionStore.TryGetSession(gameId, out GameSession session))
                {
                    throw new InvalidOperationException(ERROR_SESSION_NOT_FOUND);
                }

                return _gameplayByGameId.GetOrAdd(
                    session.GameId,
                    _ => new GameplayLogic(
                        session.Board,
                        session.PlayerUserIds));
            }

            private static void ValidateGameId(int gameId)
            {
                if (gameId <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(gameId),
                        ERROR_INVALID_GAME_ID);
                }
            }

            private static void ValidateUserId(int userId)
            {
                if (userId <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(userId),
                        ERROR_INVALID_USER_ID);
                }
            }
        }
    }

}
