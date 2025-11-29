using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class PersistPasswordResult
    {
        public bool IsSuccess { get; set; }
        public AuthResult Error { get; set; }
    }
}
