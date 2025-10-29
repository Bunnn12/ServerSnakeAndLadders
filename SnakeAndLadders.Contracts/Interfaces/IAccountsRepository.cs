using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAccountsRepository
    {
        bool EmailExists(string email);
        bool UserNameExists(string userName);
        int CreateUserWithAccountAndPassword(CreateAccountRequestDto createAccountRequest);
        (int userId, string passwordHash, string displayName)? GetAuthByIdentifier(string identifier);
    }
}
