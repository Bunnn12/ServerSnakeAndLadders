using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class LoginResultDto
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string Token { get; set; }
    }
}
