using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Console
{
    /// <summary>
    /// In-process console that provides interactive terminal functionality
    /// </summary>
    public class InProcessConsole : IDisposable
    {
        private Process _consoleProcess;
        private readonly ConcurrentQueue<string> _inputQueue = new ConcurrentQueue<string>();
        private readonly StringBuilder _outputBuffer = new StringBuilder();
        private readonly StringBuilder _errorBuffer = new StringBuilder();
        private readonly ILogger<InProcessConsole> _logger;
        private readonly AutoResetEvent _inputEvent = new AutoResetEvent(false);
        private bool _isRunning;
        private Task _outputReaderTask;
        private Task _errorReaderTask;
        private Task _inputWriterTask;

        public event EventHandler<string> OutputReceived;
        public event EventHandler<string> ErrorReceived;
        public event EventHandler<int> ProcessExited;

        public bool IsRunning => _isRunning && _consoleProcess != null && !_consoleProcess.HasExited;
        public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;
        public StreamWriter InputWriter { get; private set; }

        public InProcessConsole()
        {
            _logger = App.LoggerFactory.CreateLogger<InProcessConsole>();
        }

        public async Task<bool> StartAsync(string shellPath = null)
        {
            try
            {
                if (IsRunning)
                {
                    return true;
                }

                // Use PowerShell or CMD
                if (string.IsNullOrEmpty(shellPath))
                {
                    shellPath = "powershell.exe";
                    if (!File.Exists(shellPath))
                    {
                        shellPath = "cmd.exe";
                    }
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = shellPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = CurrentDirectory
                };

                _consoleProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _consoleProcess.Exited += (s, e) =>
                {
                    _isRunning = false;
                    ProcessExited?.Invoke(this, _consoleProcess.ExitCode);
                };

                _consoleProcess.Start();

                InputWriter = _consoleProcess.StandardInput;
                
                _outputBuffer.Clear();
                _errorBuffer.Clear();

                _isRunning = true;

                // Start reading tasks
                _outputReaderTask = Task.Run(ReadOutputAsync);
                _errorReaderTask = Task.Run(ReadErrorAsync);
                _inputWriterTask = Task.Run(WriteInputAsync);

                _logger.LogInformation($"Console started: {shellPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start console");
                return false;
            }
        }

        private async Task ReadOutputAsync()
        {
            try
            {
                using var reader = _consoleProcess.StandardOutput;
                var buffer = new char[4096];

                while (_isRunning && !_consoleProcess.HasExited)
                {
                    var count = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        var output = new string(buffer, 0, count);
                        _outputBuffer.Append(output);
                        OutputReceived?.Invoke(this, output);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading console output");
            }
        }

        private async Task ReadErrorAsync()
        {
            try
            {
                using var reader = _consoleProcess.StandardError;
                var buffer = new char[4096];

                while (_isRunning && !_consoleProcess.HasExited)
                {
                    var count = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        var error = new string(buffer, 0, count);
                        _errorBuffer.Append(error);
                        ErrorReceived?.Invoke(this, error);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading console error");
            }
        }

        private async Task WriteInputAsync()
        {
            try
            {
                while (_isRunning && !_consoleProcess.HasExited)
                {
                    if (_inputQueue.TryDequeue(out var input))
                    {
                        if (InputWriter != null && !InputWriter.BaseStream.CanWrite)
                        {
                            await Task.Delay(100);
                            continue;
                        }

                        await InputWriter.WriteLineAsync(input);
                        await InputWriter.FlushAsync();
                        _logger.LogDebug($"Sent input to console: {input}");
                    }
                    else
                    {
                        _inputEvent.WaitOne(100);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing console input");
            }
        }

        public async Task<string> ExecuteCommandAsync(string command, int timeoutMs = 30000)
        {
            if (!IsRunning)
            {
                await StartAsync();
                await Task.Delay(500); // Wait for console to initialize
            }

            var outputBuilder = new StringBuilder();
            var outputReceived = false;
            var outputEvent = new AutoResetEvent(false);

            EventHandler<string> handler = (s, output) =>
            {
                outputBuilder.Append(output);
                outputReceived = true;
                outputEvent.Set();
            };

            OutputReceived += handler;

            try
            {
                SendCommand(command);

                // Wait for output or timeout
                if (outputEvent.WaitOne(timeoutMs))
                {
                    // Wait a bit more for complete output
                    await Task.Delay(500);
                    return outputBuilder.ToString();
                }
                else
                {
                    return "Command timed out";
                }
            }
            finally
            {
                OutputReceived -= handler;
            }
        }

        public void SendCommand(string command)
        {
            if (IsRunning)
            {
                _inputQueue.Enqueue(command);
                _inputEvent.Set();
            }
        }

        public void SendInput(string input)
        {
            SendCommand(input);
        }

        public string GetAllOutput()
        {
            return _outputBuffer.ToString();
        }

        public string GetAllError()
        {
            return _errorBuffer.ToString();
        }

        public void ClearBuffers()
        {
            _outputBuffer.Clear();
            _errorBuffer.Clear();
        }

        public async Task<bool> RequestAccessAsync(string toolName, string permission)
        {
            // Auto-accept access requests for tools
            _logger.LogInformation($"Access requested by {toolName} for {permission} - Auto-accepted");
            return await Task.FromResult(true);
        }

        public void Dispose()
        {
            _isRunning = false;
            
            try
            {
                InputWriter?.Close();
                InputWriter?.Dispose();
            }
            catch { }

            try
            {
                if (_consoleProcess != null && !_consoleProcess.HasExited)
                {
                    _consoleProcess.Kill();
                    _consoleProcess.WaitForExit(2000);
                }
                _consoleProcess?.Dispose();
            }
            catch { }

            _inputEvent?.Dispose();
        }
    }
}

