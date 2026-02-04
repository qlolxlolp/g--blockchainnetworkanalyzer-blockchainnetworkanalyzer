using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BlockchainNetworkAnalyzer.Core.Console;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Views
{
    public partial class ConsoleWindow : Window
    {
        private readonly ILogger<ConsoleWindow> _logger;
        private InProcessConsole _console;
        private readonly StringBuilder _outputBuffer = new StringBuilder();

        public ConsoleWindow()
        {
            InitializeComponent();
            _logger = App.LoggerFactory.CreateLogger<ConsoleWindow>();
        }

        private async void StartConsoleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_console != null && _console.IsRunning)
            {
                _console.Dispose();
                _console = null;
                StartConsoleBtn.Content = "Start Console";
                AppendOutput("Console stopped.");
                return;
            }

            try
            {
                _console = new InProcessConsole();
                _console.OutputReceived += OnConsoleOutput;
                _console.ErrorReceived += OnConsoleError;
                _console.ProcessExited += OnConsoleExited;

                var started = await _console.StartAsync();
                if (started)
                {
                    StartConsoleBtn.Content = "Stop Console";
                    AppendOutput("Console started. Ready for commands.");
                }
                else
                {
                    AppendOutput("Failed to start console.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting console");
                AppendOutput($"Error: {ex.Message}");
            }
        }

        private void OnConsoleOutput(object sender, string output)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput(output);
            });
        }

        private void OnConsoleError(object sender, string error)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput($"[ERROR] {error}", System.Windows.Media.Brushes.Red);
            });
        }

        private void OnConsoleExited(object sender, int exitCode)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput($"\nProcess exited with code: {exitCode}");
                StartConsoleBtn.Content = "Start Console";
            });
        }

        private void AppendOutput(string text, System.Windows.Media.Brush color = null)
        {
            if (color == null)
                color = System.Windows.Media.Brushes.LimeGreen;

            _outputBuffer.Append(text);
            ConsoleOutput.Text = _outputBuffer.ToString();
            
            // Auto-scroll to bottom
            ConsoleScrollViewer.ScrollToEnd();
        }

        private async void SendCommandBtn_Click(object sender, RoutedEventArgs e)
        {
            await SendCommand();
        }

        private async void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendCommand();
            }
        }

        private async System.Threading.Tasks.Task SendCommand()
        {
            var command = CommandInput.Text.Trim();
            if (string.IsNullOrEmpty(command))
                return;

            if (_console == null || !_console.IsRunning)
            {
                AppendOutput("Console is not running. Please start the console first.");
                return;
            }

            AppendOutput($"\n> {command}\n");
            CommandInput.Clear();

            try
            {
                var output = await _console.ExecuteCommandAsync(command, 30000);
                AppendOutput(output);
            }
            catch (Exception ex)
            {
                AppendOutput($"Error executing command: {ex.Message}", System.Windows.Media.Brushes.Red);
            }
        }

        private void ClearConsoleBtn_Click(object sender, RoutedEventArgs e)
        {
            _outputBuffer.Clear();
            ConsoleOutput.Clear();
            if (_console != null)
            {
                _console.ClearBuffers();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _console?.Dispose();
            base.OnClosed(e);
        }
    }
}

