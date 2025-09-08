using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ecr.Module.Statics
{
    public static class GlobalExceptionHandler
    {
        private static bool _isHandlingException = false;

        public static void Initialize()
        {
            Application.ThreadException += OnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception, "!!! Unobserved Task Exception !!!");
            e.SetObserved();
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (_isHandlingException) return;
            _isHandlingException = true;

            try
            {
                HandleException(e.Exception, "!!! UI Thread !!!");
            }
            finally
            {
                _isHandlingException = false;
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject, "!!! Background Thread !!!");

            if (e.IsTerminating)
                ApplicationHelper.RestartApplication();
        }

        private static void HandleException(Exception ex, string threadType)
        {
            var logDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}\\EcrLog";

            var logDir = Path.Combine(logDirectory, $"log_{DateTime.Today:yyyyMMdd}_exceptions.txt");

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            File.WriteAllText(
                logDir,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {threadType} > Exception:\r\n{ex}\r\n");
        }
    }

}
