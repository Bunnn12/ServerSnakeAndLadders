using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAuthAppService
    {
        AuthResult RegisterUser(RegistrationDto registration);

        AuthResult Login(LoginDto request);

        AuthResult RequestEmailVerification(string email);

        AuthResult ConfirmEmailVerification(string email, string code);

        int GetUserIdFromToken(string token);

        AuthResult ChangePassword(ChangePasswordRequestDto request);

        AuthResult RequestPasswordChangeCode(string email);

        AuthResult Logout(LogoutRequestDto request);
    }
}
