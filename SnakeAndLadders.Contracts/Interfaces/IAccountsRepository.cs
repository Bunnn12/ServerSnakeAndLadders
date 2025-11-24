using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAccountsRepository
    {
        bool IsEmailRegistered(string email);
        bool IsUserNameTaken(string userName);

        OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto createAccountRequest);
        OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier);
    }
}
