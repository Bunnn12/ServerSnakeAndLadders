using log4net;
using SnakeAndLadders.Contracts.Interfaces;
using System;

namespace SnakesAndLadders.Host.Helpers
{
    /// <summary>Bridges log4net ILog to the IAppLogger abstraction.</summary>
    public sealed class AppLogger : IAppLogger
    {
        private readonly ILog log;
        public AppLogger(ILog log) => this.log = log ?? throw new ArgumentNullException(nameof(log));

        public void Info(string message) => log.Info(message);
        public void Warn(string message) => log.Warn(message);
        public void Error(string message, Exception ex) => log.Error(message, ex);
    }
}
