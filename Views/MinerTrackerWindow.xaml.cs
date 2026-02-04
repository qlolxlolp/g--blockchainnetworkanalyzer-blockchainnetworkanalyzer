using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlockchainNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Views
{
    public partial class MinerTrackerWindow : Window
    {
        private readonly ILogger<MinerTrackerWindow> _logger;
        private readonly ElectromagneticDetectionService _emDetectionService;
        private readonly AcousticDetectionService _acousticDetectionService;
        private readonly DirectionTrackingService _directionService;
        private readonly AudioGuidanceService _audioGuidanceService;
        private readonly List<SignalMeasurement> _signalMeasurements = new List<SignalMeasurement>();
        private DispatcherTimer _updateTimer;
        private bool _isTracking;

        public MinerTrackerWindow()
        {
            InitializeComponent();
            _logger = App.LoggerFactory.CreateLogger<MinerTrackerWindow>();
            _emDetectionService = new ElectromagneticDetectionService();
            _acousticDetectionService = new AcousticDetectionService();
            _directionService = new DirectionTrackingService();
            _audioGuidanceService = new AudioGuidanceService();

            InitializeEventHandlers();
            InitializeUI();
        }

        private void InitializeEventHandlers()
        {
            // EM Detection events
            _emDetectionService.SignalDetected += OnEMSignalDetected;
            _emDetectionService.ScanProgress += OnScanProgress;

            // Acoustic Detection events
            _acousticDetectionService.SignalDetected += OnAcousticSignalDetected;
            _acousticDetectionService.DirectionUpdated += OnDirectionUpdated;

            // Scan duration slider
            ScanDurationSlider.ValueChanged += (s, e) =>
            {
                ScanDurationText.Text = $"Duration: {e.NewValue}s";
            };

            // Update timer for UI refresh
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void InitializeUI()
        {
            // Initialize compass display
            InitializeCompass();
        }

        private void InitializeCompass()
        {
            // Draw compass background
            DrawCompass();
        }

        private void DrawCompass()
        {
            // Compass is drawn in XAML, this can be enhanced with custom drawing
        }

        private async void StartDetectionBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartDetectionBtn.IsEnabled = false;
                StopDetectionBtn.IsEnabled = true;
                _isTracking = true;

                StatusBarText.Text = "Starting detection...";
                StatusText.Text = "Detecting...";

                var scanDuration = (int)ScanDurationSlider.Value;

                // Start EM detection
                if (EnableEMDetectionCheck.IsChecked == true)
                {
                    await _emDetectionService.StartScanningAsync(scanDuration);
                }

                // Start acoustic detection
                if (EnableAcousticDetectionCheck.IsChecked == true)
                {
                    await _acousticDetectionService.StartListeningAsync();
                }

                // Start audio guidance
                if (EnableAudioGuidanceCheck.IsChecked == true)
                {
                    _audioGuidanceService.StartGuidance(100, null); // Initial distance
                    AudioGuidanceStatusText.Text = "On";
                }

                StatusBarText.Text = "Detection active";
                StatusText.Text = "Scanning for miners...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting detection");
                MessageBox.Show($"Error starting detection: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                StartDetectionBtn.IsEnabled = true;
                StopDetectionBtn.IsEnabled = false;
                _isTracking = false;
            }
        }

        private void StopDetectionBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isTracking = false;

                _emDetectionService.StopScanning();
                _acousticDetectionService.StopListening();
                _audioGuidanceService.StopGuidance();

                StartDetectionBtn.IsEnabled = true;
                StopDetectionBtn.IsEnabled = false;

                StatusBarText.Text = "Detection stopped";
                StatusText.Text = "Stopped";
                AudioGuidanceStatusText.Text = "Off";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping detection");
            }
        }

        private void OnEMSignalDetected(object sender, MinerSignalDetectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var signal = e.Signal;

                    // Add measurement for triangulation
                    var measurement = new SignalMeasurement
                    {
                        Position = GetCurrentPosition(),
                        SignalStrength = signal.SignalStrength,
                        Frequency = signal.Frequency,
                        Timestamp = signal.Timestamp,
                        SignalType = signal.SignalType
                    };

                    _signalMeasurements.Add(measurement);
                    _directionService.AddMeasurement(measurement);

                    // Update UI
                    UpdateSignalDisplay(signal);
                    UpdateDirection();

                    // Update audio guidance
                    if (EnableAudioGuidanceCheck.IsChecked == true)
                    {
                        var directionResult = _directionService.GetCurrentDirection();
                        if (directionResult != null && directionResult.Confidence > 0.5)
                        {
                            _audioGuidanceService.UpdateGuidance(
                                directionResult.Distance,
                                directionResult
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error handling EM signal");
                }
            });
        }

        private void OnAcousticSignalDetected(object sender, AcousticSignalDetectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var signal = e.Signal;

                    // Add measurement
                    var measurement = new SignalMeasurement
                    {
                        Position = GetCurrentPosition(),
                        SignalStrength = signal.Amplitude,
                        Frequency = signal.Frequency,
                        Timestamp = signal.Timestamp,
                        SignalType = signal.SignalType
                    };

                    _signalMeasurements.Add(measurement);
                    _directionService.AddMeasurement(measurement);

                    // Update UI
                    UpdateSignalDisplay(signal);
                    UpdateDirection();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error handling acoustic signal");
                }
            });
        }

        private void OnDirectionUpdated(object sender, DirectionEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDirectionDisplay(e.Bearing, e.Distance, e.Confidence);

                // Update audio guidance
                if (EnableAudioGuidanceCheck.IsChecked == true)
                {
                    var directionResult = new DirectionResult
                    {
                        Bearing = e.Bearing,
                        Distance = e.Distance,
                        Confidence = e.Confidence
                    };
                    _audioGuidanceService.UpdateGuidance(e.Distance, directionResult);
                }
            });
        }

        private void OnScanProgress(object sender, ScanProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = e.Message;
                SignalsCountText.Text = $"Signals: {e.SignalsDetected}";
            });
        }

        private void UpdateSignalDisplay(object signal)
        {
            // Add to signals list
            var signals = SignalsListBox.ItemsSource as List<object> ?? new List<object>();
            signals.Insert(0, signal);

            if (signals.Count > 10)
                signals.RemoveAt(signals.Count - 1);

            SignalsListBox.ItemsSource = signals;
            SignalsListBox.Items.Refresh();

            // Update count
            SignalsCountText.Text = $"Signals: {signals.Count}";
        }

        private void UpdateDirection()
        {
            var directionResult = _directionService.GetCurrentDirection();
            if (directionResult != null && directionResult.Confidence > 0.5)
            {
                UpdateDirectionDisplay(
                    directionResult.Bearing,
                    directionResult.Distance,
                    directionResult.Confidence
                );
            }
        }

        private void UpdateDirectionDisplay(double bearing, double distance, double confidence)
        {
            // Update compass arrow
            var angle = bearing - 90; // Adjust for display (0 = right, 90 = up)
            var radians = angle * Math.PI / 180;

            var centerX = 200.0;
            var centerY = 200.0;
            var length = 100.0;

            var endX = centerX + length * Math.Cos(radians);
            var endY = centerY - length * Math.Sin(radians);

            DirectionArrow.X2 = endX;
            DirectionArrow.Y2 = endY;

            // Update distance and bearing text
            DistanceText.Text = $"Distance: {distance:F1} m";
            BearingText.Text = $"Bearing: {bearing:F1}° ({GetBearingDirection(bearing)})";

            // Update confidence
            ConfidenceBar.Value = confidence * 100;
            ConfidenceText.Text = $"{confidence:P0}";
        }

        private string GetBearingDirection(double bearing)
        {
            if (bearing >= 337.5 || bearing < 22.5) return "N";
            if (bearing >= 22.5 && bearing < 67.5) return "NE";
            if (bearing >= 67.5 && bearing < 112.5) return "E";
            if (bearing >= 112.5 && bearing < 157.5) return "SE";
            if (bearing >= 157.5 && bearing < 202.5) return "S";
            if (bearing >= 202.5 && bearing < 247.5) return "SW";
            if (bearing >= 247.5 && bearing < 292.5) return "W";
            return "NW";
        }

        private SignalPosition GetCurrentPosition()
        {
            // In production, would get from GPS or location service
            // For now, return current position (could be from GeolocationService)
            return new SignalPosition
            {
                Latitude = 35.6892, // Default to Tehran
                Longitude = 51.3890,
                Altitude = 0
            };
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isTracking)
                return;

            // Update signal strength indicator
            if (_signalMeasurements.Any())
            {
                var avgStrength = _signalMeasurements
                    .OrderByDescending(m => m.Timestamp)
                    .Take(5)
                    .Average(m => m.SignalStrength);

                SignalStrengthBar.Value = avgStrength;
                SignalStrengthText.Text = $"{avgStrength:F0}%";
            }

            // Update audio guidance status
            var beepStatus = _audioGuidanceService.GetStatus();
            if (beepStatus.IsActive)
            {
                var statusMsg = beepStatus.IsGettingCloser ? "Getting Closer!" : "Moving Away";
                StatusBarText.Text = $"{statusMsg} - Distance: {beepStatus.CurrentDistance:F1}m";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isTracking = false;
            _emDetectionService?.Dispose();
            _acousticDetectionService?.Dispose();
            _audioGuidanceService?.Dispose();
            _updateTimer?.Stop();
            base.OnClosed(e);
        }
    }
}

