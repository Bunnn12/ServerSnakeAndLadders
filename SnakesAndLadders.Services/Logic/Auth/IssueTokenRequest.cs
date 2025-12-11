using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    internal sealed class IssueTokenRequest
    {
        public int UserId { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }
}
