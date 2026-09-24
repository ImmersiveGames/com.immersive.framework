using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Logging.Loggers;
using LoggingCoreLogger = Immersive.Logging.Loggers.Logger;
using Immersive.Logging.Records;
using Immersive.Logging.Unity;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Framework logging adapter; not game-facing API.")]
    internal sealed class FrameworkLogger
    {
        private const string Category = "Immersive.Framework";

        private static LoggingCoreLogger _sharedLogger;
        private static LoggingConfigAsset _sharedConfig;

        private readonly ScopedLogger _logger;

        private FrameworkLogger(ScopedLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _sharedLogger = null;
            _sharedConfig = null;
        }

        internal static FrameworkLogger Create()
        {
            return Create(typeof(FrameworkLogger));
        }

        internal static FrameworkLogger Create<T>()
        {
            return Create(typeof(T));
        }

        internal static FrameworkLogger Create(Type ownerType)
        {
            if (ownerType == null)
            {
                throw new ArgumentNullException(nameof(ownerType));
            }

            LoggingCoreLogger logger = GetSharedLogger();
            return new FrameworkLogger(logger.For(ownerType, Category));
        }

        internal void Trace(string message, params LogField[] fields)
        {
            _logger.Trace(message, fields);
        }

        internal void Debug(string message, params LogField[] fields)
        {
            _logger.Debug(message, fields);
        }

        internal void Info(string message, params LogField[] fields)
        {
            _logger.Info(message, fields);
        }

        internal void Warning(string message, params LogField[] fields)
        {
            _logger.Warning(message, fields);
        }

        internal void Error(string message, params LogField[] fields)
        {
            _logger.Error(message, fields);
        }

        internal void Error(string message, Exception exception, params LogField[] fields)
        {
            _logger.Error(message, exception, fields);
        }

        private static LoggingCoreLogger GetSharedLogger()
        {
            LoggingConfigAsset config = ResolveLoggingConfig();
            if (_sharedLogger != null && ReferenceEquals(_sharedConfig, config))
            {
                return _sharedLogger;
            }

            _sharedConfig = config;
            _sharedLogger = UnityLoggingFactory.CreateLogger(config);
            return _sharedLogger;
        }

        private static LoggingConfigAsset ResolveLoggingConfig()
        {
            var settings = Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath);
            return settings != null ? settings.LoggingConfig : null;
        }
    }
}
