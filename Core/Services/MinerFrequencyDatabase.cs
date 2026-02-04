using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Database of known miner frequency signatures and patterns
    /// Contains acoustic, RF, and EM signatures for all major miner types
    /// </summary>
    public class MinerFrequencyDatabase
    {
        private readonly Dictionary<string, List<KnownFrequency>> _minerFrequencies;
        private readonly Dictionary<double, MinerFrequencySignature> _frequencySignatures;

        public MinerFrequencyDatabase()
        {
            _minerFrequencies = new Dictionary<string, List<KnownFrequency>>();
            _frequencySignatures = new Dictionary<double, MinerFrequencySignature>();
        }

        public void LoadMinerFrequencies()
        {
            // Bitcoin ASIC Miners
            AddMinerFrequencies("Antminer S19", new[]
            {
                new KnownFrequency { Frequency = 25000, Tolerance = 100, Threshold = 60, MinerType = "Bitcoin ASIC", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 180, Tolerance = 10, Threshold = 45, MinerType = "Antminer S19", Range = FrequencyRange.Audible },
                new KnownFrequency { Frequency = 150000, Tolerance = 1000, Threshold = 50, MinerType = "Antminer S19", Range = FrequencyRange.RadioFrequency }
            });

            AddMinerFrequencies("Antminer S21", new[]
            {
                new KnownFrequency { Frequency = 26000, Tolerance = 100, Threshold = 65, MinerType = "Bitcoin ASIC", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 190, Tolerance = 10, Threshold = 48, MinerType = "Antminer S21", Range = FrequencyRange.Audible }
            });

            AddMinerFrequencies("Whatsminer M50", new[]
            {
                new KnownFrequency { Frequency = 24000, Tolerance = 100, Threshold = 58, MinerType = "Bitcoin ASIC", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 175, Tolerance = 10, Threshold = 50, MinerType = "Whatsminer M50", Range = FrequencyRange.Audible },
                new KnownFrequency { Frequency = 145000, Tolerance = 1000, Threshold = 55, MinerType = "Whatsminer M50", Range = FrequencyRange.RadioFrequency }
            });

            AddMinerFrequencies("AvalonMiner", new[]
            {
                new KnownFrequency { Frequency = 23000, Tolerance = 100, Threshold = 55, MinerType = "Bitcoin ASIC", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 165, Tolerance = 10, Threshold = 42, MinerType = "AvalonMiner", Range = FrequencyRange.Audible }
            });

            // GPU Miners (Ethereum, etc.)
            AddMinerFrequencies("GPU Miner (NVIDIA)", new[]
            {
                new KnownFrequency { Frequency = 30000, Tolerance = 500, Threshold = 40, MinerType = "GPU Miner", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 200, Tolerance = 20, Threshold = 35, MinerType = "GPU Miner", Range = FrequencyRange.Audible },
                new KnownFrequency { Frequency = 120000, Tolerance = 5000, Threshold = 38, MinerType = "GPU Miner", Range = FrequencyRange.RadioFrequency }
            });

            AddMinerFrequencies("GPU Miner (AMD)", new[]
            {
                new KnownFrequency { Frequency = 28000, Tolerance = 500, Threshold = 38, MinerType = "GPU Miner", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 195, Tolerance = 20, Threshold = 33, MinerType = "GPU Miner", Range = FrequencyRange.Audible }
            });

            // Specialized Miners
            AddMinerFrequencies("Etherminer", new[]
            {
                new KnownFrequency { Frequency = 32000, Tolerance = 500, Threshold = 42, MinerType = "Ethereum Miner", Range = FrequencyRange.Ultrasonic },
                new KnownFrequency { Frequency = 210, Tolerance = 15, Threshold = 37, MinerType = "Etherminer", Range = FrequencyRange.Audible }
            });

            // Power consumption signatures (infrasonic)
            AddMinerFrequencies("Miner Power Signature", new[]
            {
                new KnownFrequency { Frequency = 5, Tolerance = 1, Threshold = 30, MinerType = "Power Consumption", Range = FrequencyRange.Infrasonic },
                new KnownFrequency { Frequency = 10, Tolerance = 2, Threshold = 25, MinerType = "Power Consumption", Range = FrequencyRange.Infrasonic }
            });

            // Build frequency lookup dictionary
            BuildFrequencyIndex();
        }

        private void AddMinerFrequencies(string minerName, KnownFrequency[] frequencies)
        {
            _minerFrequencies[minerName] = new List<KnownFrequency>(frequencies);
        }

        private void BuildFrequencyIndex()
        {
            foreach (var minerFreqs in _minerFrequencies.Values)
            {
                foreach (var freq in minerFreqs)
                {
                    if (!_frequencySignatures.ContainsKey(freq.Frequency))
                    {
                        _frequencySignatures[freq.Frequency] = new MinerFrequencySignature
                        {
                            Frequency = freq.Frequency,
                            MinerTypes = new List<string> { freq.MinerType },
                            Confidence = 0.8
                        };
                    }
                    else
                    {
                        if (!_frequencySignatures[freq.Frequency].MinerTypes.Contains(freq.MinerType))
                        {
                            _frequencySignatures[freq.Frequency].MinerTypes.Add(freq.MinerType);
                        }
                    }
                }
            }
        }

        public bool IsMinerFrequency(double frequency)
        {
            return _frequencySignatures.Keys.Any(f => Math.Abs(f - frequency) < 1000);
        }

        public double GetConfidence(double frequency)
        {
            var matchingFreq = _frequencySignatures.Keys
                .FirstOrDefault(f => Math.Abs(f - frequency) < 1000);

            if (matchingFreq != 0)
            {
                return _frequencySignatures[matchingFreq].Confidence;
            }

            return 0.5; // Default confidence
        }

        public List<double> GetKnownFrequenciesInRange(double minFreq, double maxFreq)
        {
            return _frequencySignatures.Keys
                .Where(f => f >= minFreq && f <= maxFreq)
                .ToList();
        }

        public List<KnownFrequency> GetKnownAcousticFrequencies(FrequencyRange range)
        {
            var allFrequencies = _minerFrequencies.Values.SelectMany(f => f);
            return allFrequencies
                .Where(f => f.Range == range)
                .ToList();
        }

        public bool MatchesMinerPowerSignature(PowerNoiseReading reading)
        {
            // Check if power noise pattern matches known miner signatures
            var knownPowerFreqs = GetKnownAcousticFrequencies(FrequencyRange.Infrasonic);
            
            return knownPowerFreqs.Any(f => 
                Math.Abs(f.Frequency - reading.DominantFrequency) < f.Tolerance);
        }

        public bool MatchesEMSignature(double frequency, double strength)
        {
            // Check if EM field matches miner signature
            // Miners typically produce EM fields in specific ranges
            var rfFrequencies = _frequencySignatures.Keys
                .Where(f => f > 50000 && f < 500000); // RF range

            return rfFrequencies.Any(f => Math.Abs(f - frequency) < 10000);
        }
    }

    public class KnownFrequency
    {
        public double Frequency { get; set; } // Hz or MHz
        public double Tolerance { get; set; } // ±Hz or MHz
        public double Threshold { get; set; } // Minimum amplitude to detect
        public string MinerType { get; set; }
        public FrequencyRange Range { get; set; }
    }

    public class MinerFrequencySignature
    {
        public double Frequency { get; set; }
        public List<string> MinerTypes { get; set; } = new List<string>();
        public double Confidence { get; set; }
    }

    public enum FrequencyRange
    {
        Infrasonic,
        Audible,
        Ultrasonic,
        RadioFrequency
    }
}

