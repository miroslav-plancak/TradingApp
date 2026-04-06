using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using TradingApp.Business.Constants;
using TradingApp.Business.Interfaces.Logger;
namespace TradingApp.Business.Logger
{
    public class TradingAppLogger : ITradingAppLogger
    {
        private readonly ILogger<TradingAppLogger>  _logger;
        private static readonly AsyncLocal<Dictionary<string, object>> _scopeData = new();
        private Dictionary<string, object> Scope 
        {
            get
            {
                if(_scopeData.Value == null)
                {
                    _scopeData.Value = new Dictionary<string, object>();
                }
                return _scopeData.Value;
            }
        }

        public TradingAppLogger(ILogger<TradingAppLogger> logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope()
        {
            return _logger.BeginScope("TradingAppLoggerScope");
        }

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, message);
            }

            _logger.LogError(message);
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)  
        {
           _logger.LogWarning(message);
        }

        public void SetClassScope(string className)
        {
            SetScopeValue(LoggingConstants.ClassNameScope, className);
        }

        public void SetControllerScope(string controllerName)
        {
            SetScopeValue(LoggingConstants.ControllerName, controllerName);
        }

        public void SetMethodScope(string methodName)
        {
            SetScopeValue(LoggingConstants.MethodNameScope, methodName);
        }

        //Note: unused for now, might need it in the future
        private string GetScopeValue(string key)
        {
            if(Scope.TryGetValue(key, out var value))
            {
                return value?.ToString();   
            }
            return null;
        }
        
        private void SetScopeValue(string key, object value) 
        {
            Scope[key] = value;
        }
    }
}
