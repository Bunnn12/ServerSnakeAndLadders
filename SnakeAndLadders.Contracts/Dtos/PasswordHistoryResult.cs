using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class PasswordHistoryResult
    {
        public bool IsValid { get; set; }
        public IReadOnlyList<string> History { get; set; }
        public AuthResult Error { get; set; }
    }
}
