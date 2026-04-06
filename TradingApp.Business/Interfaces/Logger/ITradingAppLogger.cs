using System;

namespace TradingApp.Business.Interfaces.Logger
{
    public interface ITradingAppLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception ex = null);
        void SetClassScope(string className);
        void SetControllerScope(string controllerName);
        void SetMethodScope(string methodName);
        IDisposable BeginScope();
    }
}
