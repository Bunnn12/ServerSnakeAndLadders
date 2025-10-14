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

        [OperationContract]
        AuthResult RequestEmailVerification(string email);

        [OperationContract]
        AuthResult ConfirmEmailVerification(string email, string code);
    }
}
