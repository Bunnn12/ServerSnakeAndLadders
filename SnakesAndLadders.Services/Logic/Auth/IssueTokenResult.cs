using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    internal sealed class IssueTokenResult
    {
        public bool IsSuccess { get; set; }

        public string Token { get; set; }

        public AuthResult Error { get; set; }
    }
}
