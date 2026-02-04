using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Integrated miner tracking service that combines all detection methods
    /// Coordinates EM, acoustic, and direction tracking for comprehensive miner detection
    /// </summary>
    public class IntegratedMinerTrackingService : IDisposable
    {
        private readonly ILogger<IntegratedMinerTrackingService> _logger;
        private readonly ElectromagneticDetectionService _emDetection;
        private readonly AcousticDetectionService _acousticDetection;
        private readonly DirectionTrackingService _directionTracking;
        private readonly AudioGuidanceService _audioGuidance;
        private readonly List<ComprehensiveMinerDetection> _detectedMiners = new List<ComprehensiveMinerDetection>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isTracking;

        public event EventHandler<MinerDetectedEventArgs> MinerDetected;
        public event EventHandler<DirectionUpdatedEventArgs> DirectionUpdated;

        public IntegratedMinerTrackingService()
        {
            _logger = App.LoggerFactory.CreateLogger<IntegratedMinerTrackingService>();
            _emDetection = new ElectromagneticDetectionService();
            _acousticDetection = new AcousticDetectionService();
            _directionTracking = new DirectionTrackingService();
            _audioGuidance = new AudioGuidanceService();

            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
            _emDetection.SignalDetected += OnEMSignal;
            _acousticDetection.SignalDetected += OnAcousticSignal;
            _acousticDetection.DirectionUpdated += OnAcousticDirection;
        }

        /// <summary>
        /// Start comprehensive miner tracking
        /// </summary>
        public async Task StartTrackingAsync(int scanDurationSeconds = 60)
        {
            if (_isTracking)
            {
                _logger.LogWarning("Tracking already in progress");
                return;
            }

            _isTracking = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _detectedMiners.Clear();

            try
            {
                _logger.LogInformation("Starting integrated miner tracking...");

                // Start all detection services in parallel
                var tasks = new List<Task>
                {
                    _emDetection.StartScanningAsync(scanDurationSeconds),
                    _acousticDetection.StartListeningAsync()
                };

                // Start audio guidance
                _audioGuidance.StartGuidance(100, null);

                // Start monitoring and correlation
                var monitoringTask = MonitorAndCorrelateSignals(_cancellationTokenSource.Token);

                await Task.WhenAll(tasks);
                await monitoringTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during integrated tracking");
            }
            finally
            {
                _isTracking = false;
            }
        }

        private async Task MonitorAndCorrelateSignals(CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Correlate signals from different sources
                        await CorrelateSignals();
                        
                        // Update direction tracking
                        UpdateDirectionTracking();
                        
                        // Update audio guidance
                        UpdateAudioGuidance();

                        await Task.Delay(500, cancellationToken);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in signal correlation");
                }
            }, cancellationToken);
        }

        private async Task CorrelateSignals()
        {
            // Correlate EM and acoustic signals to identify miners
            var emSignals = _emDetection.GetDetectedSignals();
            var acousticSignals = _acousticDetection.GetDetectedSignals();

            // Group signals by frequency similarity
            var correlated = new List<ComprehensiveMinerDetection>();

            foreach (var emSignal in emSignals)
            {
                // Find matching acoustic signals
                var matchingAcoustic = acousticSignals
                    .Where(a => Math.Abs(a.Frequency - emSignal.Frequency) < 1000)
                    .ToList();

                if (matchingAcoustic.Any())
                {
                    var detection = new ComprehensiveMinerDetection
                    {
                        EMSignal = emSignal,
                        AcousticSignals = matchingAcoustic,
                        CombinedConfidence = CalculateCombinedConfidence(emSignal, matchingAcoustic),
                        EstimatedDistance = CalculateAverageDistance(emSignal, matchingAcoustic),
                        DetectedAt = DateTime.Now
                    };

                    correlated.Add(detection);
                }
            }

            // Update detected miners
            lock (_detectedMiners)
            {
                foreach (var detection in correlated)
                {
                    var existing = _detectedMiners.FirstOrDefault(d => 
                        Math.Abs(d.EMSignal.Frequency - detection.EMSignal.Frequency) < 100);

                    if (existing == null)
                    {
                        _detectedMiners.Add(detection);
                        OnMinerDetected(new MinerDetectedEventArgs(detection));
                    }
                    else
                    {
                        // Update existing detection
                        existing.EMSignal = detection.EMSignal;
                        existing.CombinedConfidence = detection.CombinedConfidence;
                        existing.EstimatedDistance = detection.EstimatedDistance;
                    }
                }
            }

            await Task.CompletedTask;
        }

        private double CalculateCombinedConfidence(MinerSignal emSignal, List<AcousticSignal> acousticSignals)
        {
            // Higher confidence when multiple detection methods agree
            var emConfidence = emSignal.Confidence;
            var acousticConfidence = acousticSignals.Average(a => a.Confidence);
            
            // Combined confidence is weighted average
            var combined = (emConfidence * 0.6) + (acousticConfidence * 0.4);
            
            // Bonus for multiple acoustic signals
            if (acousticSignals.Count > 1)
                combined *= 1.1;
            
            return Math.Min(1.0, combined);
        }

        private double CalculateAverageDistance(MinerSignal emSignal, List<AcousticSignal> acousticSignals)
        {
            var distances = new List<double> { emSignal.EstimatedDistance };
            distances.AddRange(acousticSignals.Select(a => a.EstimatedDistance));
            
            return distances.Average();
        }

        private void UpdateDirectionTracking()
        {
            lock (_detectedMiners)
            {
                foreach (var detection in _detectedMiners)
                {
                    // Create signal measurements
                    var measurements = new List<SignalMeasurement>
                    {
                        new SignalMeasurement
                        {
                            Position = GetCurrentPosition(),
                            SignalStrength = detection.EMSignal.SignalStrength,
                            Frequency = detection.EMSignal.Frequency,
                            Timestamp = detection.EMSignal.Timestamp,
                            SignalType = detection.EMSignal.SignalType
                        }
                    };

                    measurements.AddRange(detection.AcousticSignals.Select(a => new SignalMeasurement
                    {
                        Position = GetCurrentPosition(),
                        SignalStrength = a.Amplitude,
                        Frequency = a.Frequency,
                        Timestamp = a.Timestamp,
                        SignalType = a.SignalType
                    }));

                    // Calculate direction
                    var direction = _directionTracking.CalculateDirection(measurements);
                    
                    if (direction != null && direction.Confidence > 0.5)
                    {
                        detection.Direction = direction;
                        OnDirectionUpdated(new DirectionUpdatedEventArgs
                        {
                            Detection = detection,
                            Direction = direction
                        });
                    }
                }
            }
        }

        private void UpdateAudioGuidance()
        {
            lock (_detectedMiners)
            {
                var bestDetection = _detectedMiners
                    .Where(d => d.Direction != null)
                    .OrderByDescending(d => d.CombinedConfidence)
                    .FirstOrDefault();

                if (bestDetection?.Direction != null)
                {
                    _audioGuidance.UpdateGuidance(
                        bestDetection.EstimatedDistance,
                        bestDetection.Direction
                    );
                }
            }
        }

        private SignalPosition GetCurrentPosition()
        {
            // In production, get from GPS or location service
            return new SignalPosition
            {
                Latitude = 35.6892,
                Longitude = 51.3890,
                Altitude = 0
            };
        }

        private void OnEMSignal(object sender, MinerSignalDetectedEventArgs e)
        {
            // Handle EM signal
        }

        private void OnAcousticSignal(object sender, AcousticSignalDetectedEventArgs e)
        {
            // Handle acoustic signal
        }

        private void OnAcousticDirection(object sender, DirectionEventArgs e)
        {
            // Handle direction update
        }

        protected virtual void OnMinerDetected(MinerDetectedEventArgs e)
        {
            MinerDetected?.Invoke(this, e);
        }

        protected virtual void OnDirectionUpdated(DirectionUpdatedEventArgs e)
        {
            DirectionUpdated?.Invoke(this, e);
        }

        public void StopTracking()
        {
            _isTracking = false;
            _cancellationTokenSource?.Cancel();
            
            _emDetection.StopScanning();
            _acousticDetection.StopListening();
            _audioGuidance.StopGuidance();
        }

        public List<ComprehensiveMinerDetection> GetDetectedMiners()
        {
            lock (_detectedMiners)
            {
                return new List<ComprehensiveMinerDetection>(_detectedMiners);
            }
        }

        public void Dispose()
        {
            StopTracking();
            _cancellationTokenSource?.Dispose();
            _emDetection?.Dispose();
            _acousticDetection?.Dispose();
            _audioGuidance?.Dispose();
        }
    }

    public class ComprehensiveMinerDetection
    {
        public MinerSignal EMSignal { get; set; }
        public List<AcousticSignal> AcousticSignals { get; set; } = new List<AcousticSignal>();
        public double CombinedConfidence { get; set; }
        public double EstimatedDistance { get; set; }
        public DirectionResult Direction { get; set; }
        public DateTime DetectedAt { get; set; }
        public string MinerType { get; set; }
    }

    public class MinerDetectedEventArgs : EventArgs
    {
        public ComprehensiveMinerDetection Detection { get; }

        public MinerDetectedEventArgs(ComprehensiveMinerDetection detection)
        {
            Detection = detection;
        }
    }

    public class DirectionUpdatedEventArgs : EventArgs
    {
        public ComprehensiveMinerDetection Detection { get; set; }
        public DirectionResult Direction { get; set; }
    }
}

