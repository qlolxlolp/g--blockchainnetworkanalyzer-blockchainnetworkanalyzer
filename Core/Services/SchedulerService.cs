using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    public class SchedulerService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly NetworkScanner _scanner;
        private readonly ReportingService _reportingService;
        private readonly Dictionary<long, Timer> _scheduledTimers;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SchedulerService()
        {
            _logger = App.LoggerFactory.CreateLogger<SchedulerService>();
            _scanner = new NetworkScanner();
            _reportingService = new ReportingService();
            _scheduledTimers = new Dictionary<long, Timer>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<ScheduledScan> CreateScheduledScanAsync(ScheduledScan schedule)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    INSERT INTO ScheduledScans 
                    (Name, Description, Frequency, CronExpression, StartDate, EndDate, StartTime, 
                     IsEnabled, Configuration, TargetProvince, TargetCities, TargetISPs,
                     EmailEnabled, EmailAddress, WebhookEnabled, WebhookUrl,
                     OnCompletion, OnDetection, OnFailure, IncludeReport, CreatedAt, CreatedBy)
                    VALUES 
                    (@Name, @Description, @Frequency, @Cron, @StartDate, @EndDate, @StartTime,
                     @IsEnabled, @Config, @Province, @Cities, @ISPs,
                     @EmailEnabled, @Email, @WebhookEnabled, @Webhook,
                     @OnCompletion, @OnDetection, @OnFailure, @IncludeReport, @CreatedAt, @CreatedBy);
                    SELECT last_insert_rowid();";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Name", schedule.Name);
                command.Parameters.AddWithValue("@Description", schedule.Description ?? "");
                command.Parameters.AddWithValue("@Frequency", schedule.Frequency.ToString());
                command.Parameters.AddWithValue("@Cron", schedule.CronExpression ?? "");
                command.Parameters.AddWithValue("@StartDate", schedule.StartDate);
                command.Parameters.AddWithValue("@EndDate", (object)schedule.EndDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartTime", schedule.StartTime?.ToString() ?? "");
                command.Parameters.AddWithValue("@IsEnabled", schedule.IsEnabled ? 1 : 0);
                command.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(schedule.Configuration));
                command.Parameters.AddWithValue("@Province", schedule.TargetProvince ?? "");
                command.Parameters.AddWithValue("@Cities", string.Join(",", schedule.TargetCities ?? new List<string>()));
                command.Parameters.AddWithValue("@ISPs", string.Join(",", schedule.TargetISPs ?? new List<string>()));
                command.Parameters.AddWithValue("@EmailEnabled", schedule.Notifications?.EmailEnabled ?? false ? 1 : 0);
                command.Parameters.AddWithValue("@Email", schedule.Notifications?.EmailAddress ?? "");
                command.Parameters.AddWithValue("@WebhookEnabled", schedule.Notifications?.WebhookEnabled ?? false ? 1 : 0);
                command.Parameters.AddWithValue("@Webhook", schedule.Notifications?.WebhookUrl ?? "");
                command.Parameters.AddWithValue("@OnCompletion", schedule.Notifications?.OnCompletion ?? true ? 1 : 0);
                command.Parameters.AddWithValue("@OnDetection", schedule.Notifications?.OnDetection ?? true ? 1 : 0);
                command.Parameters.AddWithValue("@OnFailure", schedule.Notifications?.OnFailure ?? true ? 1 : 0);
                command.Parameters.AddWithValue("@IncludeReport", schedule.Notifications?.IncludeReport ?? true ? 1 : 0);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@CreatedBy", Environment.UserName);

                schedule.Id = Convert.ToInt64(await Task.Run(() => command.ExecuteScalar()));
                
                if (schedule.IsEnabled)
                {
                    ScheduleScan(schedule);
                }

                _logger.LogInformation($"Created scheduled scan: {schedule.Name} (ID: {schedule.Id})");
                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scheduled scan");
                throw;
            }
        }

        public async Task<List<ScheduledScan>> GetScheduledScansAsync()
        {
            var scans = new List<ScheduledScan>();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = "SELECT * FROM ScheduledScans ORDER BY CreatedAt DESC";
                using var command = new SQLiteCommand(sql, connection);
                using var reader = await Task.Run(() => command.ExecuteReader());

                while (reader.Read())
                {
                    scans.Add(MapScheduledScan(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled scans");
            }

            return scans;
        }

        public async Task<ScheduledScan> GetScheduledScanAsync(long id)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = "SELECT * FROM ScheduledScans WHERE Id = @Id";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                using var reader = await Task.Run(() => command.ExecuteReader());

                if (reader.Read())
                {
                    return MapScheduledScan(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting scheduled scan {id}");
            }

            return null;
        }

        public async Task UpdateScheduledScanAsync(ScheduledScan schedule)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    UPDATE ScheduledScans SET
                    Name = @Name, Description = @Description, Frequency = @Frequency,
                    CronExpression = @Cron, StartDate = @StartDate, EndDate = @EndDate,
                    StartTime = @StartTime, IsEnabled = @IsEnabled,
                    Configuration = @Config, TargetProvince = @Province, 
                    TargetCities = @Cities, TargetISPs = @ISPs,
                    EmailEnabled = @EmailEnabled, EmailAddress = @Email,
                    WebhookEnabled = @WebhookEnabled, WebhookUrl = @Webhook
                    WHERE Id = @Id";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", schedule.Id);
                command.Parameters.AddWithValue("@Name", schedule.Name);
                command.Parameters.AddWithValue("@Description", schedule.Description ?? "");
                command.Parameters.AddWithValue("@Frequency", schedule.Frequency.ToString());
                command.Parameters.AddWithValue("@Cron", schedule.CronExpression ?? "");
                command.Parameters.AddWithValue("@StartDate", schedule.StartDate);
                command.Parameters.AddWithValue("@EndDate", (object)schedule.EndDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartTime", schedule.StartTime?.ToString() ?? "");
                command.Parameters.AddWithValue("@IsEnabled", schedule.IsEnabled ? 1 : 0);
                command.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(schedule.Configuration));
                command.Parameters.AddWithValue("@Province", schedule.TargetProvince ?? "");
                command.Parameters.AddWithValue("@Cities", string.Join(",", schedule.TargetCities ?? new List<string>()));
                command.Parameters.AddWithValue("@ISPs", string.Join(",", schedule.TargetISPs ?? new List<string>()));
                command.Parameters.AddWithValue("@EmailEnabled", schedule.Notifications?.EmailEnabled ?? false ? 1 : 0);
                command.Parameters.AddWithValue("@Email", schedule.Notifications?.EmailAddress ?? "");
                command.Parameters.AddWithValue("@WebhookEnabled", schedule.Notifications?.WebhookEnabled ?? false ? 1 : 0);
                command.Parameters.AddWithValue("@Webhook", schedule.Notifications?.WebhookUrl ?? "");

                await Task.Run(() => command.ExecuteNonQuery());

                // Reschedule if enabled
                if (schedule.IsEnabled)
                {
                    ScheduleScan(schedule);
                }
                else
                {
                    CancelSchedule(schedule.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating scheduled scan {schedule.Id}");
                throw;
            }
        }

        public async Task DeleteScheduledScanAsync(long id)
        {
            try
            {
                CancelSchedule(id);

                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                using var command = new SQLiteCommand("DELETE FROM ScheduledScans WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                await Task.Run(() => command.ExecuteNonQuery());

                _logger.LogInformation($"Deleted scheduled scan: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting scheduled scan {id}");
                throw;
            }
        }

        public async Task ExecuteScheduledScanAsync(long scheduleId)
        {
            var schedule = await GetScheduledScanAsync(scheduleId);
            if (schedule == null) return;

            var execution = new ScheduleExecution
            {
                ScheduledScanId = scheduleId,
                StartedAt = DateTime.UtcNow,
                Status = ScheduleStatus.Running
            };

            try
            {
                // Save execution record
                execution.Id = await SaveExecutionAsync(execution);

                _logger.LogInformation($"Executing scheduled scan: {schedule.Name}");

                // Configure scan
                var config = schedule.Configuration;
                if (!string.IsNullOrEmpty(schedule.TargetProvince))
                {
                    config.ScanName = $"{schedule.Name} - {schedule.TargetProvince}";
                }

                // Execute scan
                var result = await _scanner.ScanAsync(config);

                // Update execution record
                execution.ScanResultId = result.Id;
                execution.CompletedAt = DateTime.UtcNow;
                execution.Status = ScheduleStatus.Completed;
                execution.HostsFound = result.FoundHosts;
                execution.MinersDetected = result.IPResults?.Count(r => r.BlockchainDetected) ?? 0;

                // Update schedule
                schedule.LastRun = DateTime.UtcNow;
                schedule.NextRun = CalculateNextRun(schedule);
                schedule.RunCount++;
                await UpdateScheduleExecutionAsync(schedule);

                // Send notifications
                if (schedule.Notifications?.OnCompletion == true)
                {
                    await SendNotificationAsync(schedule, execution, result);
                }

                await UpdateExecutionAsync(execution);

                _logger.LogInformation($"Scheduled scan completed: {schedule.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scheduled scan: {schedule.Name}");
                
                execution.Status = ScheduleStatus.Failed;
                execution.ErrorMessage = ex.Message;
                execution.CompletedAt = DateTime.UtcNow;
                await UpdateExecutionAsync(execution);

                if (schedule.Notifications?.OnFailure == true)
                {
                    await SendFailureNotificationAsync(schedule, execution, ex);
                }
            }
        }

        public void Start()
        {
            _logger.LogInformation("Starting scheduler service");
            
            // Load and schedule all enabled scans
            Task.Run(async () =>
            {
                var scans = await GetScheduledScansAsync();
                foreach (var scan in scans.Where(s => s.IsEnabled))
                {
                    ScheduleScan(scan);
                }
            });
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping scheduler service");
            _cancellationTokenSource.Cancel();
            
            foreach (var timer in _scheduledTimers.Values)
            {
                timer?.Dispose();
            }
            _scheduledTimers.Clear();
        }

        private void ScheduleScan(ScheduledScan schedule)
        {
            // Cancel existing schedule if any
            CancelSchedule(schedule.Id);

            var nextRun = CalculateNextRun(schedule);
            if (nextRun == null) return;

            var delay = nextRun.Value - DateTime.Now;
            if (delay.TotalMilliseconds < 0) delay = TimeSpan.Zero;

            var timer = new Timer(
                async _ => await ExecuteScheduledScanAsync(schedule.Id),
                null,
                delay,
                GetPeriod(schedule.Frequency));

            _scheduledTimers[schedule.Id] = timer;
            _logger.LogInformation($"Scheduled scan '{schedule.Name}' will run at {nextRun:yyyy-MM-dd HH:mm:ss}");
        }

        private void CancelSchedule(long scheduleId)
        {
            if (_scheduledTimers.TryGetValue(scheduleId, out var timer))
            {
                timer?.Dispose();
                _scheduledTimers.Remove(scheduleId);
            }
        }

        private DateTime? CalculateNextRun(ScheduledScan schedule)
        {
            if (schedule.EndDate.HasValue && schedule.EndDate.Value < DateTime.Now)
                return null;

            var nextRun = schedule.LastRun ?? schedule.StartDate;
            
            if (schedule.LastRun.HasValue)
            {
                nextRun = schedule.Frequency switch
                {
                    ScheduleFrequency.Hourly => nextRun.AddHours(1),
                    ScheduleFrequency.Daily => nextRun.AddDays(1),
                    ScheduleFrequency.Weekly => nextRun.AddDays(7),
                    ScheduleFrequency.Monthly => nextRun.AddMonths(1),
                    _ => nextRun.AddDays(1)
                };
            }

            // Apply start time if specified
            if (schedule.StartTime.HasValue)
            {
                nextRun = nextRun.Date + schedule.StartTime.Value;
            }

            return nextRun;
        }

        private TimeSpan GetPeriod(ScheduleFrequency frequency)
        {
            return frequency switch
            {
                ScheduleFrequency.Hourly => TimeSpan.FromHours(1),
                ScheduleFrequency.Daily => TimeSpan.FromDays(1),
                ScheduleFrequency.Weekly => TimeSpan.FromDays(7),
                ScheduleFrequency.Monthly => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(1)
            };
        }

        private async Task<long> SaveExecutionAsync(ScheduleExecution execution)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            var sql = @"
                INSERT INTO ScheduleExecutions 
                (ScheduledScanId, StartedAt, Status)
                VALUES (@ScheduleId, @Started, @Status);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@ScheduleId", execution.ScheduledScanId);
            command.Parameters.AddWithValue("@Started", execution.StartedAt);
            command.Parameters.AddWithValue("@Status", execution.Status.ToString());

            return Convert.ToInt64(await Task.Run(() => command.ExecuteScalar()));
        }

        private async Task UpdateExecutionAsync(ScheduleExecution execution)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            var sql = @"
                UPDATE ScheduleExecutions SET
                ScanResultId = @ScanResultId, CompletedAt = @Completed, 
                Status = @Status, ErrorMessage = @Error, HostsFound = @Hosts, 
                MinersDetected = @Miners, NotificationSent = @NotificationSent
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", execution.Id);
            command.Parameters.AddWithValue("@ScanResultId", (object)execution.ScanResultId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Completed", (object)execution.CompletedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", execution.Status.ToString());
            command.Parameters.AddWithValue("@Error", execution.ErrorMessage ?? "");
            command.Parameters.AddWithValue("@Hosts", execution.HostsFound);
            command.Parameters.AddWithValue("@Miners", execution.MinersDetected);
            command.Parameters.AddWithValue("@NotificationSent", execution.NotificationSent ? 1 : 0);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        private async Task UpdateScheduleExecutionAsync(ScheduledScan schedule)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            var sql = @"
                UPDATE ScheduledScans SET
                LastRun = @LastRun, NextRun = @NextRun, RunCount = @RunCount
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", schedule.Id);
            command.Parameters.AddWithValue("@LastRun", (object)schedule.LastRun ?? DBNull.Value);
            command.Parameters.AddWithValue("@NextRun", (object)schedule.NextRun ?? DBNull.Value);
            command.Parameters.AddWithValue("@RunCount", schedule.RunCount);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        private async Task SendNotificationAsync(ScheduledScan schedule, ScheduleExecution execution, ScanResult result)
        {
            try
            {
                if (schedule.Notifications?.EmailEnabled == true)
                {
                    // Email notification would be implemented here
                    _logger.LogInformation($"Would send email to: {schedule.Notifications.EmailAddress}");
                }

                if (schedule.Notifications?.WebhookEnabled == true)
                {
                    // Webhook notification would be implemented here
                    _logger.LogInformation($"Would call webhook: {schedule.Notifications.WebhookUrl}");
                }

                execution.NotificationSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
            }
            
            await Task.CompletedTask;
        }

        private async Task SendFailureNotificationAsync(ScheduledScan schedule, ScheduleExecution execution, Exception ex)
        {
            _logger.LogWarning($"Scan failed notification for {schedule.Name}: {ex.Message}");
            await Task.CompletedTask;
        }

        private ScheduledScan MapScheduledScan(SQLiteDataReader reader)
        {
            var config = reader.IsDBNull(8) ? new ScanConfiguration() : 
                Newtonsoft.Json.JsonConvert.DeserializeObject<ScanConfiguration>(reader.GetString(8));

            var cities = reader.IsDBNull(10) ? new List<string>() : 
                reader.GetString(10).Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            var isps = reader.IsDBNull(11) ? new List<string>() : 
                reader.GetString(11).Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();

            return new ScheduledScan
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Frequency = (ScheduleFrequency)Enum.Parse(typeof(ScheduleFrequency), reader.GetString(3)),
                CronExpression = reader.IsDBNull(4) ? "" : reader.GetString(4),
                StartDate = reader.GetDateTime(5),
                EndDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                StartTime = string.IsNullOrEmpty(reader.GetString(7)) ? null : TimeSpan.Parse(reader.GetString(7)),
                IsEnabled = reader.GetInt32(9) == 1,
                Configuration = config,
                TargetProvince = reader.IsDBNull(10) ? "" : reader.GetString(10),
                TargetCities = cities,
                TargetISPs = isps,
                Notifications = new NotificationSettings
                {
                    EmailEnabled = reader.GetInt32(11) == 1,
                    EmailAddress = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    WebhookEnabled = reader.GetInt32(13) == 1,
                    WebhookUrl = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    OnCompletion = reader.GetInt32(15) == 1,
                    OnDetection = reader.GetInt32(16) == 1,
                    OnFailure = reader.GetInt32(17) == 1,
                    IncludeReport = reader.GetInt32(18) == 1
                },
                CreatedAt = reader.GetDateTime(19),
                LastRun = reader.IsDBNull(20) ? null : reader.GetDateTime(20),
                NextRun = reader.IsDBNull(21) ? null : reader.GetDateTime(21),
                RunCount = reader.GetInt32(22),
                CreatedBy = reader.IsDBNull(23) ? "" : reader.GetString(23)
            };
        }
    }
}
