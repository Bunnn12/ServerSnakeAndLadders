using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public interface ILobbyCleanupService
    {
        IReadOnlyCollection<LobbyInfo> CleanupExpiredLobbies();
    }
}
