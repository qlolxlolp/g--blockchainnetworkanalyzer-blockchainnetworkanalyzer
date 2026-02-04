using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Base class for all scanning tools
    /// </summary>
    public abstract class BaseScanTool : IScanTool
    {
        protected readonly ILogger _logger;
        protected Process _currentProcess;
        protected CancellationTokenSource _cancellationTokenSource;
        protected readonly StringBuilder _outputBuffer = new StringBuilder();
        protected readonly StringBuilder _errorBuffer = new StringBuilder();

        public abstract string Name { get; }
        public abstract string Description { get; }
        public string ExecutablePath { get; set; }
        public virtual bool IsInstalled => File.Exists(ExecutablePath);
        public virtual bool IsAvailable => IsInstalled;

        public event EventHandler<string> LogReceived;
        public event EventHandler<ToolProgressEventArgs> ProgressChanged;

        protected BaseScanTool()
        {
            _logger = App.LoggerFactory.CreateLogger(GetType());
            ExecutablePath = FindExecutable();
        }

        protected abstract string FindExecutable();
        protected abstract string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters);
        protected virtual List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            // Default implementation - override in derived classes
            return new List<IPResult>();
        }

        public virtual async Task<bool> CheckInstallationAsync()
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(ExecutablePath))
                {
                    ExecutablePath = FindExecutable();
                }
                return IsInstalled;
            });
        }

        public virtual async Task<ToolResult> ExecuteAsync(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var arguments = BuildArguments(config, parameters);
            return await ExecuteAsync(arguments);
        }

        public virtual async Task<ToolResult> ExecuteAsync(string arguments)
        {
            var result = new ToolResult { Success = false };
            var startTime = DateTime.Now;

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _outputBuffer.Clear();
                _errorBuffer.Clear();

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                OnLogReceived($"Executing: {ExecutablePath} {arguments}");

                _currentProcess = Process.Start(processStartInfo);
                if (_currentProcess == null)
                {
                    throw new Exception("Failed to start process");
                }

                // Read output asynchronously
                var outputTask = ReadOutputAsync(_currentProcess.StandardOutput, _outputBuffer, _cancellationTokenSource.Token);
                var errorTask = ReadOutputAsync(_currentProcess.StandardError, _errorBuffer, _cancellationTokenSource.Token);

                await _currentProcess.WaitForExitAsync(_cancellationTokenSource.Token);
                await Task.WhenAll(outputTask, errorTask);

                result.ExitCode = _currentProcess.ExitCode;
                result.Output = _outputBuffer.ToString();
                result.ErrorOutput = _errorBuffer.ToString();
                result.Success = result.ExitCode == 0;
                result.ExecutionTime = DateTime.Now - startTime;

                OnLogReceived($"Process completed with exit code: {result.ExitCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing {Name}");
                result.ErrorOutput = ex.Message;
                result.Success = false;
                result.ExecutionTime = DateTime.Now - startTime;
            }
            finally
            {
                _currentProcess?.Dispose();
                _currentProcess = null;
            }

            return result;
        }

        protected virtual async Task ReadOutputAsync(StreamReader reader, StringBuilder buffer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    buffer.AppendLine(line);
                    OnLogReceived(line);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading output from {Name}");
            }
        }

        public virtual async Task CancelAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    _currentProcess.Kill();
                    await Task.Delay(1000);
                }
                OnLogReceived("Process cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling {Name}");
            }
        }

        public abstract Dictionary<string, object> GetDefaultParameters();

        protected virtual void OnLogReceived(string message)
        {
            LogReceived?.Invoke(this, $"[{Name}] {message}");
        }

        protected virtual void OnProgressChanged(int progress, string status, string message = null)
        {
            ProgressChanged?.Invoke(this, new ToolProgressEventArgs
            {
                Progress = progress,
                Status = status,
                Message = message
            });
        }
    }
}

