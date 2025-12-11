using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class CreateLobbyRequestDto
    {
        public int HostUserId { get; set; }

        public byte MaxPlayers { get; set; }

        public string Difficulty { get; set; }

        public string Code { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }
}
