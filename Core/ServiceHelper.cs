using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BlockchainNetworkAnalyzer.Core
{
    /// <summary>
    /// Helper class to provide global access to common services
    /// This avoids circular dependencies and provides static access to loggers and configuration
    /// </summary>
    public static class ServiceHelper
    {
        private static ILoggerFactory _loggerFactory;
        private static IConfiguration _configuration;

/// <summary>
        /// Initialize the service helper with logger factory and configuration
    /// Call this once during application startup
   /// </summary>
        public static void Initialize(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

 /// <summary>
        /// Get a logger for the specified type
     /// </summary>
   public static ILogger<T> GetLogger<T>()
        {
  if (_loggerFactory == null)
  {
          throw new InvalidOperationException("ServiceHelper has not been initialized. Call Initialize() during application startup.");
            }
  return _loggerFactory.CreateLogger<T>();
        }

  /// <summary>
        /// Get configuration section value
        /// </summary>
        public static string GetConfiguration(string key)
        {
            if (_configuration == null)
            {
          throw new InvalidOperationException("ServiceHelper has not been initialized. Call Initialize() during application startup.");
            }
       return _configuration[key] ?? "";
      }

 /// <summary>
     /// Get configuration with default value
/// </summary>
        public static string GetConfiguration(string key, string defaultValue)
 {
      if (_configuration == null)
 {
        throw new InvalidOperationException("ServiceHelper has not been initialized. Call Initialize() during application startup.");
 }
    return _configuration[key] ?? defaultValue;
        }
    }
}
