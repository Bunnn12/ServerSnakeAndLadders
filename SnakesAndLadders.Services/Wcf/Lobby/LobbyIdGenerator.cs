using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakesAndLadders.Services.Constants;


namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public sealed class LobbyIdGenerator : ILobbyIdGenerator
    {
        private readonly Random _random = new Random();

        public int GenerateLobbyId()
        {
            lock (_random)
            {
                return _random.Next(
                    LobbyServiceConstants.MIN_RANDOM_LOBBY_ID,
                    LobbyServiceConstants.MAX_RANDOM_LOBBY_ID);
            }
        }

        public string GenerateLobbyCode()
        {
            lock (_random)
            {
                int numericCode = _random.Next(
                    LobbyServiceConstants.MIN_RANDOM_CODE,
                    LobbyServiceConstants.MAX_RANDOM_CODE);

                return numericCode.ToString("000000");
            }
        }
    }
}
