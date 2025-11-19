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

        // Orden de turnos y posiciones actuales
        private readonly List<int> turnOrder;
        private readonly Dictionary<int, int> positionsByUserId;

        // Mapa de serpientes/escaleras: índice origen -> índice destino
        private readonly Dictionary<int, int> jumpDestinationsByStartIndex;

        // Índice de la casilla final (meta)
        private readonly int finalCellIndex;

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

            
            finalCellIndex = ResolveFinalCellIndex(board);

            
            jumpDestinationsByStartIndex = ResolveJumpMap(board);
        }

        private static int ResolveFinalCellIndex(BoardDefinitionDto board)
        {
            if (board == null || board.Cells == null || board.Cells.Count == 0)
            {
                return 0;
            }

            
            return board.Cells.Max(c => c.Index);
        }

        private static Dictionary<int, int> ResolveJumpMap(BoardDefinitionDto board)
        {
            var result = new Dictionary<int, int>();

            if (board == null || board.Links == null || board.Links.Count == 0)
            {
                return result;
            }

            foreach (BoardLinkDto link in board.Links)
            {
                int from = link.StartIndex;
                int to = link.EndIndex;

                if (from > 0 && to > 0 && from != to)
                {
                    // Si hubiera duplicados, el último link gana
                    result[from] = to;
                }
            }

            return result;
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

                // Posición tentativa sin aplicar serpientes/escaleras
                int tentativeTarget = fromCellIndex + diceValue;
                int finalTarget = fromCellIndex;
                string extraInfo = string.Empty;

                // 1) Regla de tiro exacto para llegar a la meta
                if (finalCellIndex > 0 && tentativeTarget > finalCellIndex)
                {
                    // Se pasa: no se mueve, solo pierde el turno
                    finalTarget = fromCellIndex;
                    extraInfo = "RollTooHigh_NoMove";

                    // Avanzar el turno aunque no se haya movido
                    currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

                    return new RollDiceResult
                    {
                        DiceValue = diceValue,
                        FromCellIndex = fromCellIndex,
                        ToCellIndex = finalTarget,
                        IsGameOver = isFinished,
                        ExtraInfo = extraInfo
                    };
                }

                // Si no se pasó, por ahora aterriza en la casilla tentativa
                finalTarget = tentativeTarget;

                // 2) Aplicar serpientes/escaleras si hay link en esa casilla
                if (jumpDestinationsByStartIndex.TryGetValue(tentativeTarget, out int jumpDestination))
                {
                    finalTarget = jumpDestination;

                    if (jumpDestination > tentativeTarget)
                    {
                        extraInfo = "Ladder";
                    }
                    else if (jumpDestination < tentativeTarget)
                    {
                        extraInfo = "Snake";
                    }
                    else
                    {
                        extraInfo = "JumpButSameIndex";
                    }
                }

                // 3) Actualizar posición real del jugador
                positionsByUserId[userId] = finalTarget;

                // 4) ¿Ganó?
                bool isGameOver = false;
                if (finalCellIndex > 0 && finalTarget >= finalCellIndex)
                {
                    isFinished = true;
                    isGameOver = true;

                    extraInfo = string.IsNullOrWhiteSpace(extraInfo)
                        ? "Win"
                        : extraInfo + "_Win";
                }

                // 5) Pasar el turno al siguiente (aunque alguien haya ganado,
                // isFinished evita que vuelvan a tirar)
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

                return new RollDiceResult
                {
                    DiceValue = diceValue,
                    FromCellIndex = fromCellIndex,
                    ToCellIndex = finalTarget,
                    IsGameOver = isGameOver,
                    ExtraInfo = extraInfo
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
