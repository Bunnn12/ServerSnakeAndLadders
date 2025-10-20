namespace SnakeAndLadders.Contracts.Dtos
{
    
    public sealed class CreateGameRequest
    {
        public int HostUserId { get; set; }
        public byte MaxPlayers { get; set; } = 2;   
        public string Dificultad { get; set; }      
        public int TtlMinutes { get; set; } = 30;   
    }
}
