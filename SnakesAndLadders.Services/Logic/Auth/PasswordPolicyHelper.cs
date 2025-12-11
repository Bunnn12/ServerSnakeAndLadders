using System.Collections.Generic;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic.Auth
{
    internal static class PasswordPolicyHelper
    {
        private const int PasswordMinLength = 8;
        private const int PasswordMaxLength = 64;

        internal const int PasswordHistoryLimit = 3;

        internal static bool IsPasswordFormatValid(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            {
                return false;
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;

            for (int index = 0; index < password.Length; index++)
            {
                char character = password[index];

                if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
            }

            return hasUpper && hasLower && hasDigit;
        }

        internal static bool IsPasswordReused(
            string newPassword,
            IReadOnlyList<string> passwordHistory,
            IPasswordHasher passwordHasher)
        {
            if (passwordHistory == null || passwordHistory.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < passwordHistory.Count; index++)
            {
                string oldHash = passwordHistory[index];

                if (string.IsNullOrWhiteSpace(oldHash))
                {
                    continue;
                }

                if (passwordHasher.Verify(newPassword, oldHash))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
