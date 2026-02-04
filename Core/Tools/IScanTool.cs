using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Interface for all scanning tools
    /// </summary>
    public interface IScanTool
    {
        string Name { get; }
        string Description { get; }
        string ExecutablePath { get; set; }
        bool IsInstalled { get; }
        bool IsAvailable { get; }

        event EventHandler<string> LogReceived;
        event EventHandler<ToolProgressEventArgs> ProgressChanged;

        Task<bool> CheckInstallationAsync();
        Task<ToolResult> ExecuteAsync(ScanConfiguration config, Dictionary<string, object> parameters);
        Task<ToolResult> ExecuteAsync(string arguments);
        Task CancelAsync();
        Dictionary<string, object> GetDefaultParameters();
    }

    public class ToolProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ToolResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string ErrorOutput { get; set; }
        public int ExitCode { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<IPResult> ParsedResults { get; set; } = new List<IPResult>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}

