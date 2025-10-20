﻿namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string plainText);
        bool Verify(string plainText, string hash);
    }
}
