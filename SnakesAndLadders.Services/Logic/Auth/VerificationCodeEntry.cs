using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class VerificationCodeEntry
    {
        public string Code { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public DateTime LastSentUtc { get; set; }
        public int FailedAttempts { get; set; }
    }
}
