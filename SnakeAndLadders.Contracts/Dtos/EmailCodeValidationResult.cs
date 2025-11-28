using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class EmailCodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Email { get; set; }
        public AuthResult Error { get; set; }
    }
}
