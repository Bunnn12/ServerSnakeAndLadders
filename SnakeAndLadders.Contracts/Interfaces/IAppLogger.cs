﻿using System;

namespace SnakeAndLadders.Contracts.Interfaces
{
    
    public interface IAppLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception ex);
    }
}
