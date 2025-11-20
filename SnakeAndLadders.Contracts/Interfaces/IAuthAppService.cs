using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAuthAppService
    {
        AuthResult RegisterUser(RegistrationDto request);
        AuthResult Login(LoginDto request);
        AuthResult RequestEmailVerification(string email);
        AuthResult ConfirmEmailVerification(string email, string code);
        int GetUserIdFromToken(string token);
    }
}
