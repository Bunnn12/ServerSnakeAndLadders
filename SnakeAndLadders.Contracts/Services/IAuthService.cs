using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IAuthService
    {
        [OperationContract]
        AuthResult Register(RegistrationDto request);

        [OperationContract]
        AuthResult Login(LoginDto request);
    }
}
