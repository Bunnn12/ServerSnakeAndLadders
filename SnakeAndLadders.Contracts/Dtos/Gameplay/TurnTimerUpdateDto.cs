namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class TurnTimerUpdateDto
    {
        public int GameId { get; set; }
        public int CurrentTurnUserId { get; set; }
        public int RemainingSeconds { get; set; }
    }
}
