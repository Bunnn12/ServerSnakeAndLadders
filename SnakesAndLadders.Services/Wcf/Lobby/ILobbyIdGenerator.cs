using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public interface ILobbyIdGenerator
    {
        int GenerateLobbyId();
        string GenerateLobbyCode();
    }
}
