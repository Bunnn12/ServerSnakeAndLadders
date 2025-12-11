using System;
using System.Security.Cryptography;

namespace SnakesAndLadders.Services.Logic.Auth
{
    internal static class VerificationCodeGenerator
    {
        private const int RandomBytesLength = 4;
        private const int FirstByteIndex = 0;
        private const int DecimalBase = 10;
        private const char VerificationCodePadChar = '0';

        internal static string GenerateCode(int digits)
        {
            byte[] bytes = new byte[RandomBytesLength];

            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(bytes);
            }

            uint value = BitConverter.ToUInt32(bytes, FirstByteIndex);
            uint mod = (uint)Math.Pow(DecimalBase, digits);
            uint number = value % mod;

            return number.ToString(new string(VerificationCodePadChar, digits));
        }
    }
}
