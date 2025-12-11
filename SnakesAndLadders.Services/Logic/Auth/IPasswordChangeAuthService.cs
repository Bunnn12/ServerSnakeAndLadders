using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public interface IPasswordChangeAuthService
    {
        AuthResult ChangePassword(ChangePasswordRequestDto request);
    }
}
