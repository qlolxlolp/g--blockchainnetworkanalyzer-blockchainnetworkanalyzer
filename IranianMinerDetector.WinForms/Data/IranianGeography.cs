using System.Collections.Generic;
using System.Linq;

namespace IranianMinerDetector.WinForms.Data
{
    public static class IranianGeography
    {
        private static List<Province>? _provinces;

        public static List<Province> Provinces => _provinces ??= LoadProvinces();

        public static List<string> ProvinceNames => Provinces.Select(p => p.Name).ToList();

        private static List<Province> LoadProvinces()
        {
            return new List<Province>
            {
                new Province
                {
                    Name = "Tehran",
                    NamePersian = "تهران",
                    Code = 1,
                    Latitude = 35.6892,
                    Longitude = 51.3890,
                    Cities = new List<City>
                    {
                        new City { Name = "Tehran", Latitude = 35.6892, Longitude = 51.3890 },
                        new City { Name = "Rey", Latitude = 35.6030, Longitude = 51.4278 },
                        new City { Name = "Shemiranat", Latitude = 35.8036, Longitude = 51.4142 },
                        new City { Name = "Islamshahr", Latitude = 35.5656, Longitude = 51.2369 },
                        new City { Name = "Pakdasht", Latitude = 35.4794, Longitude = 51.6978 },
                        new City { Name = "Varamin", Latitude = 35.3242, Longitude = 51.6481 },
                        new City { Name = "Damavand", Latitude = 35.7075, Longitude = 52.0542 },
                        new City { Name = "Karaj", Latitude = 35.8327, Longitude = 50.9915 }
                    }
                },
                new Province
                {
                    Name = "Alborz",
                    NamePersian = "البرز",
                    Code = 2,
                    Latitude = 36.0667,
                    Longitude = 50.8667,
                    Cities = new List<City>
                    {
                        new City { Name = "Karaj", Latitude = 35.8327, Longitude = 50.9915 },
                        new City { Name = "Hashtgerd", Latitude = 35.9586, Longitude = 50.9556 },
                        new City { Name = "Nazabad", Latitude = 35.9289, Longitude = 50.7367 },
                        new City { Name = "Savojbolagh", Latitude = 35.9697, Longitude = 50.6772 },
                        new City { Name = "Taleqan", Latitude = 36.1914, Longitude = 50.6597 },
                        new City { Name = "Eshtehard", Latitude = 35.6967, Longitude = 50.3222 }
                    }
                },
                new Province
                {
                    Name = "Isfahan",
                    NamePersian = "اصفهان",
                    Code = 3,
                    Latitude = 32.6546,
                    Longitude = 51.6678,
                    Cities = new List<City>
                    {
                        new City { Name = "Isfahan", Latitude = 32.6546, Longitude = 51.6678 },
                        new City { Name = "Kashan", Latitude = 33.9831, Longitude = 51.4364 },
                        new City { Name = "Najafabad", Latitude = 32.6381, Longitude = 51.3689 },
                        new City { Name = "Shahreza", Latitude = 32.0094, Longitude = 51.8581 },
                        new City { Name = "Mobarak-e Sar", Latitude = 32.7344, Longitude = 51.2369 },
                        new City { Name = "Fuladshahr", Latitude = 32.5303, Longitude = 51.3728 },
                        new City { Name = "Khomeyni Shahr", Latitude = 32.6956, Longitude = 51.5236 },
                        new City { Name = "Natanz", Latitude = 33.5169, Longitude = 51.7331 },
                        new City { Name = "Ardestan", Latitude = 33.3739, Longitude = 52.3753 }
                    }
                },
                new Province
                {
                    Name = "Fars",
                    NamePersian = "فارس",
                    Code = 4,
                    Latitude = 29.5918,
                    Longitude = 52.5837,
                    Cities = new List<City>
                    {
                        new City { Name = "Shiraz", Latitude = 29.5918, Longitude = 52.5837 },
                        new City { Name = "Marvdasht", Latitude = 29.8642, Longitude = 52.8067 },
                        new City { Name = "Fasa", Latitude = 28.9694, Longitude = 53.6883 },
                        new City { Name = "Jahrom", Latitude = 28.5003, Longitude = 53.5881 },
                        new City { Name = "Kazerun", Latitude = 29.6158, Longitude = 51.6544 },
                        new City { Name = "Lar", Latitude = 27.6753, Longitude = 54.3325 },
                        new City { Name = "Neyriz", Latitude = 29.1983, Longitude = 54.3275 },
                        new City { Name = "Estahban", Latitude = 29.1292, Longitude = 54.0294 },
                        new City { Name = "Zarqan", Latitude = 29.7583, Longitude = 52.7208 }
                    }
                },
                new Province
                {
                    Name = "Khorasan Razavi",
                    NamePersian = "خراسان رضوی",
                    Code = 5,
                    Latitude = 36.3172,
                    Longitude = 59.5628,
                    Cities = new List<City>
                    {
                        new City { Name = "Mashhad", Latitude = 36.3172, Longitude = 59.5628 },
                        new City { Name = "Neishabur", Latitude = 36.2089, Longitude = 58.6831 },
                        new City { Name = "Sabzevar", Latitude = 36.2186, Longitude = 57.6833 },
                        new City { Name = "Torbat-e Heydarieh", Latitude = 35.2742, Longitude = 59.2250 },
                        new City { Name = "Torbat-e Jam", Latitude = 35.2417, Longitude = 60.6214 },
                        new City { Name = "Quchan", Latitude = 37.1089, Longitude = 58.5039 },
                        new City { Name = "Kashmar", Latitude = 35.2428, Longitude = 58.4647 },
                        new City { Name = "Gonabad", Latitude = 34.5656, Longitude = 58.6819 }
                    }
                },
                new Province
                {
                    Name = "East Azerbaijan",
                    NamePersian = "آذربایجان شرقی",
                    Code = 6,
                    Latitude = 38.0818,
                    Longitude = 46.2889,
                    Cities = new List<City>
                    {
                        new City { Name = "Tabriz", Latitude = 38.0818, Longitude = 46.2889 },
                        new City { Name = "Maragheh", Latitude = 37.3839, Longitude = 46.2525 },
                        new City { Name = "Ahar", Latitude = 38.4758, Longitude = 47.0689 },
                        new City { Name = "Mianeh", Latitude = 37.4531, Longitude = 47.6861 },
                        new City { Name = "Bostanabad", Latitude = 37.8408, Longitude = 46.8675 },
                        new City { Name = "Azarshahr", Latitude = 37.8567, Longitude = 45.9256 },
                        new City { Name = "Bonab", Latitude = 37.4758, Longitude = 46.0667 }
                    }
                },
                new Province
                {
                    Name = "West Azerbaijan",
                    NamePersian = "آذربایجان غربی",
                    Code = 7,
                    Latitude = 37.5531,
                    Longitude = 45.0764,
                    Cities = new List<City>
                    {
                        new City { Name = "Urmia", Latitude = 37.5531, Longitude = 45.0764 },
                        new City { Name = "Khoy", Latitude = 38.5503, Longitude = 44.9519 },
                        new City { Name = "Mahabad", Latitude = 36.7675, Longitude = 45.7239 },
                        new City { Name = "Maku", Latitude = 39.1975, Longitude = 44.5150 },
                        new City { Name = "Salmas", Latitude = 38.2011, Longitude = 44.7683 },
                        new City { Name = "Takab", Latitude = 36.4000, Longitude = 47.1167 }
                    }
                },
                new Province
                {
                    Name = "Kermanshah",
                    NamePersian = "کرمانشاه",
                    Code = 8,
                    Latitude = 34.3142,
                    Longitude = 47.0650,
                    Cities = new List<City>
                    {
                        new City { Name = "Kermanshah", Latitude = 34.3142, Longitude = 47.0650 },
                        new City { Name = "Sahneh", Latitude = 34.5011, Longitude = 47.4469 },
                        new City { Name = "Harsin", Latitude = 34.3358, Longitude = 47.5883 },
                        new City { Name = "Kangavar", Latitude = 34.5050, Longitude = 47.9869 },
                        new City { Name = "Qasr-e Shirin", Latitude = 34.1303, Longitude = 45.5747 },
                        new City { Name = "Sarpol-e Zahab", Latitude = 34.4669, Longitude = 45.8567 },
                        new City { Name = "Eslamabad-e Gharb", Latitude = 34.1067, Longitude = 46.4639 },
                        new City { Name = "Paveh", Latitude = 35.0619, Longitude = 46.1586 }
                    }
                },
                new Province
                {
                    Name = "Khuzestan",
                    NamePersian = "خوزستان",
                    Code = 9,
                    Latitude = 31.3188,
                    Longitude = 48.6706,
                    Cities = new List<City>
                    {
                        new City { Name = "Ahvaz", Latitude = 31.3188, Longitude = 48.6706 },
                        new City { Name = "Abadan", Latitude = 30.3450, Longitude = 48.2667 },
                        new City { Name = "Khorramshahr", Latitude = 30.4347, Longitude = 48.1656 },
                        new City { Name = "Shushtar", Latitude = 32.0519, Longitude = 48.8517 },
                        new City { Name = "Dezful", Latitude = 32.3764, Longitude = 48.3939 },
                        new City { Name = "Behbahan", Latitude = 30.5967, Longitude = 50.2417 },
                        new City { Name = "Mahshahr", Latitude = 30.5450, Longitude = 49.1828 },
                        new City { Name = "Andimeshk", Latitude = 32.4578, Longitude = 48.3519 }
                    }
                },
                new Province
                {
                    Name = "Kerman",
                    NamePersian = "کرمان",
                    Code = 10,
                    Latitude = 30.2839,
                    Longitude = 57.0834,
                    Cities = new List<City>
                    {
                        new City { Name = "Kerman", Latitude = 30.2839, Longitude = 57.0834 },
                        new City { Name = "Bam", Latitude = 29.1089, Longitude = 58.3539 },
                        new City { Name = "Rafsanjan", Latitude = 30.4058, Longitude = 55.9953 },
                        new City { Name = "Sirjan", Latitude = 29.4569, Longitude = 55.6836 },
                        new City { Name = "Zarand", Latitude = 31.1286, Longitude = 56.5700 },
                        new City { Name = "Bardsir", Latitude = 29.9222, Longitude = 57.0333 },
                        new City { Name = "Baft", Latitude = 29.2775, Longitude = 56.5789 },
                        new City { Name = "Jiroft", Latitude = 28.6736, Longitude = 57.7342 },
                        new City { Name = "Kahnuj", Latitude = 27.9542, Longitude = 57.7242 }
                    }
                },
                new Province
                {
                    Name = "Yazd",
                    NamePersian = "یزد",
                    Code = 11,
                    Latitude = 31.8974,
                    Longitude = 54.3675,
                    Cities = new List<City>
                    {
                        new City { Name = "Yazd", Latitude = 31.8974, Longitude = 54.3675 },
                        new City { Name = "Mehriz", Latitude = 31.5519, Longitude = 54.4097 },
                        new City { Name = "Ardakan", Latitude = 32.2858, Longitude = 53.9647 },
                        new City { Name = "Maybod", Latitude = 32.2206, Longitude = 54.0289 },
                        new City { Name = "Taft", Latitude = 31.7367, Longitude = 54.2139 },
                        new City { Name = "Bafq", Latitude = 31.6028, Longitude = 55.4014 },
                        new City { Name = "Abarkuh", Latitude = 31.1294, Longitude = 53.2758 }
                    }
                },
                new Province
                {
                    Name = "Hormozgan",
                    NamePersian = "هرمزگان",
                    Code = 12,
                    Latitude = 27.1865,
                    Longitude = 56.2789,
                    Cities = new List<City>
                    {
                        new City { Name = "Bandar Abbas", Latitude = 27.1865, Longitude = 56.2789 },
                        new City { Name = "Qeshm", Latitude = 26.9561, Longitude = 56.2711 },
                        new City { Name = "Minab", Latitude = 27.0453, Longitude = 57.0789 },
                        new City { Name = "Bandar Lengeh", Latitude = 26.5497, Longitude = 54.8861 },
                        new City { Name = "Rudan", Latitude = 27.4039, Longitude = 57.0081 },
                        new City { Name = "Bastak", Latitude = 27.2033, Longitude = 54.8864 },
                        new City { Name = "Hajiabad", Latitude = 28.0064, Longitude = 55.8317 },
                        new City { Name = "Kish Island", Latitude = 26.5378, Longitude = 53.9817 }
                    }
                },
                new Province
                {
                    Name = "Hamadan",
                    NamePersian = "همدان",
                    Code = 13,
                    Latitude = 34.7998,
                    Longitude = 48.5147,
                    Cities = new List<City>
                    {
                        new City { Name = "Hamadan", Latitude = 34.7998, Longitude = 48.5147 },
                        new City { Name = "Malayer", Latitude = 34.2931, Longitude = 48.8214 },
                        new City { Name = "Nahavand", Latitude = 34.1903, Longitude = 48.3925 },
                        new City { Name = "Tuyserkan", Latitude = 34.5483, Longitude = 48.4669 },
                        new City { Name = "Asadabad", Latitude = 34.7681, Longitude = 48.1142 },
                        new City { Name = "Bahar", Latitude = 34.8878, Longitude = 48.3544 },
                        new City { Name = "Razan", Latitude = 35.1686, Longitude = 49.0267 },
                        new City { Name = "Kabudarahang", Latitude = 35.2117, Longitude = 48.7447 }
                    }
                },
                new Province
                {
                    Name = "Gilan",
                    NamePersian = "گیلان",
                    Code = 14,
                    Latitude = 37.2440,
                    Longitude = 49.5661,
                    Cities = new List<City>
                    {
                        new City { Name = "Rasht", Latitude = 37.2440, Longitude = 49.5661 },
                        new City { Name = "Lahijan", Latitude = 37.1961, Longitude = 50.0136 },
                        new City { Name = "Bandar-e Anzali", Latitude = 37.4658, Longitude = 49.4589 },
                        new City { Name = "Fuman", Latitude = 37.2547, Longitude = 49.3394 },
                        new City { Name = "Astara", Latitude = 38.4269, Longitude = 48.8719 },
                        new City { Name = "Rudbar", Latitude = 36.8239, Longitude = 49.4169 },
                        new City { Name = "Talesh", Latitude = 37.8333, Longitude = 48.9167 },
                        new City { Name = "Sowme'eh Sara", Latitude = 37.3119, Longitude = 49.2939 }
                    }
                },
                new Province
                {
                    Name = "Mazandaran",
                    NamePersian = "مازندران",
                    Code = 15,
                    Latitude = 36.5358,
                    Longitude = 52.3231,
                    Cities = new List<City>
                    {
                        new City { Name = "Sari", Latitude = 36.5358, Longitude = 52.3231 },
                        new City { Name = "Babol", Latitude = 36.5517, Longitude = 52.6825 },
                        new City { Name = "Amol", Latitude = 36.4667, Longitude = 52.3539 },
                        new City { Name = "Qaem Shahr", Latitude = 36.4758, Longitude = 52.8644 },
                        new City { Name = "Nowshahr", Latitude = 36.6556, Longitude = 51.5000 },
                        new City { Name = "Chalus", Latitude = 36.6514, Longitude = 51.4208 },
                        new City { Name = "Tonekabon", Latitude = 36.8167, Longitude = 50.8750 },
                        new City { Name = "Behshahr", Latitude = 36.6833, Longitude = 53.5333 }
                    }
                },
                new Province
                {
                    Name = "Golestan",
                    NamePersian = "گلستان",
                    Code = 16,
                    Latitude = 36.8433,
                    Longitude = 54.4392,
                    Cities = new List<City>
                    {
                        new City { Name = "Gorgan", Latitude = 36.8433, Longitude = 54.4392 },
                        new City { Name = "Gonbad-e Kavus", Latitude = 37.2583, Longitude = 55.1653 },
                        new City { Name = "Aliabad", Latitude = 36.9214, Longitude = 54.8700 },
                        new City { Name = "Bandar-e Torkaman", Latitude = 36.9056, Longitude = 54.0694 },
                        new City { Name = "Kordkuy", Latitude = 36.8458, Longitude = 54.0847 },
                        new City { Name = "Bandar-e Gaz", Latitude = 36.7347, Longitude = 54.0306 },
                        new City { Name = "Minudasht", Latitude = 37.2378, Longitude = 55.2894 },
                        new City { Name = "Kalaleh", Latitude = 37.3783, Longitude = 55.4964 }
                    }
                },
                new Province
                {
                    Name = "Kohgiluyeh and Boyer-Ahmad",
                    NamePersian = "کهگیلویه و بویراحمد",
                    Code = 17,
                    Latitude = 30.6508,
                    Longitude = 51.5903,
                    Cities = new List<City>
                    {
                        new City { Name = "Yasuj", Latitude = 30.6508, Longitude = 51.5903 },
                        new City { Name = "Dehdasht", Latitude = 30.8269, Longitude = 50.8614 },
                        new City { Name = "Gachsaran", Latitude = 30.3492, Longitude = 50.7975 },
                        new City { Name = "Likak", Latitude = 30.6561, Longitude = 50.5097 },
                        new City { Name = "Bahmai", Latitude = 30.9500, Longitude = 50.6167 },
                        new City { Name = "Dishmok", Latitude = 30.6500, Longitude = 50.3667 }
                    }
                },
                new Province
                {
                    Name = "Bushehr",
                    NamePersian = "بوشهر",
                    Code = 18,
                    Latitude = 28.9234,
                    Longitude = 50.8231,
                    Cities = new List<City>
                    {
                        new City { Name = "Bushehr", Latitude = 28.9234, Longitude = 50.8231 },
                        new City { Name = "Bandar-e Ganaveh", Latitude = 29.5756, Longitude = 50.5239 },
                        new City { Name = "Dashti", Latitude = 29.4531, Longitude = 50.4556 },
                        new City { Name = "Dashtestan", Latitude = 29.2839, Longitude = 51.0611 },
                        new City { Name = "Kangan", Latitude = 27.8364, Longitude = 51.9069 },
                        new City { Name = "Bandar-e Dayyer", Latitude = 28.7772, Longitude = 51.2508 },
                        new City { Name = "Tangestan", Latitude = 29.1500, Longitude = 51.1500 }
                    }
                },
                new Province
                {
                    Name = "Zanjan",
                    NamePersian = "زنجان",
                    Code = 19,
                    Latitude = 36.6736,
                    Longitude = 48.4839,
                    Cities = new List<City>
                    {
                        new City { Name = "Zanjan", Latitude = 36.6736, Longitude = 48.4839 },
                        new City { Name = "Abhar", Latitude = 36.1500, Longitude = 49.2167 },
                        new City { Name = "Khodabandeh", Latitude = 36.1175, Longitude = 48.5750 },
                        new City { Name = "Qeydar", Latitude = 36.9083, Longitude = 48.7458 },
                        new City { Name = "Mahneshan", Latitude = 36.6389, Longitude = 48.4900 },
                        new City { Name = "Ijrud", Latitude = 36.7333, Longitude = 48.5667 },
                        new City { Name = "Khorramdarreh", Latitude = 36.2156, Longitude = 49.1972 },
                        new City { Name = "Tarom", Latitude = 36.8764, Longitude = 49.0531 }
                    }
                },
                new Province
                {
                    Name = "Semnan",
                    NamePersian = "سمنان",
                    Code = 20,
                    Latitude = 35.5769,
                    Longitude = 53.3931,
                    Cities = new List<City>
                    {
                        new City { Name = "Semnan", Latitude = 35.5769, Longitude = 53.3931 },
                        new City { Name = "Damghan", Latitude = 36.1533, Longitude = 54.3439 },
                        new City { Name = "Shahrud", Latitude = 36.4156, Longitude = 55.0181 },
                        new City { Name = "Garmsar", Latitude = 35.2175, Longitude = 52.3136 },
                        new City { Name = "Mehdishahr", Latitude = 35.7169, Longitude = 53.3822 },
                        new City { Name = "Sorkheh", Latitude = 35.4644, Longitude = 53.7122 },
                        new City { Name = "Biarjomand", Latitude = 36.4833, Longitude = 55.4167 }
                    }
                },
                new Province
                {
                    Name = "Sistan and Baluchestan",
                    NamePersian = "سیستان و بلوچستان",
                    Code = 21,
                    Latitude = 29.4963,
                    Longitude = 60.8629,
                    Cities = new List<City>
                    {
                        new City { Name = "Zahedan", Latitude = 29.4963, Longitude = 60.8629 },
                        new City { Name = "Zabol", Latitude = 31.0289, Longitude = 61.4914 },
                        new City { Name = "Chabahar", Latitude = 25.2867, Longitude = 60.6217 },
                        new City { Name = "Iranshahr", Latitude = 27.2017, Longitude = 60.6839 },
                        new City { Name = "Saravan", Latitude = 27.3642, Longitude = 62.3389 },
                        new City { Name = "Khash", Latitude = 28.2208, Longitude = 61.2133 },
                        new City { Name = "Nikshahr", Latitude = 26.2389, Longitude = 60.2167 },
                        new City { Name = "Sarbaz", Latitude = 26.4333, Longitude = 61.8333 }
                    }
                },
                new Province
                {
                    Name = "Kurdistan",
                    NamePersian = "کردستان",
                    Code = 22,
                    Latitude = 35.3156,
                    Longitude = 46.9964,
                    Cities = new List<City>
                    {
                        new City { Name = "Sanandaj", Latitude = 35.3156, Longitude = 46.9964 },
                        new City { Name = "Marivan", Latitude = 35.5278, Longitude = 46.1781 },
                        new City { Name = "Saghez", Latitude = 36.2536, Longitude = 46.2611 },
                        new City { Name = "Baneh", Latitude = 35.9911, Longitude = 45.8778 },
                        new City { Name = "Qorveh", Latitude = 35.0797, Longitude = 47.8056 },
                        new City { Name = "Divandarreh", Latitude = 36.0919, Longitude = 46.9381 },
                        new City { Name = "Kamyaran", Latitude = 34.9406, Longitude = 47.0950 },
                        new City { Name = "Bijar", Latitude = 35.8694, Longitude = 47.5953 }
                    }
                },
                new Province
                {
                    Name = "Markazi",
                    NamePersian = "مرکزی",
                    Code = 23,
                    Latitude = 34.0996,
                    Longitude = 49.6998,
                    Cities = new List<City>
                    {
                        new City { Name = "Arak", Latitude = 34.0996, Longitude = 49.6998 },
                        new City { Name = "Saveh", Latitude = 35.0639, Longitude = 50.3472 },
                        new City { Name = "Khomein", Latitude = 33.9947, Longitude = 50.0647 },
                        new City { Name = "Mahallat", Latitude = 33.8750, Longitude = 50.4986 },
                        new City { Name = "Delijan", Latitude = 33.9897, Longitude = 50.6842 },
                        new City { Name = "Tafresh", Latitude = 34.6944, Longitude = 50.0111 },
                        new City { Name = "Ashtian", Latitude = 34.5169, Longitude = 50.0111 },
                        new City { Name = "Shazand", Latitude = 33.9333, Longitude = 49.4167 }
                    }
                },
                new Province
                {
                    Name = "Chaharmahal and Bakhtiari",
                    NamePersian = "چهارمحال و بختیاری",
                    Code = 24,
                    Latitude = 32.3252,
                    Longitude = 50.8625,
                    Cities = new List<City>
                    {
                        new City { Name = "Shahrekord", Latitude = 32.3252, Longitude = 50.8625 },
                        new City { Name = "Borujen", Latitude = 32.2686, Longitude = 51.2942 },
                        new City { Name = "Farsan", Latitude = 32.2389, Longitude = 50.5858 },
                        new City { Name = "Lordegan", Latitude = 31.4981, Longitude = 51.2247 },
                        new City { Name = "Ardal", Latitude = 32.2739, Longitude = 50.5456 },
                        new City { Name = "Kiar", Latitude = 31.9547, Longitude = 50.8483 },
                        new City { Name = "Samun", Latitude = 32.0667, Longitude = 50.7667 },
                        new City { Name = "Farrokhshahr", Latitude = 32.3961, Longitude = 50.9389 }
                    }
                },
                new Province
                {
                    Name = "Qazvin",
                    NamePersian = "قزوین",
                    Code = 25,
                    Latitude = 36.2675,
                    Longitude = 50.0044,
                    Cities = new List<City>
                    {
                        new City { Name = "Qazvin", Latitude = 36.2675, Longitude = 50.0044 },
                        new City { Name = "Takestan", Latitude = 36.0697, Longitude = 49.6983 },
                        new City { Name = "Buin Zahra", Latitude = 35.7678, Longitude = 50.0508 },
                        new City { Name = "Abyek", Latitude = 36.0689, Longitude = 50.5536 },
                        new City { Name = "Alamut-e Sharqi", Latitude = 36.4333, Longitude = 50.5500 },
                        new City { Name = "Alamut-e Gharbi", Latitude = 36.4500, Longitude = 50.5667 },
                        new City { Name = "Avaj", Latitude = 35.5853, Longitude = 49.3828 }
                    }
                },
                new Province
                {
                    Name = "Ilam",
                    NamePersian = "ایلام",
                    Code = 26,
                    Latitude = 33.6358,
                    Longitude = 46.4236,
                    Cities = new List<City>
                    {
                        new City { Name = "Ilam", Latitude = 33.6358, Longitude = 46.4236 },
                        new City { Name = "Eivan", Latitude = 33.7417, Longitude = 46.0500 },
                        new City { Name = "Dehloran", Latitude = 33.2858, Longitude = 46.9128 },
                        new City { Name = "Darreh Shahr", Latitude = 33.1306, Longitude = 47.4242 },
                        new City { Name = "Mehran", Latitude = 33.1278, Longitude = 46.1781 },
                        new City { Name = "Abdanan", Latitude = 33.1169, Longitude = 47.4142 },
                        new City { Name = "Shirvan", Latitude = 33.6508, Longitude = 46.3333 },
                        new City { Name = "Badreh", Latitude = 33.1500, Longitude = 47.2000 }
                    }
                },
                new Province
                {
                    Name = "Lorestan",
                    NamePersian = "لرستان",
                    Code = 27,
                    Latitude = 33.4956,
                    Longitude = 48.3533,
                    Cities = new List<City>
                    {
                        new City { Name = "Khorramabad", Latitude = 33.4956, Longitude = 48.3533 },
                        new City { Name = "Borujerd", Latitude = 33.8969, Longitude = 48.7539 },
                        new City { Name = "Dorud", Latitude = 33.4967, Longitude = 49.2219 },
                        new City { Name = "Kuhdasht", Latitude = 33.5256, Longitude = 47.6197 },
                        new City { Name = "Aligudarz", Latitude = 33.4008, Longitude = 49.6881 },
                        new City { Name = "Nurabad", Latitude = 34.0764, Longitude = 47.9739 },
                        new City { Name = "Azna", Latitude = 33.8756, Longitude = 49.4633 },
                        new City { Name = "Pol-e Dokhtar", Latitude = 33.5117, Longitude = 47.7289 }
                    }
                },
                new Province
                {
                    Name = "South Khorasan",
                    NamePersian = "خراسان جنوبی",
                    Code = 28,
                    Latitude = 32.8669,
                    Longitude = 59.2169,
                    Cities = new List<City>
                    {
                        new City { Name = "Birjand", Latitude = 32.8669, Longitude = 59.2169 },
                        new City { Name = "Bojnurd", Latitude = 37.4728, Longitude = 57.3267 },
                        new City { Name = "Ghayen", Latitude = 33.7378, Longitude = 59.1786 },
                        new City { Name = "Ferdows", Latitude = 34.0147, Longitude = 58.1675 },
                        new City { Name = "Tabas", Latitude = 33.5964, Longitude = 56.9361 },
                        new City { Name = "Sarayan", Latitude = 32.9364, Longitude = 58.9597 },
                        new City { Name = "Zirkuh", Latitude = 32.8667, Longitude = 59.7667 }
                    }
                },
                new Province
                {
                    Name = "North Khorasan",
                    NamePersian = "خراسان شمالی",
                    Code = 29,
                    Latitude = 37.4728,
                    Longitude = 57.3267,
                    Cities = new List<City>
                    {
                        new City { Name = "Bojnurd", Latitude = 37.4728, Longitude = 57.3267 },
                        new City { Name = "Shirvan", Latitude = 37.4247, Longitude = 57.9364 },
                        new City { Name = "Esfarayen", Latitude = 37.0800, Longitude = 57.5069 },
                        new City { Name = "Garmeh", Latitude = 36.9739, Longitude = 57.2311 },
                        new City { Name = "Jajrom", Latitude = 37.3614, Longitude = 57.7819 },
                        new City { Name = "Maneh", Latitude = 37.4000, Longitude = 57.5000 },
                        new City { Name = "Sankhast", Latitude = 36.9500, Longitude = 57.5000 }
                    }
                },
                new Province
                {
                    Name = "Ardabil",
                    NamePersian = "اردبیل",
                    Code = 30,
                    Latitude = 38.2506,
                    Longitude = 48.2935,
                    Cities = new List<City>
                    {
                        new City { Name = "Ardabil", Latitude = 38.2506, Longitude = 48.2935 },
                        new City { Name = "Parsabad", Latitude = 39.6481, Longitude = 47.9158 },
                        new City { Name = "Meshginshahr", Latitude = 38.3956, Longitude = 47.9964 },
                        new City { Name = "Namin", Latitude = 38.4167, Longitude = 48.4667 },
                        new City { Name = "Khalkhal", Latitude = 37.6217, Longitude = 48.5267 },
                        new City { Name = "Sarein", Latitude = 38.1500, Longitude = 48.0833 },
                        new City { Name = "Germi", Latitude = 38.9697, Longitude = 47.9206 }
                    }
                },
                new Province
                {
                    Name = "Qom",
                    NamePersian = "قم",
                    Code = 31,
                    Latitude = 34.6409,
                    Longitude = 50.8764,
                    Cities = new List<City>
                    {
                        new City { Name = "Qom", Latitude = 34.6409, Longitude = 50.8764 },
                        new City { Name = "Kahak", Latitude = 34.7667, Longitude = 50.8833 },
                        new City { Name = "Qahan", Latitude = 34.5167, Longitude = 50.8667 }
                    }
                }
            };
        }

        public static Province? GetProvinceByName(string name)
        {
            return Provinces.FirstOrDefault(p => 
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static List<City> GetCitiesByProvince(string provinceName)
        {
            var province = GetProvinceByName(provinceName);
            return province?.Cities ?? new List<City>();
        }
    }
}
