using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract(CallbackContract = typeof(ILobbyCallback))]
    public interface ILobbyService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        CreateGameResponse CreateGame(CreateGameRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        JoinLobbyResponse JoinLobby(JoinLobbyRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        OperationResult LeaveLobby(LeaveLobbyRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        OperationResult StartMatch(StartMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        LobbyInfo GetLobbyInfo(GetLobbyInfoRequest request);

        [OperationContract(IsOneWay = true)]
        void SubscribePublicLobbies(int userId);

        [OperationContract(IsOneWay = true)]
        void UnsubscribePublicLobbies(int userId);
    }
}
