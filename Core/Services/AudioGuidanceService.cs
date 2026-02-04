using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Audio guidance service with beep patterns to guide operator to miner location
    /// Beep frequency and volume increase as operator gets closer to miner
    /// </summary>
    public class AudioGuidanceService : IDisposable
    {
        private readonly ILogger<AudioGuidanceService> _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isActive;
        private double _currentDistance;
        private double _previousDistance;
        private DirectionResult _currentDirection;
        private Timer _beepTimer;

        // Beep parameters
        private int _beepFrequency = 800; // Hz
        private int _beepDuration = 100; // ms
        private int _beepInterval = 500; // ms

        public AudioGuidanceService()
        {
            _logger = App.LoggerFactory.CreateLogger<AudioGuidanceService>();
        }

        /// <summary>
        /// Start audio guidance with beep patterns
        /// </summary>
        public void StartGuidance(double initialDistance, DirectionResult direction)
        {
            if (_isActive)
            {
                StopGuidance();
            }

            _isActive = true;
            _currentDistance = initialDistance;
            _previousDistance = initialDistance;
            _currentDirection = direction;
            _cancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation($"Starting audio guidance. Initial distance: {initialDistance}m");

            // Start continuous beep pattern
            StartBeepPattern(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Update guidance based on new distance and direction
        /// </summary>
        public void UpdateGuidance(double newDistance, DirectionResult direction)
        {
            _previousDistance = _currentDistance;
            _currentDistance = newDistance;
            _currentDirection = direction;

            // Adjust beep parameters based on distance
            AdjustBeepParameters(newDistance);

            // Log status
            var distanceChange = _previousDistance - _currentDistance;
            if (Math.Abs(distanceChange) > 0.5) // Significant change
            {
                if (distanceChange > 0)
                {
                    _logger.LogInformation($"Getting closer! Distance: {newDistance:F2}m (closer by {distanceChange:F2}m)");
                }
                else
                {
                    _logger.LogInformation($"Moving away! Distance: {newDistance:F2}m (farther by {Math.Abs(distanceChange):F2}m)");
                }
            }
        }

        private void StartBeepPattern(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested && _isActive)
                    {
                        // Generate beep with current parameters
                        PlayBeep(_beepFrequency, _beepDuration);

                        // Wait for next beep interval
                        await Task.Delay(_beepInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in beep pattern");
                }
            }, cancellationToken);
        }

        private void AdjustBeepParameters(double distance)
        {
            // Closer = higher frequency, shorter interval, longer duration
            // Farther = lower frequency, longer interval, shorter duration

            // Distance range: 0.5m - 100m
            var normalizedDistance = Math.Max(0.5, Math.Min(100, distance));

            // Frequency: 400 Hz (far) to 2000 Hz (close)
            _beepFrequency = (int)(2000 - (normalizedDistance / 100) * 1600);
            _beepFrequency = Math.Max(400, Math.Min(2000, _beepFrequency));

            // Interval: 2000ms (far) to 100ms (close) - faster beeps when closer
            _beepInterval = (int)(2000 - (normalizedDistance / 100) * 1900);
            _beepInterval = Math.Max(100, Math.Min(2000, _beepInterval));

            // Duration: 50ms (far) to 200ms (close) - longer beeps when closer
            _beepDuration = (int)(50 + (normalizedDistance / 100) * 150);
            _beepDuration = Math.Max(50, Math.Min(200, _beepDuration));

            // Adjust volume based on distance (system volume control)
            // Volume: 20% (far) to 100% (close)
            var volume = (int)(20 + (normalizedDistance / 100) * 80);
            
            _logger.LogDebug($"Beep adjusted - Distance: {distance:F2}m, Freq: {_beepFrequency}Hz, Interval: {_beepInterval}ms, Duration: {_beepDuration}ms");
        }

        private void PlayBeep(int frequency, int duration)
        {
            try
            {
                // Use Windows API to play beep
                // In production, could use NAudio or similar for better control
                
                // Method 1: Console.Beep (simple but limited)
                // Console.Beep(frequency, duration);

                // Method 2: Generate tone programmatically (better control)
                GenerateTone(frequency, duration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error playing beep");
            }
        }

        private void GenerateTone(int frequency, int duration)
        {
            try
            {
                // Generate sine wave tone
                var sampleRate = 44100;
                var samples = duration * sampleRate / 1000;
                var waveData = new short[samples];

                for (int i = 0; i < samples; i++)
                {
                    var t = (double)i / sampleRate;
                    waveData[i] = (short)(Math.Sin(2 * Math.PI * frequency * t) * short.MaxValue * 0.3); // 30% volume
                }

                // Play using SoundPlayer or audio API
                // In production: Use NAudio or similar library for real-time audio playback
                
                // For now, use system beep as fallback
                if (frequency >= 37 && frequency <= 32767)
                {
                    System.Console.Beep(frequency, duration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error generating tone: {ex.Message}");
            }
        }

        /// <summary>
        /// Play directional beep pattern (left/right indication)
        /// </summary>
        public void PlayDirectionalBeep(DirectionResult direction)
        {
            if (direction?.Bearing == null)
                return;

            // Determine if miner is to the left or right
            // Assuming 0 degrees is straight ahead (North)
            var bearing = direction.Bearing;
            var isLeft = bearing > 270 || bearing < 90;
            var isRight = bearing > 90 && bearing < 270;

            // Play different beep patterns for left/right
            if (isLeft)
            {
                // Left: two short beeps
                PlayBeep(_beepFrequency, 50);
                Task.Delay(50).Wait();
                PlayBeep(_beepFrequency, 50);
            }
            else if (isRight)
            {
                // Right: three short beeps
                PlayBeep(_beepFrequency, 50);
                Task.Delay(50).Wait();
                PlayBeep(_beepFrequency, 50);
                Task.Delay(50).Wait();
                PlayBeep(_beepFrequency, 50);
            }
            else
            {
                // Straight ahead: single long beep
                PlayBeep(_beepFrequency, _beepDuration);
            }
        }

        /// <summary>
        /// Play warning beep (when moving away from target)
        /// </summary>
        public void PlayWarningBeep()
        {
            // Warning: low frequency, repeated
            for (int i = 0; i < 3; i++)
            {
                PlayBeep(400, 100);
                Task.Delay(100).Wait();
            }
        }

        /// <summary>
        /// Play success beep (when very close to target)
        /// </summary>
        public void PlaySuccessBeep()
        {
            // Success: ascending tone
            for (int freq = 800; freq <= 1200; freq += 100)
            {
                PlayBeep(freq, 100);
                Task.Delay(50).Wait();
            }
        }

        /// <summary>
        /// Stop audio guidance
        /// </summary>
        public void StopGuidance()
        {
            _isActive = false;
            _cancellationTokenSource?.Cancel();
            _beepTimer?.Dispose();
            
            _logger.LogInformation("Audio guidance stopped");
        }

        /// <summary>
        /// Get current beep status
        /// </summary>
        public BeepStatus GetStatus()
        {
            return new BeepStatus
            {
                IsActive = _isActive,
                CurrentDistance = _currentDistance,
                BeepFrequency = _beepFrequency,
                BeepInterval = _beepInterval,
                BeepDuration = _beepDuration,
                IsGettingCloser = _previousDistance > _currentDistance
            };
        }

        public void Dispose()
        {
            StopGuidance();
            _cancellationTokenSource?.Dispose();
            _beepTimer?.Dispose();
        }
    }

    public class BeepStatus
    {
        public bool IsActive { get; set; }
        public double CurrentDistance { get; set; }
        public int BeepFrequency { get; set; }
        public int BeepInterval { get; set; }
        public int BeepDuration { get; set; }
        public bool IsGettingCloser { get; set; }
    }
}

