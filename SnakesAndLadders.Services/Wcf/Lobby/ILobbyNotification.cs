using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public interface ILobbyNotification
    {
        void RegisterLobbyCallback(int lobbyId, int userId, ILobbyCallback callback);

        void RemoveLobbyCallback(int lobbyId, int userId);

        void NotifyLobbyUpdated(LobbyInfo lobby);

        void NotifyLobbyClosed(int lobbyId, string reason);

        void NotifyKickedFromLobby(int lobbyId, int userId, string reason);

        void SubscribePublicLobbies(int userId, ILobbyCallback callback);

        void UnsubscribePublicLobbies(int userId);

        void BroadcastPublicLobbies(IList<LobbySummary> summaries);
    }
}
