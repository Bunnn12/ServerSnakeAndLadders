using System;

namespace SnakeAndLadders.Contracts.Dtos
{
    
    public sealed class CreateGameResponse
    {
        public int PartidaId { get; set; }
        public string CodigoPartida { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
