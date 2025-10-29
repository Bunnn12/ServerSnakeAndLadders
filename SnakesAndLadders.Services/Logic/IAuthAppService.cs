using SnakeAndLadders.Contracts.Dtos;

namespace SnakesAndLadders.Services.Logic
{
    public interface IAuthAppService
    {
        AuthResult RegisterUser(RegistrationDto request);
        AuthResult Login(LoginDto request);
        AuthResult RequestEmailVerification(string email);
        AuthResult ConfirmEmailVerification(string email, string code);
    }
}
