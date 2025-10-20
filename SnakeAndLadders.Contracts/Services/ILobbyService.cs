using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using System.ServiceModel;

namespace SnakeAndLadders.Contracts.Services
{
    
    [ServiceContract]
    public interface ILobbyService
    {
        [FaultContract(typeof(ServiceFault))]
        [OperationContract]
        CreateGameResponse CreateGame(CreateGameRequest request);
    }
}
