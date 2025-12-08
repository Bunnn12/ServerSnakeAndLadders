using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IAuthService
    {
        [OperationContract]
        AuthResult Register(RegistrationDto registration);

        [OperationContract]
        AuthResult Login(LoginDto request);

        [OperationContract]
        AuthResult RequestEmailVerification(string email);

        [OperationContract]
        AuthResult ConfirmEmailVerification(string email, string code);

        [OperationContract]
        AuthResult ChangePassword(ChangePasswordRequestDto request);

        [OperationContract]
        AuthResult RequestPasswordChangeCode(string email);

        [OperationContract]
        AuthResult Logout(LogoutRequestDto request);
    }
}
