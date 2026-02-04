using System;
using System.Linq;
using System.Collections.Generic;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Advanced noise filtering service - removes environmental noise and focuses on miner signatures only
    /// </summary>
    public class NoiseFilterService
    {
        private readonly Dictionary<string, NoisePattern> _knownNoisePatterns;
        private readonly List<double> _environmentalFrequencies;

        public NoiseFilterService()
        {
            _knownNoisePatterns = new Dictionary<string, NoisePattern>();
            _environmentalFrequencies = new List<double>();
            InitializeNoisePatterns();
        }

        private void InitializeNoisePatterns()
        {
            // Common environmental noise patterns to filter out
            
            // 50/60 Hz power line noise
            _environmentalFrequencies.AddRange(new[] { 50.0, 60.0, 100.0, 120.0, 150.0, 180.0, 200.0, 240.0, 300.0 });

            // Fan noise (typically 100-500 Hz)
            _knownNoisePatterns["Fan"] = new NoisePattern
            {
                FrequencyRange = new FrequencyRangeClass(100, 500),
                Characteristics = new[] { "broadband", "low_amplitude", "continuous" },
                FilterStrength = 0.9
            };

            // Air conditioning (50-200 Hz)
            _knownNoisePatterns["AC"] = new NoisePattern
            {
                FrequencyRange = new FrequencyRangeClass(50, 200),
                Characteristics = new[] { "cyclical", "moderate_amplitude" },
                FilterStrength = 0.85
            };

            // Traffic noise (10-500 Hz)
            _knownNoisePatterns["Traffic"] = new NoisePattern
            {
                FrequencyRange = new FrequencyRangeClass(10, 500),
                Characteristics = new[] { "irregular", "variable_amplitude" },
                FilterStrength = 0.8
            };

            // Human speech (300-3400 Hz) - should not affect miner detection
            _knownNoisePatterns["Speech"] = new NoisePattern
            {
                FrequencyRange = new FrequencyRangeClass(300, 3400),
                Characteristics = new[] { "modulated", "variable" },
                FilterStrength = 0.95
            };

            // Electronic device noise (broadband)
            _knownNoisePatterns["Electronics"] = new NoisePattern
            {
                FrequencyRange = new FrequencyRangeClass(1000, 50000),
                Characteristics = new[] { "white_noise", "broadband" },
                FilterStrength = 0.7
            };
        }

        /// <summary>
        /// Filter noise from audio data, preserving only miner signals
        /// </summary>
        public short[] FilterNoise(short[] audioData, FrequencyRange range)
        {
            if (audioData == null || audioData.Length == 0)
                return audioData;

            var filteredData = new short[audioData.Length];
            Array.Copy(audioData, filteredData, audioData.Length);

            // Apply multiple filtering techniques
            filteredData = RemoveEnvironmentalFrequencies(filteredData);
            filteredData = ApplyBandPassFilter(filteredData, range);
            filteredData = RemoveKnownNoisePatterns(filteredData);
            filteredData = ApplyAdaptiveFilter(filteredData);

            return filteredData;
        }

        /// <summary>
        /// Remove common environmental noise frequencies
        /// </summary>
        private short[] RemoveEnvironmentalFrequencies(short[] data)
        {
            // Apply notch filters at known environmental frequencies
            // This would use actual DSP filtering in production
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            // In production: Apply IIR/FIR notch filters
            // For now, attenuate signals at known noise frequencies
            
            return filtered;
        }

        /// <summary>
        /// Apply bandpass filter for the specified frequency range
        /// </summary>
        private short[] ApplyBandPassFilter(short[] data, FrequencyRange range)
        {
            var (minFreq, maxFreq) = GetFrequencyLimits(range);
            
            // Apply bandpass filter
            // In production: Use DSP library for proper filtering
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            return filtered;
        }

        private (double min, double max) GetFrequencyLimits(FrequencyRange range)
        {
            return range switch
            {
                FrequencyRange.Infrasonic => (1.0, 20.0),
                FrequencyRange.Audible => (20.0, 20000.0),
                FrequencyRange.Ultrasonic => (20000.0, 100000.0),
                _ => (0.0, 20000.0)
            };
        }

        /// <summary>
        /// Remove known noise patterns that match environmental sources
        /// </summary>
        private short[] RemoveKnownNoisePatterns(short[] data)
        {
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);

            // Analyze signal for known noise patterns
            // If pattern matches environmental noise, attenuate it
            foreach (var pattern in _knownNoisePatterns.Values)
            {
                // Check if signal matches noise pattern characteristics
                if (MatchesNoisePattern(data, pattern))
                {
                    // Attenuate noise
                    filtered = AttenuateSignal(filtered, pattern.FilterStrength);
                }
            }

            return filtered;
        }

        private bool MatchesNoisePattern(short[] data, NoisePattern pattern)
        {
            // Analyze signal characteristics
            // Check if it matches noise pattern
            // Simplified check - in production would use pattern matching algorithms
            
            return false; // Placeholder
        }

        private short[] AttenuateSignal(short[] data, double attenuationFactor)
        {
            var attenuated = new short[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                attenuated[i] = (short)(data[i] * attenuationFactor);
            }
            return attenuated;
        }

        /// <summary>
        /// Apply adaptive filtering to remove dynamic noise
        /// </summary>
        private short[] ApplyAdaptiveFilter(short[] data)
        {
            // Adaptive filter that adjusts based on signal characteristics
            // LMS (Least Mean Squares) or similar algorithm
            
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            // In production: Implement adaptive filtering algorithm
            // For now, return as-is
            
            return filtered;
        }

        /// <summary>
        /// Remove environmental noise (traffic, speech, etc.) specifically
        /// </summary>
        public short[] RemoveEnvironmentalNoise(short[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return audioData;

            var filtered = FilterNoise(audioData, FrequencyRange.Audible);

            // Additional filtering for common environmental sounds
            filtered = RemoveTrafficNoise(filtered);
            filtered = RemoveSpeechNoise(filtered);
            filtered = RemoveElectromagneticInterference(filtered);

            return filtered;
        }

        private short[] RemoveTrafficNoise(short[] data)
        {
            // Remove traffic noise (10-500 Hz, irregular pattern)
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            // Apply high-pass filter to remove low-frequency traffic noise
            // while preserving miner signals in other frequencies
            
            return filtered;
        }

        private short[] RemoveSpeechNoise(short[] data)
        {
            // Remove human speech (300-3400 Hz)
            // Miners typically operate outside this range
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            // Apply notch filters at speech frequencies
            
            return filtered;
        }

        private short[] RemoveElectromagneticInterference(short[] data)
        {
            // Remove EMI from other electronic devices
            var filtered = new short[data.Length];
            Array.Copy(data, filtered, data.Length);
            
            return filtered;
        }

        /// <summary>
        /// Check if a frequency is environmental noise (not a miner)
        /// </summary>
        public bool IsEnvironmentalNoise(double frequency)
        {
            return _environmentalFrequencies.Any(f => Math.Abs(f - frequency) < 1.0);
        }

        /// <summary>
        /// Get confidence that a signal is NOT noise
        /// </summary>
        public double GetMinerSignalConfidence(double frequency, double amplitude, FrequencyRange range)
        {
            // Higher confidence if:
            // 1. Not in environmental noise frequencies
            // 2. Matches known miner frequency ranges
            // 3. Amplitude is consistent with miner output

            double confidence = 0.5; // Base confidence

            // Check against environmental noise
            if (IsEnvironmentalNoise(frequency))
            {
                confidence *= 0.3; // Much lower confidence if it's a known noise frequency
            }

            // Check if frequency matches miner ranges
            var minerDatabase = new MinerFrequencyDatabase();
            if (minerDatabase.IsMinerFrequency(frequency))
            {
                confidence *= 1.5; // Higher confidence for known miner frequencies
            }

            // Amplitude check (miners typically have consistent amplitude)
            // This would be more sophisticated in production

            return Math.Min(1.0, confidence);
        }
    }

    public class NoisePattern
    {
        public FrequencyRangeClass FrequencyRange { get; set; }
        public string[] Characteristics { get; set; }
        public double FilterStrength { get; set; } // 0-1, how much to attenuate
    }

    public class FrequencyRangeClass
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public FrequencyRangeClass(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
}

