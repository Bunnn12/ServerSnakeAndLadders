using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public interface IVerificationAuthService
    {
        AuthResult RequestEmailVerification(string email);

        AuthResult ConfirmEmailVerification(string email, string code);

        AuthResult RequestPasswordChangeCode(string email);
    }
}
