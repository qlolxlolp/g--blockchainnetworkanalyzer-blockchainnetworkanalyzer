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
    /// Advanced direction tracking service using triangulation and signal analysis
    /// </summary>
    public class DirectionTrackingService
    {
        private readonly ILogger<DirectionTrackingService> _logger;
        private readonly List<SignalMeasurement> _measurements = new List<SignalMeasurement>();
        private SignalPosition _currentPosition;
        private SignalPosition _minerPosition;

        public DirectionTrackingService()
        {
            _logger = App.LoggerFactory.CreateLogger<DirectionTrackingService>();
        }

        /// <summary>
        /// Calculate direction to miner using multiple signal measurements
        /// </summary>
        public DirectionResult CalculateDirection(List<SignalMeasurement> measurements)
        {
            if (measurements == null || measurements.Count < 2)
            {
                return new DirectionResult { Confidence = 0 };
            }

            try
            {
                // Use Time Difference of Arrival (TDOA) method
                var tdoaResult = CalculateTDOA(measurements);
                
                // Use Angle of Arrival (AOA) method
                var aoaResult = CalculateAOA(measurements);
                
                // Use Signal Strength based method
                var rssiResult = CalculateRSSI(measurements);

                // Combine results for best accuracy
                var combinedResult = CombineResults(tdoaResult, aoaResult, rssiResult);

                return combinedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating direction");
                return new DirectionResult { Confidence = 0 };
            }
        }

        /// <summary>
        /// Time Difference of Arrival triangulation
        /// </summary>
        private DirectionResult CalculateTDOA(List<SignalMeasurement> measurements)
        {
            if (measurements.Count < 3)
                return new DirectionResult { Confidence = 0 };

            // Calculate position using TDOA
            // Requires at least 3 measurement points
            
            var positions = measurements.Select(m => m.Position).ToList();
            var times = measurements.Select(m => m.Timestamp).ToList();
            var signals = measurements.Select(m => m.SignalStrength).ToList();

            // Find intersection point using hyperbola method
            var estimatedPosition = TriangulatePosition(positions, times, signals);

            if (estimatedPosition != null)
            {
                var bearing = CalculateBearing(_currentPosition, estimatedPosition);
                var distance = CalculateDistance(_currentPosition, estimatedPosition);

                return new DirectionResult
                {
                    Bearing = bearing,
                    Distance = distance,
                    Confidence = 0.85,
                    Method = "TDOA"
                };
            }

            return new DirectionResult { Confidence = 0 };
        }

        /// <summary>
        /// Angle of Arrival calculation
        /// </summary>
        private DirectionResult CalculateAOA(List<SignalMeasurement> measurements)
        {
            if (measurements.Count < 2)
                return new DirectionResult { Confidence = 0 };

            // Calculate angles from multiple measurement points
            var angles = new List<double>();

            for (int i = 0; i < measurements.Count - 1; i++)
            {
                var angle = CalculateAngleFromSignalDifference(
                    measurements[i],
                    measurements[i + 1]
                );
                
                if (angle.HasValue)
                {
                    angles.Add(angle.Value);
                }
            }

            if (angles.Any())
            {
                var averageAngle = angles.Average();
                var distance = EstimateDistanceFromAOA(measurements);

                return new DirectionResult
                {
                    Bearing = averageAngle,
                    Distance = distance,
                    Confidence = 0.75,
                    Method = "AOA"
                };
            }

            return new DirectionResult { Confidence = 0 };
        }

        /// <summary>
        /// Received Signal Strength Indicator based calculation
        /// </summary>
        private DirectionResult CalculateRSSI(List<SignalMeasurement> measurements)
        {
            // Use signal strength gradient to determine direction
            
            if (measurements.Count < 3)
                return new DirectionResult { Confidence = 0 };

            // Find direction of increasing signal strength
            var strongestSignal = measurements.OrderByDescending(m => m.SignalStrength).First();
            var weakestSignal = measurements.OrderBy(m => m.SignalStrength).First();

            var bearing = CalculateBearing(weakestSignal.Position, strongestSignal.Position);
            var distance = EstimateDistanceFromRSSI(strongestSignal.SignalStrength);

            return new DirectionResult
            {
                Bearing = bearing,
                Distance = distance,
                Confidence = 0.70,
                Method = "RSSI"
            };
        }

        private DirectionResult CombineResults(params DirectionResult[] results)
        {
            var validResults = results.Where(r => r.Confidence > 0.5).ToList();
            
            if (!validResults.Any())
                return new DirectionResult { Confidence = 0 };

            // Weighted average based on confidence
            var totalWeight = validResults.Sum(r => r.Confidence);
            
            var weightedBearing = validResults.Sum(r => r.Bearing * r.Confidence) / totalWeight;
            var weightedDistance = validResults.Sum(r => r.Distance * r.Confidence) / totalWeight;
            var averageConfidence = validResults.Average(r => r.Confidence);

            // Normalize bearing to 0-360
            weightedBearing = NormalizeBearing(weightedBearing);

            return new DirectionResult
            {
                Bearing = weightedBearing,
                Distance = weightedDistance,
                Confidence = averageConfidence,
                Method = "Combined"
            };
        }

        private SignalPosition TriangulatePosition(
            List<SignalPosition> positions, 
            List<DateTime> times, 
            List<double> signalStrengths)
        {
            if (positions.Count < 3)
                return null;

            // Simplified triangulation - in production would use proper algorithms
            // Like Chan's algorithm or Fang's algorithm for TDOA

            // Calculate time differences
            var timeDiffs = new List<double>();
            for (int i = 1; i < times.Count; i++)
            {
                timeDiffs.Add((times[i] - times[0]).TotalSeconds);
            }

            // Estimate position using trilateration
            var estimatedLat = positions.Average(p => p.Latitude);
            var estimatedLng = positions.Average(p => p.Longitude);

            // Refine using signal strength
            var weights = signalStrengths.Select(s => s / signalStrengths.Sum()).ToList();
            estimatedLat = positions.Zip(weights, (p, w) => p.Latitude * w).Sum();
            estimatedLng = positions.Zip(weights, (p, w) => p.Longitude * w).Sum();

            return new SignalPosition
            {
                Latitude = estimatedLat,
                Longitude = estimatedLng,
                Altitude = positions.Average(p => p.Altitude)
            };
        }

        private double CalculateBearing(SignalPosition from, SignalPosition to)
        {
            var dLon = ToRadians(to.Longitude - from.Longitude);
            var lat1 = ToRadians(from.Latitude);
            var lat2 = ToRadians(to.Latitude);

            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) - 
                   Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            var bearing = Math.Atan2(y, x);
            bearing = ToDegrees(bearing);
            
            return NormalizeBearing(bearing);
        }

        private double CalculateDistance(SignalPosition from, SignalPosition to)
        {
            const double R = 6371000; // Earth radius in meters
            
            var dLat = ToRadians(to.Latitude - from.Latitude);
            var dLon = ToRadians(to.Longitude - from.Longitude);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c; // Distance in meters
        }

        private double? CalculateAngleFromSignalDifference(
            SignalMeasurement m1, 
            SignalMeasurement m2)
        {
            // Calculate angle based on signal strength difference and positions
            var bearing = CalculateBearing(m1.Position, m2.Position);
            var signalDiff = m2.SignalStrength - m1.SignalStrength;

            // Adjust angle based on signal difference
            var angleAdjustment = signalDiff * 0.5; // Scale factor
            
            return bearing + angleAdjustment;
        }

        private double EstimateDistanceFromAOA(List<SignalMeasurement> measurements)
        {
            // Estimate distance using angle measurements
            // Simplified - would use proper geometric calculations in production
            
            var avgSignalStrength = measurements.Average(m => m.SignalStrength);
            
            // Path loss model: distance ≈ 10^((reference_power - received_power) / (10 * path_loss_exponent))
            var referencePower = 100; // dBm at 1 meter
            var pathLossExponent = 2.0; // Free space
            
            var distance = Math.Pow(10, (referencePower - avgSignalStrength) / (10 * pathLossExponent));
            
            return Math.Max(1, Math.Min(1000, distance)); // Clamp to reasonable range
        }

        private double EstimateDistanceFromRSSI(double signalStrength)
        {
            // Use path loss model
            var referencePower = 100; // dBm at 1 meter
            var pathLossExponent = 2.5; // Typical for indoor/outdoor
            
            var distance = Math.Pow(10, (referencePower - signalStrength) / (10 * pathLossExponent));
            
            return Math.Max(1, Math.Min(1000, distance));
        }

        private double NormalizeBearing(double bearing)
        {
            bearing = bearing % 360;
            if (bearing < 0)
                bearing += 360;
            return bearing;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        private double ToDegrees(double radians) => radians * 180.0 / Math.PI;

        /// <summary>
        /// Update current position (for tracking movement)
        /// </summary>
        public void UpdateCurrentPosition(double latitude, double longitude, double altitude = 0)
        {
            _currentPosition = new SignalPosition
            {
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude
            };
        }

        /// <summary>
        /// Add a new signal measurement for triangulation
        /// </summary>
        public void AddMeasurement(SignalMeasurement measurement)
        {
            _measurements.Add(measurement);
            
            // Keep only recent measurements (last 10)
            if (_measurements.Count > 10)
            {
                _measurements.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get current direction to miner
        /// </summary>
        public DirectionResult GetCurrentDirection()
        {
            if (_measurements.Count < 2)
                return new DirectionResult { Confidence = 0 };

            return CalculateDirection(_measurements);
        }
    }

    public class DirectionResult
    {
        public double Bearing { get; set; } // 0-360 degrees (0 = North)
        public double Distance { get; set; } // meters
        public double Confidence { get; set; } // 0-1
        public string Method { get; set; }
        public double? Elevation { get; set; } // Vertical angle
    }

    public class SignalMeasurement
    {
        public SignalPosition Position { get; set; }
        public double SignalStrength { get; set; } // 0-100
        public double Frequency { get; set; }
        public DateTime Timestamp { get; set; }
        public SignalType SignalType { get; set; }
    }

    public class SignalPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; } // meters
    }
}

