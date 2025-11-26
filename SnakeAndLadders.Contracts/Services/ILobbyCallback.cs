using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface ILobbyCallback
    {
        
        [OperationContract(IsOneWay = true)]
        void OnLobbyUpdated(LobbyInfo lobby);

        [OperationContract(IsOneWay = true)]
        void OnLobbyClosed(int partidaId, string reason);

        [OperationContract(IsOneWay = true)]
        void OnKickedFromLobby(int partidaId, string reason);

        [OperationContract(IsOneWay = true)]
        void OnPublicLobbiesChanged(IList<LobbySummary> lobbies);
    }
}
