using System.Collections.Generic;
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

        OperationResult<IReadOnlyList<string>> GetLastPasswordHashes(int userId, int maxCount);

        OperationResult<bool> AddPasswordHash(int userId, string passwordHash);

        OperationResult<string> GetEmailByUserId(int userId);
    }
}
