using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAccountsRepository
    {
        bool IsEmailRegistered(string email);
        bool IsUserNameTaken(string userName);
        int CreateUserWithAccountAndPassword(CreateAccountRequestDto createAccountRequest);
        AuthCredentialsDto GetAuthByIdentifier(string identifier);
    }
}
