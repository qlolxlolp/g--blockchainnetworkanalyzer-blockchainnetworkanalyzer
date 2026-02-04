using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockchainNetworkAnalyzer
{
    public partial class App : Application
    {
        public static IConfiguration? Configuration { get; private set; }
        public static ILoggerFactory? LoggerFactory { get; private set; }
        public static ILogger<App>? Logger { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // تنظیم مدیریت خطاهای غیرمنتظره
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // بارگذاری تنظیمات
                LoadConfiguration();

                // ایجاد دایرکتوری‌های مورد نیاز
                CreateRequiredDirectories();

                // راه‌اندازی لاگینگ
                InitializeLogging();

                Logger?.LogInformation("Application started successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطا در راه‌اندازی برنامه:\n{ex.Message}",
                    "خطای راه‌اندازی",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "config.default.json"), optional: true, reloadOnChange: true);
            Configuration = builder.Build();
        }

        private void CreateRequiredDirectories()
        {
            string[] directories = { "Data", "Data\\Backups", "Logs", "Reports", "Exports", "Config" };

            foreach (var dir in directories)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private void InitializeLogging()
        {
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            Logger = LoggerFactory.CreateLogger<App>();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger?.LogError(e.Exception, "Unhandled dispatcher exception");

            MessageBox.Show(
                $"خطای غیرمنتظره:\n{e.Exception.Message}\n\nبرنامه ممکن است به درستی کار نکند.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger?.LogCritical(ex, "Unhandled domain exception");

                MessageBox.Show(
                    $"خطای بحرانی:\n{ex.Message}\n\nبرنامه بسته خواهد شد.",
                    "خطای بحرانی",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Environment.Exit(1);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger?.LogInformation("Application shutting down");
            base.OnExit(e);
        }
    }
}