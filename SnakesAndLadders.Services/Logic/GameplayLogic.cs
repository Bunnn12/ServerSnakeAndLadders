using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;

namespace SnakesAndLadders.Services.Logic
{
 
    public sealed class GameplayLogic
    {
        private const int DICE_MIN_VALUE = 1;
        private const int DICE_MAX_VALUE = 6;

        private readonly BoardDefinitionDto board;
        private readonly List<int> turnOrder;
        private readonly Dictionary<int, int> positionsByUserId;
        private readonly object syncRoot = new object();
        private readonly Random random;

        private int currentTurnIndex;
        private bool isFinished;

        public GameplayLogic(BoardDefinitionDto board, IEnumerable<int> playerUserIds)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (playerUserIds == null)
            {
                throw new ArgumentNullException(nameof(playerUserIds));
            }

            this.board = board;

            turnOrder = playerUserIds
                .Distinct()
                .Where(id => id > 0)
                .ToList();

            if (turnOrder.Count == 0)
            {
                throw new InvalidOperationException("GameplayLogic requires at least one player.");
            }

            positionsByUserId = turnOrder.ToDictionary(id => id, _ => 0);
            currentTurnIndex = 0;
            isFinished = false;

            random = new Random(unchecked((int)DateTime.UtcNow.Ticks));
        }

        public RollDiceResult RollDice(int userId)
        {
            lock (syncRoot)
            {
                if (isFinished)
                {
                    throw new InvalidOperationException("The game has already finished.");
                }

                if (!turnOrder.Contains(userId))
                {
                    throw new InvalidOperationException("User is not part of this game.");
                }

                int currentTurnUserId = turnOrder[currentTurnIndex];
                if (currentTurnUserId != userId)
                {
                    throw new InvalidOperationException("It is not this user's turn.");
                }

                int diceValue = random.Next(DICE_MIN_VALUE, DICE_MAX_VALUE + 1);

                int fromCellIndex = positionsByUserId[userId];
                int toCellIndex = fromCellIndex + diceValue;

                positionsByUserId[userId] = toCellIndex;

                
                bool isGameOver = false;

                
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

                return new RollDiceResult
                {
                    DiceValue = diceValue,
                    FromCellIndex = fromCellIndex,
                    ToCellIndex = toCellIndex,
                    IsGameOver = isGameOver,
                    ExtraInfo = string.Empty
                };
            }
        }

        public GameStateSnapshot GetCurrentState()
        {
            lock (syncRoot)
            {
                int currentTurnUserId = turnOrder[currentTurnIndex];

                var tokens = positionsByUserId
                    .Select(pair => new TokenState
                    {
                        UserId = pair.Key,
                        CellIndex = pair.Value
                    })
                    .ToList();

                return new GameStateSnapshot
                {
                    CurrentTurnUserId = currentTurnUserId,
                    IsFinished = isFinished,
                    Tokens = tokens
                };
            }
        }
    }

    public sealed class RollDiceResult
    {
        public int DiceValue { get; set; }

        public int FromCellIndex { get; set; }

        public int ToCellIndex { get; set; }

        public bool IsGameOver { get; set; }

        public string ExtraInfo { get; set; }
    }

    public sealed class GameStateSnapshot
    {
        public int CurrentTurnUserId { get; set; }

        public bool IsFinished { get; set; }

        public IReadOnlyCollection<TokenState> Tokens { get; set; }
    }

    public sealed class TokenState
    {
        public int UserId { get; set; }

        public int CellIndex { get; set; }
    }
}
