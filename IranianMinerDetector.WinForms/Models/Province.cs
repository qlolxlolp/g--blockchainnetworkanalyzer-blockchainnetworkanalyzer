using System;
using System.Collections.Generic;
using System.Linq;

namespace IranianMinerDetector.WinForms.Models
{
    public class Province
    {
        public string Name { get; set; } = string.Empty;
        public string NamePersian { get; set; } = string.Empty;
        public int Code { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<City> Cities { get; set; } = new List<City>();

        public City? GetCity(string name)
        {
            return Cities.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class City
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
