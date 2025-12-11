using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IAccountIdentityRepository
    {
        bool IsEmailRegistered(string email);

        bool IsUserNameTaken(string userName);

        OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier);
    }
}
