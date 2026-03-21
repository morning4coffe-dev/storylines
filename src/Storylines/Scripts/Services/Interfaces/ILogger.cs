using System;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception ex = null);
    }
}
