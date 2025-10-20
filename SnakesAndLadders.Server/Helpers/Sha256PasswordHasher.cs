using System;
using System.Security.Cryptography;
using System.Text;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Host.Helpers
{
    public sealed class Sha256PasswordHasher : IPasswordHasher
    {
        public string Hash(string plainText)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainText ?? string.Empty));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        public bool Verify(string plainText, string hash) =>
            string.Equals(Hash(plainText), hash, StringComparison.Ordinal);

    }
}
