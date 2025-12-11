using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public interface ILobbyStore
    {
        void AddOrUpdateLobby(LobbyInfo lobby);

        bool TryGetLobby(int lobbyId, out LobbyInfo lobby);

        bool TryFindByCode(string code, out LobbyInfo lobby);

        bool RemoveLobby(int lobbyId);

        IReadOnlyCollection<LobbyInfo> GetAll();

        LobbyInfo CloneLobby(LobbyInfo lobby);
    }
}
