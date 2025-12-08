using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public interface IVerificationCodeStore
    {
        bool TryGet(string email, out VerificationCodeEntry entry);
        void SaveNewCode(string email, string code, DateTime nowUtc);
        void Remove(string email);
        VerificationCodeEntry RegisterFailedAttempt(string email, VerificationCodeEntry currentEntry);
    }
}
