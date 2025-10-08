
using System.ServiceModel;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        int AddUser(string username, string nombre, string apellidos);
    }
}
